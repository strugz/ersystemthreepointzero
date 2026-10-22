using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Entitlements;
using ERFWeb.Application.Features.Lookups;
using ERFWeb.Domain;
using ERFWeb.Domain.Rates;
using ERFWeb.Domain.Scope;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Entitlements;

/// <summary>
/// Rate profiles (RBAC v0.6 flow 4). Only a System Administrator changes rates, never in an
/// assignment that names themselves (9.12). Lines are versioned and never edited
/// in place; a change that starts in the past needs an override and a reason. Every change is audited
/// in the same transaction and affected employees are notified after commit (9.14).
/// </summary>
public sealed class RateService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    EntitlementContext context,
    IEntitlementChangeNotifier notifier,
    ILogger<RateService> logger) : IRateService
{
    private const int MaxNameLength = 200;

    // ---------- Profiles ----------

    public async Task<IReadOnlyList<RateProfileListItemDto>> ListProfilesAsync(bool includeInactive, CancellationToken ct = default)
    {
        var today = context.Today;
        return await db.RateProfiles.AsNoTracking()
            .Where(p => includeInactive || p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new RateProfileListItemDto(
                p.Id,
                p.Name,
                p.IsActive,
                db.RateProfileLines.Count(l => l.RateProfileId == p.Id && l.EffectiveFrom <= today && (l.EffectiveTo == null || l.EffectiveTo >= today)),
                db.RateProfileAssignments.Count(a => a.RateProfileId == p.Id && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)),
                EF.Property<uint>(p, DbContextExtensions.RowVersionProperty)))
            .ToListAsync(ct);
    }

    public async Task<RateProfileDetailDto> GetProfileAsync(long profileId, CancellationToken ct = default)
    {
        var profile = await db.RateProfiles.AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => new { p.Id, p.Name, p.IsActive, RowVersion = EF.Property<uint>(p, DbContextExtensions.RowVersionProperty) })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Rate profile not found.");

        var lines = await LinesAsync(l => l.RateProfileId == profileId, ct);
        var assignments = await AssignmentsAsync(a => a.RateProfileId == profileId, ct);
        return new RateProfileDetailDto(profile.Id, profile.Name, profile.IsActive, lines, assignments, profile.RowVersion);
    }

    public async Task<RateProfileDetailDto> CreateProfileAsync(CreateRateProfileRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = ValidName(request.Name);
        await RequireRateRoleAsync(ct);

        var id = await tx.ExecuteAsync(async token =>
        {
            if (await db.RateProfiles.AnyAsync(p => p.Name == name, token))
            {
                throw new ValidationException("name", "A rate profile with that name already exists.");
            }

            var entity = new RateProfile { Name = name, IsActive = true, CreatedByUserId = user.UserId, CreatedAt = clock.UtcNow };
            db.RateProfiles.Add(entity);
            await db.SaveChangesAsync(token);
            await audit.WriteAsync(new WorkflowAuditEntry(EntitlementAuditEvents.ProfileCreated, null, user.UserId,
                JsonSerializer.Serialize(new { profileId = entity.Id, entity.Name })), token);
            return entity.Id;
        }, ct);

        return await GetProfileAsync(id, ct);
    }

    public async Task<RateProfileDetailDto> UpdateProfileAsync(long profileId, UpdateRateProfileRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = ValidName(request.Name);
        await RequireRateRoleAsync(ct);

        await tx.ExecuteAsync(async token =>
        {
            var entity = await db.RateProfiles.FirstOrDefaultAsync(p => p.Id == profileId, token)
                ?? throw new NotFoundException("Rate profile not found.");
            if (await db.RateProfiles.AnyAsync(p => p.Name == name && p.Id != profileId, token))
            {
                throw new ValidationException("name", "A rate profile with that name already exists.");
            }

            var today = context.Today;
            if (!request.IsActive && entity.IsActive
                && await db.RateProfileAssignments.AnyAsync(a => a.RateProfileId == profileId && (a.EffectiveTo == null || a.EffectiveTo >= today), token))
            {
                throw new ConflictException("The profile is still assigned. End its assignments before deactivating it.");
            }

            var before = new { entity.Name, entity.IsActive };
            entity.Name = name;
            entity.IsActive = request.IsActive;
            db.SetRowVersion(entity, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);
            await audit.WriteAsync(new WorkflowAuditEntry(EntitlementAuditEvents.ProfileUpdated, null, user.UserId,
                JsonSerializer.Serialize(new { profileId, before, after = new { entity.Name, entity.IsActive } })), token);
        }, ct);

        return await GetProfileAsync(profileId, ct);
    }

    // ---------- Lines (versioned) ----------

    public async Task<RateProfileDetailDto> AddLineAsync(long profileId, AddRateLineRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sub = ValidateLine(request);
        var today = context.Today;
        if (RateRules.BackdateProblem(request.EffectiveFrom, today, request.BackdateOverride, request.ChangeReason) is { } backdate)
        {
            throw new ValidationException("effectiveFrom", backdate);
        }

        var outcome = await tx.ExecuteAsync(async token =>
        {
            var profile = await db.RateProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == profileId, token)
                ?? throw new NotFoundException("Rate profile not found.");
            if (!profile.IsActive)
            {
                throw new ConflictException("The profile is inactive. Reactivate it before changing its rates.");
            }

            var currency = await ResolveCurrencyAsync(request.Currency, token);

            // A line change reaches everyone the profile is assigned to on that date; each target is checked (9.12, 9.15).
            var actor = await context.ActorAsync(token);
            var targets = await db.RateProfileAssignments.AsNoTracking()
                .Where(a => a.RateProfileId == profileId && (a.EffectiveTo == null || a.EffectiveTo >= request.EffectiveFrom))
                .Select(a => new RateTarget(a.TargetType, a.UserId, a.JobGrade, a.DepartmentId))
                .ToListAsync(token);
            Authorize(actor, targets.Count > 0 ? targets : [new RateTarget(RateTargetType.Individual, null, null, null)]);

            var key = await db.RateProfileLines
                .Where(l => l.RateProfileId == profileId && l.Category == request.Category && (l.SubCategory ?? "") == (sub ?? ""))
                .ToListAsync(token);
            var plan = RateRules.PlanVersion(
                key.Select(l => new RateLineCandidate(l.Id, l.RateProfileId, l.Category, l.SubCategory, l.RateType, l.Amount, l.RequiresReceipt, l.EffectiveFrom, l.EffectiveTo)),
                request.EffectiveFrom,
                request.EffectiveTo);
            if (!plan.IsValid)
            {
                throw new ValidationException("effectiveFrom", plan.Problem!);
            }

            object? before = null;
            if (plan.CloseLineId is { } closeId)
            {
                var closing = key.First(l => l.Id == closeId);
                before = new { closing.Id, closing.Amount, closing.RateType, closing.EffectiveFrom, closing.EffectiveTo };
                closing.EffectiveTo = plan.CloseOn;
            }

            var line = new RateProfileLine
            {
                RateProfileId = profileId,
                Category = request.Category,
                SubCategory = sub,
                RateType = request.RateType,
                Amount = request.Amount,
                Currency = currency,
                FrequencyCap = request.FrequencyCap,
                RequiresReceipt = request.RequiresReceipt,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                ChangedByUserId = user.UserId,
                ChangeReason = Clean(request.ChangeReason),
                BackdateOverride = request.EffectiveFrom < today && request.BackdateOverride,
                CreatedAt = clock.UtcNow,
            };
            db.RateProfileLines.Add(line);
            await db.SaveWithConcurrencyAsync(token);

            var after = new { line.Id, category = line.Category.ToString(), line.SubCategory, rateType = line.RateType.ToString(), line.Amount, line.EffectiveFrom, line.EffectiveTo };
            await audit.WriteAsync(new WorkflowAuditEntry(EntitlementAuditEvents.LineAdded, null, user.UserId,
                JsonSerializer.Serialize(new { profileId, reason = line.ChangeReason, backdate = line.BackdateOverride, before, after })), token);

            var affected = await AffectedByProfileAsync(profileId, request.EffectiveFrom, token);
            return (Affected: affected, Before: before is null ? null : JsonSerializer.Serialize(before), After: JsonSerializer.Serialize(after), profile.Name);
        }, ct);

        await NotifyAsync(EntitlementAuditEvents.LineAdded, $"Rate change on profile {outcome.Name}", outcome.Affected, outcome.Before, outcome.After, ct);
        return await GetProfileAsync(profileId, ct);
    }

    // ---------- Assignments ----------

    public Task<IReadOnlyList<RateAssignmentDto>> ListAssignmentsAsync(RateAssignmentQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var today = context.Today;
        return AssignmentsAsync(a =>
            (query.ProfileId == null || a.RateProfileId == query.ProfileId)
            && (query.TargetType == null || a.TargetType == query.TargetType)
            && (query.IncludeEnded || a.EffectiveTo == null || a.EffectiveTo >= today), ct);
    }

    public async Task<RateAssignmentDto> CreateAssignmentAsync(CreateRateAssignmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = ValidTarget(request);
        var today = context.Today;
        var from = request.EffectiveFrom ?? today;
        if (request.EffectiveTo is { } to && to < from)
        {
            throw new ValidationException("effectiveTo", "The end date must be on or after the start date.");
        }

        if (RateRules.BackdateProblem(from, today, request.BackdateOverride, request.ChangeReason) is { } backdate)
        {
            throw new ValidationException("effectiveFrom", backdate);
        }

        var outcome = await tx.ExecuteAsync(async token =>
        {
            var profile = await db.RateProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProfileId, token)
                ?? throw new ValidationException("profileId", "The rate profile does not exist.");
            if (!profile.IsActive)
            {
                throw new ValidationException("profileId", "The rate profile is inactive.");
            }

            await EnsureTargetExistsAsync(target, token);
            Authorize(await context.ActorAsync(token), [target]);

            // One assignment per target at a time: the open one is closed the day before the new one starts.
            var sameTarget = await db.RateProfileAssignments
                .Where(a => a.TargetType == target.Type && a.UserId == target.UserId && a.JobGrade == target.JobGrade && a.DepartmentId == target.DepartmentId)
                .ToListAsync(token);
            if (sameTarget.Any(a => a.EffectiveFrom >= from && RateRules.Overlaps(a.EffectiveFrom, a.EffectiveTo, from, request.EffectiveTo)))
            {
                throw new ConflictException("Another assignment for this target starts on or after that date. End or change it first.");
            }

            object? closed = null;
            foreach (var open in sameTarget.Where(a => RateRules.Overlaps(a.EffectiveFrom, a.EffectiveTo, from, request.EffectiveTo)))
            {
                closed = new { open.Id, open.RateProfileId, open.EffectiveFrom, open.EffectiveTo };
                open.EffectiveTo = from.AddDays(-1);
            }

            var entity = new RateProfileAssignment
            {
                RateProfileId = request.ProfileId,
                TargetType = target.Type,
                UserId = target.UserId,
                JobGrade = target.JobGrade,
                DepartmentId = target.DepartmentId,
                EffectiveFrom = from,
                EffectiveTo = request.EffectiveTo,
                ChangeReason = Clean(request.ChangeReason),
                AssignedByUserId = user.UserId,
                AssignedAt = clock.UtcNow,
            };
            db.RateProfileAssignments.Add(entity);
            await db.SaveWithConcurrencyAsync(token);

            var after = new { entity.Id, profile = profile.Name, target = target.Type.ToString(), target.UserId, target.JobGrade, target.DepartmentId, from, to = request.EffectiveTo };
            await audit.WriteAsync(new WorkflowAuditEntry(EntitlementAuditEvents.AssignmentCreated, null, user.UserId,
                JsonSerializer.Serialize(new { reason = entity.ChangeReason, closed, after })), token);

            var affected = await AffectedByTargetAsync(target, token);
            return (entity.Id, Affected: affected, Before: closed is null ? null : JsonSerializer.Serialize(closed), After: JsonSerializer.Serialize(after));
        }, ct);

        await NotifyAsync(EntitlementAuditEvents.AssignmentCreated, $"Rate profile assignment {outcome.Id} created", outcome.Affected, outcome.Before, outcome.After, ct);
        return (await AssignmentsAsync(a => a.Id == outcome.Id, ct)).Single();
    }

    public async Task<RateAssignmentDto> EndAssignmentAsync(long assignmentId, EndRateAssignmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var today = context.Today;
        if (request.EffectiveTo < today.AddDays(-1))
        {
            throw new ValidationException("effectiveTo", "An assignment can end yesterday at the earliest; earlier periods may already hold filed reports.");
        }

        var outcome = await tx.ExecuteAsync(async token =>
        {
            var entity = await db.RateProfileAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId, token)
                ?? throw new NotFoundException("Rate profile assignment not found.");
            if (request.EffectiveTo < entity.EffectiveFrom)
            {
                throw new ValidationException("effectiveTo", "The end date must be on or after the start date.");
            }

            var target = new RateTarget(entity.TargetType, entity.UserId, entity.JobGrade, entity.DepartmentId);
            Authorize(await context.ActorAsync(token), [target]);

            var before = new { entity.EffectiveTo };
            entity.EffectiveTo = request.EffectiveTo;
            db.SetRowVersion(entity, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(EntitlementAuditEvents.AssignmentEnded, null, user.UserId,
                JsonSerializer.Serialize(new { assignmentId, reason = Clean(request.ChangeReason), before, after = new { entity.EffectiveTo } })), token);
            return (Affected: await AffectedByTargetAsync(target, token), Before: JsonSerializer.Serialize(before));
        }, ct);

        await NotifyAsync(EntitlementAuditEvents.AssignmentEnded, $"Rate profile assignment {assignmentId} ends {request.EffectiveTo:yyyy-MM-dd}", outcome.Affected, outcome.Before, null, ct);
        return (await AssignmentsAsync(a => a.Id == assignmentId, ct)).Single();
    }

    // ---------- Resolution ----------

    public async Task<ResolvedRatesDto> ResolveAsync(long userId, DateOnly? on, CancellationToken ct = default)
    {
        var date = on ?? context.Today;
        var subject = await context.SubjectAsync(userId, ct) ?? throw new NotFoundException("User not found.");
        var assignment = RateRules.ResolveAssignment(await context.RateAssignmentsAsync(ct), subject.Rate, date);
        if (assignment is null)
        {
            return new ResolvedRatesDto(userId, date, null, null, null, []);
        }

        var name = await db.RateProfiles.AsNoTracking().Where(p => p.Id == assignment.ProfileId).Select(p => p.Name).FirstAsync(ct);
        var lines = await LinesAsync(l => l.RateProfileId == assignment.ProfileId
            && l.EffectiveFrom <= date && (l.EffectiveTo == null || l.EffectiveTo >= date), ct);
        return new ResolvedRatesDto(userId, date, assignment.ProfileId, name, assignment.TargetType, lines);
    }

    public async Task<EmployeeRatesDto> EditorRatesAsync(long userId, DateOnly date, CancellationToken ct = default)
    {
        var resolved = await ResolveAsync(userId, date, ct);
        decimal Amount(ExpenseCategory category, string? sub) => resolved.Lines
            .FirstOrDefault(l => l.Category == category && string.Equals(l.SubCategory ?? "", sub ?? "", StringComparison.OrdinalIgnoreCase))?.Amount ?? 0m;

        return new EmployeeRatesDto(
            Amount(ExpenseCategory.Transportation, null),
            Amount(ExpenseCategory.Meals, MealSubCategories.Breakfast),
            Amount(ExpenseCategory.Meals, MealSubCategories.Lunch),
            Amount(ExpenseCategory.Meals, MealSubCategories.Dinner),
            Amount(ExpenseCategory.Meals, MealSubCategories.OtMeal));
    }

    // ---------- Rules and helpers ----------

    private async Task RequireRateRoleAsync(CancellationToken ct)
    {
        var actor = await context.ActorAsync(ct);
        if (!actor.Has(UserRole.SystemAdministrator))
        {
            throw new ForbiddenException("Only a System Administrator can set rates.");
        }
    }

    /// <summary>Checks every target; refuses on the first refusal.</summary>
    private static void Authorize(EntitlementActor actor, IEnumerable<RateTarget> targets)
    {
        foreach (var target in targets)
        {
            var decision = EntitlementAuthority.ForRates(actor, target);
            if (!decision.IsAllowed)
            {
                throw new ForbiddenException(decision.Reason!);
            }
        }
    }

    private async Task<List<long>> AffectedByProfileAsync(long profileId, DateOnly date, CancellationToken ct)
    {
        var assignments = await context.RateAssignmentsAsync(ct);
        var subjects = await context.SubjectsAsync(ct);
        return subjects.Where(s => RateRules.ResolveAssignment(assignments, s.Rate, date)?.ProfileId == profileId).Select(s => s.UserId).ToList();
    }

    private async Task<List<long>> AffectedByTargetAsync(RateTarget target, CancellationToken ct)
    {
        var probe = new RateAssignmentCandidate(0, 0, target.Type, target.UserId, target.JobGrade, target.DepartmentId, DateOnly.MinValue, null);
        var subjects = await context.SubjectsAsync(ct);
        return subjects.Where(s => RateRules.Matches(probe, s.Rate)).Select(s => s.UserId).ToList();
    }

    private async Task EnsureTargetExistsAsync(RateTarget target, CancellationToken ct)
    {
        if (target.UserId is { } u && !await db.Users.AsNoTracking().AnyAsync(x => x.Id == u && x.IsActive, ct))
        {
            throw new ValidationException("userId", "The person does not exist or is inactive.");
        }

        if (target.DepartmentId is { } d && !await db.Departments.AsNoTracking().AnyAsync(x => x.Id == d && x.IsActive, ct))
        {
            throw new ValidationException("departmentId", "The department does not exist or is inactive.");
        }
    }

    private async Task<string> ResolveCurrencyAsync(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return await db.Currencies.AsNoTracking().Where(c => c.IsDefault && c.IsActive).Select(c => c.Code).FirstOrDefaultAsync(ct) ?? "PHP";
        }

        var value = code.Trim().ToUpperInvariant();
        return await db.Currencies.AsNoTracking().AnyAsync(c => c.Code == value && c.IsActive, ct)
            ? value
            : throw new ValidationException("currency", "The currency does not exist or is inactive.");
    }

    private static RateTarget ValidTarget(CreateRateAssignmentRequest r)
    {
        var grade = string.IsNullOrWhiteSpace(r.JobGrade) ? null : r.JobGrade.Trim();
        return r.TargetType switch
        {
            RateTargetType.Individual when r.UserId is not null && grade is null && r.DepartmentId is null => new RateTarget(r.TargetType, r.UserId, null, null),
            RateTargetType.Grade when grade is not null && r.UserId is null && r.DepartmentId is null => new RateTarget(r.TargetType, null, grade, null),
            RateTargetType.Department when r.DepartmentId is not null && r.UserId is null && grade is null => new RateTarget(r.TargetType, null, null, r.DepartmentId),
            RateTargetType.Company when r.UserId is null && grade is null && r.DepartmentId is null => new RateTarget(r.TargetType, null, null, null),
            _ => throw new ValidationException("targetType",
                "Give exactly the field the target needs: a person for Individual, a job grade for Grade, a department for Department, nothing for Company."),
        };
    }

    private static string? ValidateLine(AddRateLineRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        var sub = string.IsNullOrWhiteSpace(r.SubCategory) ? null : r.SubCategory.Trim();
        if (r.Category == ExpenseCategory.Meals)
        {
            var match = MealSubCategories.All.FirstOrDefault(m => string.Equals(m, sub, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                errors["subCategory"] = [$"Meals need a meal: {string.Join(", ", MealSubCategories.All)}."];
            }

            sub = match;
        }
        else if (sub is not null)
        {
            errors["subCategory"] = ["Only Meals take a sub-category."];
        }

        if (r.Amount < 0)
        {
            errors["amount"] = ["The amount cannot be negative."];
        }

        if (r.FrequencyCap is < 1)
        {
            errors["frequencyCap"] = ["The frequency cap must be at least 1, or left empty."];
        }

        if (r.ChangeReason is { Length: > 500 })
        {
            errors["changeReason"] = ["The reason must be at most 500 characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return sub;
    }

    private static string ValidName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("name", "Name is required.");
        }

        var value = name.Trim();
        return value.Length <= MaxNameLength ? value : throw new ValidationException("name", $"Name must be at most {MaxNameLength} characters.");
    }

    private async Task<IReadOnlyList<RateLineDto>> LinesAsync(System.Linq.Expressions.Expression<Func<RateProfileLine, bool>> filter, CancellationToken ct) =>
        await (
            from l in db.RateProfileLines.AsNoTracking().Where(filter)
            join u in db.Users.AsNoTracking() on l.ChangedByUserId equals u.Id
            orderby l.Category, l.SubCategory, l.EffectiveFrom descending
            select new RateLineDto(l.Id, l.RateProfileId, l.Category, l.SubCategory, l.RateType, l.Amount, l.Currency, l.FrequencyCap,
                l.RequiresReceipt, l.EffectiveFrom, l.EffectiveTo, u.FullName, l.ChangeReason, l.BackdateOverride, l.CreatedAt))
            .ToListAsync(ct);

    private async Task<IReadOnlyList<RateAssignmentDto>> AssignmentsAsync(System.Linq.Expressions.Expression<Func<RateProfileAssignment, bool>> filter, CancellationToken ct) =>
        await (
            from a in db.RateProfileAssignments.AsNoTracking().Where(filter)
            join p in db.RateProfiles.AsNoTracking() on a.RateProfileId equals p.Id
            join assigner in db.Users.AsNoTracking() on a.AssignedByUserId equals assigner.Id
            join tu in db.Users.AsNoTracking() on a.UserId equals (long?)tu.Id into tus
            from target in tus.DefaultIfEmpty()
            join dj in db.Departments.AsNoTracking() on a.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            orderby a.TargetType, a.EffectiveFrom descending
            select new RateAssignmentDto(a.Id, a.RateProfileId, p.Name, a.TargetType, a.UserId, target != null ? target.FullName : null,
                a.JobGrade, a.DepartmentId, d != null ? d.Name : null, a.EffectiveFrom, a.EffectiveTo, assigner.FullName, a.AssignedAt,
                a.IsBootstrap, a.ChangeReason, EF.Property<uint>(a, DbContextExtensions.RowVersionProperty)))
            .ToListAsync(ct);

    private async Task NotifyAsync(string kind, string summary, IReadOnlyList<long> affected, string? before, string? after, CancellationToken ct)
    {
        try
        {
            await notifier.NotifyAsync(new EntitlementChangeNotification(kind, summary, user.UserId, affected, before, after), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Rate change notification failed: {Summary}", summary);
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
