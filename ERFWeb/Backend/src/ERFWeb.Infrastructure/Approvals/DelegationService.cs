using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Delegations;
using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Scope;
using ERFWeb.Infrastructure.Entitlements;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Approvals;

/// <summary>
/// Approval delegation (plan Phase 7, Flow 7). Creating and cancelling run in a serializable
/// transaction with a <c>workflow_audit</c> row, so two overlapping delegations cannot both be
/// saved. A Level 2 is "over" a Level 1 when scope resolves them as the Level 2 for the Level 1's
/// own account, or when they are routed after that Level 1 on a report still awaiting approval.
/// </summary>
public sealed class DelegationService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    EntitlementContext context,
    ILogger<DelegationService> logger) : IDelegationService
{
    public async Task<MyDelegationsDto> MineAsync(CancellationToken ct = default)
    {
        var me = user.UserId;
        var rows = await db.ApprovalDelegations.AsNoTracking()
            .Where(d => d.SignatoryUserId == me || d.DelegateUserId == me || d.CreatedByUserId == me)
            .OrderByDescending(d => d.StartsOn).ThenByDescending(d => d.Id)
            .Select(d => new { Row = d, RowVersion = EF.Property<uint>(d, DbContextExtensions.RowVersionProperty) })
            .ToListAsync(ct);
        var dtos = await ToDtosAsync(rows.Select(r => (r.Row, r.RowVersion)).ToList(), ct);

        return new MyDelegationsDto(
            dtos.Where(d => d.SignatoryUserId == me).ToList(),
            dtos.Where(d => d.DelegateUserId == me).ToList(),
            dtos.Where(d => d.IsForced && d.SignatoryUserId != me && rows.Any(r => r.Row.Id == d.Id && r.Row.CreatedByUserId == me)).ToList());
    }

    public async Task<DelegationOptionsDto> OptionsAsync(CancellationToken ct = default)
    {
        var me = user.UserId;
        var levels = await DelegationLookup.SignatoryLevelsAsync(db, null, ct);
        int? myLevel = levels.TryGetValue(me, out var l) ? l : null;

        var subjects = await context.SubjectsAsync(ct);
        var candidates = subjects
            .Where(s => s.UserId != me && levels.ContainsKey(s.UserId))
            .Select(s => new DelegateCandidateDto(s.UserId, s.FullName, s.DepartmentName, levels[s.UserId]))
            .ToList();

        var covered = new List<CoveredSignatoryDto>();
        if (myLevel == 2)
        {
            var level1s = subjects.Where(s => s.UserId != me && levels.GetValueOrDefault(s.UserId) == 1).ToList();
            var over = await Level2OverAsync(me, level1s.Select(s => s.UserId).ToList(), ct);
            var pending = await PendingStepCountsAsync(over, ct);
            covered = level1s
                .Where(s => over.Contains(s.UserId))
                .Select(s => new CoveredSignatoryDto(s.UserId, s.FullName, s.DepartmentName, pending.GetValueOrDefault(s.UserId)))
                .ToList();
        }

        return new DelegationOptionsDto(myLevel, context.Today, DelegationRules.MaxDays, candidates, covered);
    }

    public async Task<DelegationDto> CreateAsync(CreateDelegationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var me = user.UserId;
        var signatoryId = request.SignatoryUserId ?? me;
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason is { Length: > DelegationRules.MaxReasonLength })
        {
            throw new ValidationException("reason", $"The reason must be at most {DelegationRules.MaxReasonLength} characters.");
        }

        var id = await tx.ExecuteAsync(async token =>
        {
            var today = context.Today;
            var levels = await DelegationLookup.SignatoryLevelsAsync(db, [signatoryId, request.DelegateUserId], token);
            var isOver = signatoryId != me && (await Level2OverAsync(me, [signatoryId], token)).Contains(signatoryId);
            var existing = await db.ApprovalDelegations.AsNoTracking()
                .Where(d => d.SignatoryUserId == signatoryId && d.CancelledAt == null && d.EndsOn >= today)
                .Select(d => new DelegationWindow(d.Id, d.SignatoryUserId, d.DelegateUserId, d.StartsOn, d.EndsOn, false))
                .ToListAsync(token);

            var verdict = DelegationRules.Check(
                new DelegationProposal(me, signatoryId, Level(levels, signatoryId), request.DelegateUserId, Level(levels, request.DelegateUserId),
                    request.StartsOn, request.EndsOn, isOver),
                existing,
                today);
            switch (verdict.Refusal)
            {
                case WorkflowRefusal.None:
                    break;
                case WorkflowRefusal.Forbidden:
                    throw new ForbiddenException(verdict.Reason!);
                case WorkflowRefusal.Conflict:
                    throw new ConflictException(verdict.Reason!);
                default:
                    throw new ValidationException("delegateUserId", verdict.Reason!);
            }

            var now = clock.UtcNow;
            var row = new ApprovalDelegation
            {
                SignatoryUserId = signatoryId,
                DelegateUserId = request.DelegateUserId,
                StartsOn = request.StartsOn,
                EndsOn = request.EndsOn,
                Reason = reason,
                IsForced = verdict.IsForced,
                CreatedByUserId = me,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.ApprovalDelegations.Add(row);
            await db.SaveChangesAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(DelegationAuditEvents.Created, null, me, JsonSerializer.Serialize(new
            {
                delegationId = row.Id,
                signatoryUserId = signatoryId,
                delegateUserId = request.DelegateUserId,
                startsOn = request.StartsOn,
                endsOn = request.EndsOn,
                forced = verdict.IsForced,
                reason,
            })), token);
            return row.Id;
        }, ct);

        logger.LogInformation("Delegation {DelegationId} created by {ActorUserId}", id, me);
        return await LoadAsync(id, ct);
    }

    public async Task<DelegationDto> CancelAsync(long delegationId, CancelDelegationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var me = user.UserId;

        await tx.ExecuteAsync(async token =>
        {
            var row = await db.ApprovalDelegations.FirstOrDefaultAsync(d => d.Id == delegationId, token)
                ?? throw new NotFoundException("Delegation not found.");
            if (row.SignatoryUserId != me && row.DelegateUserId != me && row.CreatedByUserId != me)
            {
                throw new NotFoundException("Delegation not found.");
            }

            if (DelegationRules.CancelError(ToWindow(row), me, row.CreatedByUserId, context.Today) is { } error)
            {
                throw row.SignatoryUserId == me || row.CreatedByUserId == me ? new ConflictException(error) : new ForbiddenException(error);
            }

            var now = clock.UtcNow;
            row.CancelledAt = now;
            row.CancelledByUserId = me;
            row.UpdatedAt = now;
            db.SetRowVersion(row, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(DelegationAuditEvents.Cancelled, null, me, JsonSerializer.Serialize(new
            {
                delegationId,
                signatoryUserId = row.SignatoryUserId,
                delegateUserId = row.DelegateUserId,
            })), token);
        }, ct);

        return await LoadAsync(delegationId, ct);
    }

    // ---------- Helpers ----------

    private async Task<DelegationDto> LoadAsync(long id, CancellationToken ct)
    {
        var row = await db.ApprovalDelegations.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new { Row = d, RowVersion = EF.Property<uint>(d, DbContextExtensions.RowVersionProperty) })
            .FirstAsync(ct);
        return (await ToDtosAsync([(row.Row, row.RowVersion)], ct))[0];
    }

    private async Task<List<DelegationDto>> ToDtosAsync(List<(ApprovalDelegation Row, uint RowVersion)> rows, CancellationToken ct)
    {
        var ids = rows
            .SelectMany(r => new[] { r.Row.SignatoryUserId, r.Row.DelegateUserId, r.Row.CreatedByUserId, r.Row.CancelledByUserId ?? r.Row.CreatedByUserId })
            .Distinct()
            .ToList();
        var names = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var levels = await DelegationLookup.SignatoryLevelsAsync(db, rows.Select(r => r.Row.SignatoryUserId), ct);
        var today = context.Today;
        var me = user.UserId;

        return rows.Select(x =>
        {
            var d = x.Row;
            var window = ToWindow(d);
            return new DelegationDto(
                d.Id,
                d.SignatoryUserId,
                names.GetValueOrDefault(d.SignatoryUserId, "?"),
                Level(levels, d.SignatoryUserId),
                d.DelegateUserId,
                names.GetValueOrDefault(d.DelegateUserId, "?"),
                d.StartsOn,
                d.EndsOn,
                d.Reason,
                d.IsForced,
                names.GetValueOrDefault(d.CreatedByUserId, "?"),
                d.CreatedAt,
                d.CancelledByUserId is { } by ? names.GetValueOrDefault(by) : null,
                d.CancelledAt,
                DelegationRules.StateOn(window, today),
                DelegationRules.CancelError(window, me, d.CreatedByUserId, today) is null,
                x.RowVersion);
        }).ToList();
    }

    /// <summary>Of <paramref name="level1Ids"/>, those <paramref name="level2Id"/> is the Level 2 over.</summary>
    private async Task<HashSet<long>> Level2OverAsync(long level2Id, IReadOnlyCollection<long> level1Ids, CancellationToken ct)
    {
        var result = new HashSet<long>();
        if (level1Ids.Count == 0)
        {
            return result;
        }

        // Routed after the Level 1 on a report that is still waiting.
        var coRouted = await (
            from l1 in db.ApprovalSteps.AsNoTracking()
            join r in db.ExpenseReports.AsNoTracking() on l1.ReportId equals r.Id
            where level1Ids.Contains(l1.ApproverUserId) && l1.Status == ApprovalStatus.Pending
                && l1.Cycle == r.CurrentCycle && r.Status == ReportStatus.ForApproval
            join l2 in db.ApprovalSteps.AsNoTracking() on new { l1.ReportId, l1.Cycle } equals new { l2.ReportId, l2.Cycle }
            where l2.ApproverUserId == level2Id && l2.StepOrder > l1.StepOrder
            select l1.ApproverUserId)
            .Distinct()
            .ToListAsync(ct);
        result.UnionWith(coRouted);

        // Scope: the Level 2 that covers the Level 1's own account today.
        var candidates = await context.ScopeCandidatesAsync(validHoldersOnly: true, ct);
        var today = context.Today;
        foreach (var id in level1Ids.Where(id => !result.Contains(id)))
        {
            if (await context.SubjectAsync(id, ct) is { } subject
                && ScopeRules.Resolve(candidates, subject.Scope, 2, today).HolderUserId == level2Id)
            {
                result.Add(id);
            }
        }

        return result;
    }

    private async Task<Dictionary<long, int>> PendingStepCountsAsync(IReadOnlyCollection<long> approverIds, CancellationToken ct) =>
        await (
            from s in db.ApprovalSteps.AsNoTracking()
            join r in db.ExpenseReports.AsNoTracking() on s.ReportId equals r.Id
            where approverIds.Contains(s.ApproverUserId) && s.Status == ApprovalStatus.Pending
                && s.Cycle == r.CurrentCycle && r.Status == ReportStatus.ForApproval
            group s by s.ApproverUserId into g
            select new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    private static int? Level(Dictionary<long, int> levels, long userId) => levels.TryGetValue(userId, out var level) ? level : null;

    private static DelegationWindow ToWindow(ApprovalDelegation d) =>
        new(d.Id, d.SignatoryUserId, d.DelegateUserId, d.StartsOn, d.EndsOn, d.CancelledAt is not null);
}
