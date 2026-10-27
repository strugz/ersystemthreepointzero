using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Entitlements;
using ERFWeb.Domain;
using ERFWeb.Domain.Scope;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Entitlements;

/// <summary>
/// Scope assignments (RBAC v0.6 flow 3). Every save is checked against the authority rules and
/// against the whole company: no employee may end up with two signatories at the same level and
/// precedence tier. Changes are audited in the same transaction; affected people are notified after commit.
/// </summary>
public sealed class ScopeService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    EntitlementContext context,
    IEntitlementChangeNotifier notifier,
    ILogger<ScopeService> logger) : IScopeService
{
    private const int MaxNamesInMessage = 5;

    public async Task<IReadOnlyList<ScopeAssignmentDto>> ListAsync(ScopeAssignmentQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = db.ScopeAssignments.AsNoTracking().AsQueryable();
        if (!query.IncludeInactive)
        {
            var today = context.Today;
            rows = rows.Where(a => a.IsActive && (a.EffectiveTo == null || a.EffectiveTo >= today));
        }

        if (query.HolderUserId is { } holder)
        {
            rows = rows.Where(a => a.HolderUserId == holder);
        }

        if (query.DepartmentId is { } department)
        {
            rows = rows.Where(a => a.DepartmentId == department);
        }

        if (query.Level is { } level)
        {
            rows = rows.Where(a => a.SignatoryLevel == level);
        }

        var ids = await rows.Select(a => a.Id).ToListAsync(ct);
        return await ProjectAsync(ids, ct);
    }

    public async Task<ScopeAssignmentDto> GetAsync(long id, CancellationToken ct = default) =>
        (await ProjectAsync([id], ct)).FirstOrDefault() ?? throw new NotFoundException("Scope assignment not found.");

    public async Task<ScopeAssignmentDto> CreateAsync(CreateScopeAssignmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var members = (request.MemberUserIds ?? []).Distinct().ToList();
        var from = request.EffectiveFrom ?? context.Today;
        ThrowIfInvalid(ScopeRules.ShapeErrors(request.Level, request.Type, request.DepartmentId, members, request.HolderUserId, from, request.EffectiveTo, request.ApprovalLimit));
        if (from < context.Today)
        {
            throw new ValidationException("effectiveFrom", "Scope cannot start in the past; reports already filed keep their snapshot.");
        }

        var outcome = await tx.ExecuteAsync(async token =>
        {
            await EnsureHolderAsync(request.HolderUserId, request.Level, token);
            await EnsureMembersAsync(members, token);
            await EnsureDepartmentAsync(request.DepartmentId, token);

            var existing = await context.ScopeCandidatesAsync(validHoldersOnly: true, token);
            var proposed = new ScopeCandidate(0, request.HolderUserId, request.Level, request.Type, request.DepartmentId,
                members.ToHashSet(), from, request.EffectiveTo, true, request.ApprovalLimit);

            await EnsureAuthorityAsync(token);
            await EnsureNoNewAmbiguityAsync(existing, existing.Append(proposed).ToList(), request.Level, from, token);

            var entity = new ScopeAssignment
            {
                HolderUserId = request.HolderUserId,
                SignatoryLevel = (short)request.Level,
                AssignmentType = request.Type,
                DepartmentId = request.DepartmentId,
                ApprovalLimit = request.ApprovalLimit,
                EffectiveFrom = from,
                EffectiveTo = request.EffectiveTo,
                IsActive = true,
                Note = Clean(request.Note),
                CreatedByUserId = user.UserId,
                CreatedAt = clock.UtcNow,
            };
            db.ScopeAssignments.Add(entity);
            await db.SaveChangesAsync(token);
            db.ScopeAssignmentMembers.AddRange(members.Select(m => new ScopeAssignmentMember { ScopeAssignmentId = entity.Id, UserId = m }));
            await db.SaveChangesAsync(token);

            var after = Describe(entity, members);
            await audit.WriteAsync(new WorkflowAuditEntry(EntitlementAuditEvents.ScopeCreated, null, user.UserId,
                JsonSerializer.Serialize(new { assignmentId = entity.Id, after })), token);

            var affected = await AffectedAsync(proposed, token);
            return (entity.Id, Affected: affected, After: JsonSerializer.Serialize(after));
        }, ct);

        await NotifyAsync(EntitlementAuditEvents.ScopeCreated, $"Scope assignment {outcome.Id} created", outcome.Affected, null, outcome.After, ct);
        return await GetAsync(outcome.Id, ct);
    }

    public async Task<ScopeAssignmentDto> UpdateAsync(long id, UpdateScopeAssignmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var members = (request.MemberUserIds ?? []).Distinct().ToList();

        var outcome = await tx.ExecuteAsync(async token =>
        {
            var entity = await db.ScopeAssignments.FirstOrDefaultAsync(a => a.Id == id, token)
                ?? throw new NotFoundException("Scope assignment not found.");
            var from = request.EffectiveFrom ?? entity.EffectiveFrom;
            ThrowIfInvalid(ScopeRules.ShapeErrors(request.Level, request.Type, request.DepartmentId, members,
                request.HolderUserId, from, request.EffectiveTo, request.ApprovalLimit));
            if (from != entity.EffectiveFrom && from < context.Today)
            {
                throw new ValidationException("effectiveFrom", "Scope cannot start in the past; reports already filed keep their snapshot.");
            }

            // The holder and department are checked only when they change, so an assignment whose holder has since
            // lost the role, or whose department was deactivated, can still be ended or narrowed.
            if (request.HolderUserId != entity.HolderUserId || request.Level != entity.SignatoryLevel)
            {
                await EnsureHolderAsync(request.HolderUserId, request.Level, token);
            }

            if (request.DepartmentId != entity.DepartmentId)
            {
                await EnsureDepartmentAsync(request.DepartmentId, token);
            }

            await EnsureMembersAsync(members, token);

            var oldMembers = await db.ScopeAssignmentMembers.Where(m => m.ScopeAssignmentId == id).ToListAsync(token);
            var existing = await context.ScopeCandidatesAsync(validHoldersOnly: true, token);
            var proposed = new ScopeCandidate(id, request.HolderUserId, request.Level, request.Type, request.DepartmentId,
                members.ToHashSet(), from, request.EffectiveTo, request.IsActive, request.ApprovalLimit);

            await EnsureAuthorityAsync(token);
            var checkFrom = from > context.Today ? from : context.Today;
            await EnsureNoNewAmbiguityAsync(existing, existing.Where(c => c.Id != id).Append(proposed).ToList(), request.Level, checkFrom, token);

            var before = Describe(entity, oldMembers.Select(m => m.UserId).ToList());
            var previous = EntitlementContext.ToCandidate(entity, oldMembers.Select(m => m.UserId).ToHashSet());

            entity.HolderUserId = request.HolderUserId;
            entity.SignatoryLevel = (short)request.Level;
            entity.AssignmentType = request.Type;
            entity.DepartmentId = request.DepartmentId;
            entity.EffectiveFrom = from;
            entity.ApprovalLimit = request.ApprovalLimit;
            entity.EffectiveTo = request.EffectiveTo;
            entity.IsActive = request.IsActive;
            entity.Note = Clean(request.Note) ?? entity.Note;
            db.SetRowVersion(entity, request.RowVersion);
            db.ScopeAssignmentMembers.RemoveRange(oldMembers);
            db.ScopeAssignmentMembers.AddRange(members.Select(m => new ScopeAssignmentMember { ScopeAssignmentId = id, UserId = m }));
            await db.SaveWithConcurrencyAsync(token);

            var after = Describe(entity, members);
            await audit.WriteAsync(new WorkflowAuditEntry(EntitlementAuditEvents.ScopeUpdated, null, user.UserId,
                JsonSerializer.Serialize(new { assignmentId = id, before, after })), token);

            var affected = (await AffectedAsync(previous, token)).Union(await AffectedAsync(proposed, token)).ToList();
            return (Affected: affected, Before: JsonSerializer.Serialize(before), After: JsonSerializer.Serialize(after));
        }, ct);

        await NotifyAsync(EntitlementAuditEvents.ScopeUpdated, $"Scope assignment {id} changed", outcome.Affected, outcome.Before, outcome.After, ct);
        return await GetAsync(id, ct);
    }

    public async Task<IReadOnlyList<ScopeCoverageRowDto>> CoverageAsync(DateOnly? date, CancellationToken ct = default)
    {
        var on = date ?? context.Today;
        var candidates = await context.ScopeCandidatesAsync(validHoldersOnly: true, ct);
        var subjects = await context.SubjectsAsync(ct);
        var names = await db.Users.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return subjects.Select(s =>
        {
            var l1 = ScopeRules.Resolve(candidates, s.Scope, 1, on);
            var l2 = ScopeRules.Resolve(candidates, s.Scope, 2, on);
            return new ScopeCoverageRowDto(
                s.UserId, s.FullName, s.DepartmentName,
                l1.HolderUserId, l1.HolderUserId is { } a ? names.GetValueOrDefault(a) : null, l1.Problem,
                l2.HolderUserId, l2.HolderUserId is { } b ? names.GetValueOrDefault(b) : null, l2.Problem);
        }).ToList();
    }

    // ---------- Rules ----------

    private async Task EnsureAuthorityAsync(CancellationToken ct)
    {
        var decision = EntitlementAuthority.ForScope(await context.ActorAsync(ct));
        if (!decision.IsAllowed)
        {
            throw new ForbiddenException(decision.Reason!);
        }
    }

    /// <summary>Refuses a change that would leave someone with two signatories at one level who had one before.</summary>
    private async Task EnsureNoNewAmbiguityAsync(IReadOnlyList<ScopeCandidate> before, IReadOnlyList<ScopeCandidate> after, int level, DateOnly date, CancellationToken ct)
    {
        var subjects = await context.SubjectsAsync(ct);
        var clashes = subjects
            .Where(s =>
            {
                var now = ScopeRules.Resolve(after, s.Scope, level, date);
                var was = ScopeRules.Resolve(before, s.Scope, level, date);
                return now.IsAmbiguous && !was.IsAmbiguous;
            })
            .Select(s => s.FullName)
            .ToList();

        if (clashes.Count > 0)
        {
            var shown = string.Join(", ", clashes.Take(MaxNamesInMessage)) + (clashes.Count > MaxNamesInMessage ? $" and {clashes.Count - MaxNamesInMessage} more" : string.Empty);
            throw new ValidationException("scope",
                $"This would give {shown} more than one Level {level} signatory. End or narrow the other assignment first, or name them individually.");
        }
    }

    private async Task<List<long>> AffectedAsync(ScopeCandidate assignment, CancellationToken ct)
    {
        var subjects = await context.SubjectsAsync(ct);
        return subjects.Where(s => ScopeRules.Covers(assignment, s.Scope)).Select(s => s.UserId).Append(assignment.HolderUserId).Distinct().ToList();
    }

    private async Task EnsureHolderAsync(long holderUserId, int level, CancellationToken ct)
    {
        var role = level == 1 ? UserRole.SignatoryLevel1 : UserRole.SignatoryLevel2;
        var ok = await (
            from u in db.Users.AsNoTracking()
            where u.Id == holderUserId && u.IsActive
            join g in db.UserRoleGrants.AsNoTracking() on u.Id equals g.UserId
            where g.Role == role && g.Status == RoleGrantStatus.Active
            select u.Id).AnyAsync(ct);
        if (!ok)
        {
            throw new ValidationException("holderUserId", $"The holder must be an active account with an Active Signatory Level {level} role.");
        }
    }

    private async Task EnsureMembersAsync(IReadOnlyCollection<long> members, CancellationToken ct)
    {
        if (members.Count == 0)
        {
            return;
        }

        var found = await db.Users.AsNoTracking().CountAsync(u => members.Contains(u.Id), ct);
        if (found != members.Count)
        {
            throw new ValidationException("memberUserIds", "One or more named people do not exist.");
        }
    }

    private async Task EnsureDepartmentAsync(long? departmentId, CancellationToken ct)
    {
        if (departmentId is { } d && !await db.Departments.AsNoTracking().AnyAsync(x => x.Id == d && x.IsActive, ct))
        {
            throw new ValidationException("departmentId", "The department does not exist or is inactive.");
        }
    }

    // ---------- Projection and helpers ----------

    private async Task<IReadOnlyList<ScopeAssignmentDto>> ProjectAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await (
            from a in db.ScopeAssignments.AsNoTracking()
            where ids.Contains(a.Id)
            join h in db.Users.AsNoTracking() on a.HolderUserId equals h.Id
            join cb in db.Users.AsNoTracking() on a.CreatedByUserId equals cb.Id
            join dj in db.Departments.AsNoTracking() on a.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            orderby a.SignatoryLevel, h.FullName, a.EffectiveFrom
            select new
            {
                Row = a,
                HolderName = h.FullName,
                CreatedByName = cb.FullName,
                DepartmentName = d != null ? d.Name : null,
                RowVersion = EF.Property<uint>(a, DbContextExtensions.RowVersionProperty),
            })
            .ToListAsync(ct);

        var members = await (
            from m in db.ScopeAssignmentMembers.AsNoTracking()
            where ids.Contains(m.ScopeAssignmentId)
            join u in db.Users.AsNoTracking() on m.UserId equals u.Id
            orderby u.FullName
            select new { m.ScopeAssignmentId, m.UserId, u.FullName })
            .ToListAsync(ct);

        return rows.Select(r => new ScopeAssignmentDto(
            r.Row.Id, r.Row.HolderUserId, r.HolderName, r.Row.SignatoryLevel, r.Row.AssignmentType,
            r.Row.DepartmentId, r.DepartmentName,
            members.Where(m => m.ScopeAssignmentId == r.Row.Id).Select(m => new ScopeMemberDto(m.UserId, m.FullName)).ToList(),
            r.Row.ApprovalLimit, r.Row.EffectiveFrom, r.Row.EffectiveTo, r.Row.IsActive, r.Row.IsBootstrap,
            r.CreatedByName, r.Row.CreatedAt, r.Row.Note, r.RowVersion)).ToList();
    }

    private async Task NotifyAsync(string kind, string summary, IReadOnlyList<long> affected, string? before, string? after, CancellationToken ct)
    {
        try
        {
            await notifier.NotifyAsync(new EntitlementChangeNotification(kind, summary, user.UserId, affected, before, after), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Scope change notification failed: {Summary}", summary);
        }
    }

    private static object Describe(ScopeAssignment a, IReadOnlyCollection<long> members) => new
    {
        holder = a.HolderUserId,
        level = a.SignatoryLevel,
        type = a.AssignmentType.ToString(),
        department = a.DepartmentId,
        members,
        limit = a.ApprovalLimit,
        from = a.EffectiveFrom,
        to = a.EffectiveTo,
        active = a.IsActive,
    };

    private static void ThrowIfInvalid(IReadOnlyDictionary<string, string> errors)
    {
        if (errors.Count > 0)
        {
            throw new ValidationException(errors.ToDictionary(e => e.Key, e => new[] { e.Value }));
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
