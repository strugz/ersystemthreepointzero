using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Approvals;

/// <summary>
/// Approver actions (plan Phase 5, RBAC v0.6 flow 5). Each action runs in one serializable
/// transaction: the report row version is checked (stale → 409), the step changes and the report
/// status are written together with a <c>workflow_audit</c> row, and notifications go out after
/// commit so a delivery failure never undoes a decision. A delegate acts on the steps of the
/// signatories they stand in for today; the step records who acted and under which delegation.
/// </summary>
public sealed class ApprovalService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    ExpenseReportDetailLoader loader,
    IApprovalNotifier notifier,
    IDelegationLookup delegations,
    ILogger<ApprovalService> logger) : IApprovalService
{
    // ---------- Inbox ----------

    public async Task<PagedResult<ApprovalInboxItemDto>> InboxAsync(ApprovalInboxQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var ordered = await InboxItemsAsync(query.View, query.Search, ct);
        var page = ordered.Skip(query.Skip).Take(query.NormalizedPageSize).ToList();
        return new PagedResult<ApprovalInboxItemDto>(page, ordered.Count, query.NormalizedPage, query.NormalizedPageSize);
    }

    public Task<IReadOnlyList<ApprovalInboxItemDto>> InboxExportAsync(ApprovalInboxView view, string? search, CancellationToken ct = default) =>
        InboxItemsAsync(view, search, ct);

    /// <summary>The whole inbox for a view, in display order (the inbox is small enough to order in memory).</summary>
    private async Task<IReadOnlyList<ApprovalInboxItemDto>> InboxItemsAsync(ApprovalInboxView view, string? search, CancellationToken ct)
    {
        var me = user.UserId;
        var pendingView = view == ApprovalInboxView.Pending;
        var actingFor = (await delegations.ActingForAsync(ct)).Keys.ToList();

        // Pending: my steps, plus the steps of signatories I stand in for today. Acted: steps I acted
        // on myself, whether mine or as a delegate (a step a delegate acted on is theirs, not mine).
        var mine =
            from s in db.ApprovalSteps.AsNoTracking()
            where pendingView
                ? s.ApproverUserId == me || actingFor.Contains(s.ApproverUserId)
                : (s.ApproverUserId == me && (s.ActedByUserId == null || s.ActedByUserId == me)) || s.ActedByUserId == me
            join r in db.ExpenseReports.AsNoTracking() on s.ReportId equals r.Id
            where pendingView
                ? s.Status == ApprovalStatus.Pending && s.Cycle == r.CurrentCycle && r.Status == ReportStatus.ForApproval
                : s.Status == ApprovalStatus.Approved || s.Status == ApprovalStatus.Returned || s.Status == ApprovalStatus.Rejected
            join e in db.Users.AsNoTracking() on r.EmployeeUserId equals e.Id
            join dj in db.Departments.AsNoTracking() on e.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            join a in db.Users.AsNoTracking() on s.ApproverUserId equals a.Id
            select new { Step = s, Report = r, EmployeeName = e.FullName, DepartmentName = d != null ? d.Name : null, SignatoryName = a.FullName };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            mine = mine.Where(x => EF.Functions.ILike(x.EmployeeName, term)
                || (x.Report.ReferenceNo != null && EF.Functions.ILike(x.Report.ReferenceNo, term))
                || (x.Report.Purpose != null && EF.Functions.ILike(x.Report.Purpose, term)));
        }

        var rows = await mine.ToListAsync(ct);
        if (pendingView)
        {
            // Same rule as the actions: an approver on a report never stands in for another step of it.
            var ownReports = rows.Where(x => x.Step.ApproverUserId == me).Select(x => x.Report.Id).ToHashSet();
            rows = rows
                .Where(x => x.Step.ApproverUserId == me || !ownReports.Contains(x.Report.Id))
                .GroupBy(x => x.Report.Id)
                .Select(g => g.OrderBy(x => x.Step.StepOrder).First())
                .ToList();
        }

        var reportIds = rows.Select(x => x.Report.Id).Distinct().ToList();

        var totals = await db.ExpenseLines.AsNoTracking()
            .Where(l => reportIds.Contains(l.ReportId) && l.IsActive)
            .GroupBy(l => l.ReportId)
            .Select(g => new { ReportId = g.Key, Total = g.Sum(l => l.TotalAmount) })
            .ToDictionaryAsync(x => x.ReportId, x => x.Total, ct);

        var currentSteps = await (
            from s in db.ApprovalSteps.AsNoTracking()
            where reportIds.Contains(s.ReportId) && s.Status == ApprovalStatus.Pending
            join r in db.ExpenseReports.AsNoTracking() on s.ReportId equals r.Id
            where s.Cycle == r.CurrentCycle
            join a in db.Users.AsNoTracking() on s.ApproverUserId equals a.Id
            select new { s.Id, s.ReportId, s.StepOrder, ApproverName = a.FullName })
            .ToListAsync(ct);
        var currentByReport = currentSteps.GroupBy(s => s.ReportId).ToDictionary(g => g.Key, g => g.OrderBy(s => s.StepOrder).First());

        var now = clock.UtcNow;
        var items = rows.Select(x =>
        {
            var current = currentByReport.GetValueOrDefault(x.Report.Id);
            var isMyTurn = pendingView && current is not null && current.Id == x.Step.Id;
            var since = pendingView ? x.Report.FiledAt : x.Step.ActedAt;
            return new ApprovalInboxItemDto(
                x.Report.Id, x.Report.ReferenceNo, x.Report.EmployeeUserId, x.EmployeeName, x.DepartmentName,
                x.Report.ReportType, x.Report.Purpose, x.Report.Status, totals.GetValueOrDefault(x.Report.Id),
                x.Report.FiledAt, x.Step.Cycle, x.Step.StepOrder, x.Step.SignatoryLevel, x.Step.Status, x.Step.ActedAt,
                current?.StepOrder, current?.ApproverName, isMyTurn,
                CanOverride: pendingView && !isMyTurn && x.Step.SignatoryLevel == 2,
                DaysWaiting: since is { } t ? Math.Max(0, (int)(now - t).TotalDays) : 0,
                OnBehalfOfName: x.Step.ApproverUserId != me ? x.SignatoryName : null);
        });

        // Oldest first for work to do; most recent first for history.
        return pendingView
            ? items.OrderByDescending(i => i.IsMyTurn).ThenBy(i => i.FiledAt).ToList()
            : items.OrderByDescending(i => i.MyActedAt).ToList();
    }

    // ---------- Actions ----------

    public Task<ApprovalActionResultDto> ApproveAsync(Guid reportId, ApprovalActionRequest request, CancellationToken ct = default) =>
        ActAsync(reportId, ApprovalAction.Approve, request, ct);

    public Task<ApprovalActionResultDto> ReturnAsync(Guid reportId, ApprovalActionRequest request, CancellationToken ct = default) =>
        ActAsync(reportId, ApprovalAction.Return, request, ct);

    public Task<ApprovalActionResultDto> RejectAsync(Guid reportId, ApprovalActionRequest request, CancellationToken ct = default) =>
        ActAsync(reportId, ApprovalAction.Reject, request, ct);

    public Task<ApprovalActionResultDto> OverrideAsync(Guid reportId, ApprovalActionRequest request, CancellationToken ct = default) =>
        ActAsync(reportId, ApprovalAction.Override, request, ct);

    private async Task<ApprovalActionResultDto> ActAsync(Guid reportId, ApprovalAction action, ApprovalActionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reason = Clean(request.Note);
        var actingFor = await delegations.ActingForAsync(ct);

        var outcome = await tx.ExecuteAsync(async token =>
        {
            var report = await db.ExpenseReports.FirstOrDefaultAsync(r => r.Id == reportId, token)
                ?? throw new NotFoundException("Expense report not found.");
            var steps = await db.ApprovalSteps
                .Where(s => s.ReportId == reportId && s.Cycle == report.CurrentCycle)
                .OrderBy(s => s.StepOrder)
                .ToListAsync(token);

            var decision = ApprovalWorkflow.Decide(report.Status, ToWorkflow(steps), report.EmployeeUserId, user.UserId, action, reason,
                actingFor.Keys.ToHashSet());
            ThrowIfRefused(decision);

            var acting = steps.First(s => s.Id == decision.ActingStepId);
            var onBehalfOf = acting.ApproverUserId != user.UserId
                ? await db.Users.AsNoTracking().Where(u => u.Id == acting.ApproverUserId).Select(u => u.FullName).FirstAsync(token)
                : null;
            var actor = onBehalfOf is null ? user.FullName : $"{user.FullName} on behalf of {onBehalfOf}";

            var now = clock.UtcNow;
            var signature = await db.Users.AsNoTracking().Where(u => u.Id == user.UserId).Select(u => u.Signature).FirstOrDefaultAsync(token);
            foreach (var step in steps.Where(s => decision.StepChanges.ContainsKey(s.Id)))
            {
                var status = decision.StepChanges[step.Id];
                var isActing = step.Id == decision.ActingStepId;
                step.Status = status;
                step.ActedAt = now;
                if (isActing)
                {
                    step.Note = reason;
                    step.ActedByUserId = user.UserId;
                    step.DelegationId = onBehalfOf is null ? null : actingFor[step.ApproverUserId];
                    if (status == ApprovalStatus.Approved)
                    {
                        step.Signature = signature;
                    }
                }
                else if (status == ApprovalStatus.Overridden)
                {
                    step.Note = $"Overridden by {actor}: {reason}";
                }
            }

            switch (decision.NewReportStatus)
            {
                case ReportStatus.Approved:
                    report.Status = ReportStatus.Approved;
                    report.ApprovedAt = now;
                    db.FinanceTrackings.Add(new FinanceTracking { ReportId = reportId, Status = FinanceStatus.Pending, CreatedAt = now, UpdatedAt = now });
                    break;
                case ReportStatus.Returned:
                case ReportStatus.Rejected:
                    report.Status = decision.NewReportStatus.Value;
                    report.CancelNote = reason;
                    break;
            }

            // Touching the report also enforces its row version, so a stale screen cannot act twice.
            report.UpdatedAt = now;
            db.SetRowVersion(report, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            var auditEvent = action switch
            {
                ApprovalAction.Return => ApprovalAuditEvents.Returned,
                ApprovalAction.Reject => ApprovalAuditEvents.Rejected,
                ApprovalAction.Override => ApprovalAuditEvents.Overridden,
                _ => decision.CompletesApproval ? ApprovalAuditEvents.FullyApproved : ApprovalAuditEvents.Approved,
            };
            await audit.WriteAsync(new WorkflowAuditEntry(auditEvent, reportId, user.UserId, JsonSerializer.Serialize(new
            {
                cycle = report.CurrentCycle,
                step = acting.StepOrder,
                level = acting.SignatoryLevel,
                onBehalfOfUserId = onBehalfOf is null ? (long?)null : acting.ApproverUserId,
                delegationId = acting.DelegationId,
                action = action.ToString(),
                reason,
                changes = decision.StepChanges.ToDictionary(c => steps.First(s => s.Id == c.Key).StepOrder.ToString(), c => c.Value.ToString()),
                reportStatus = report.Status.ToString(),
            })), token);

            var next = steps.Where(s => s.Status == ApprovalStatus.Pending).OrderBy(s => s.StepOrder).FirstOrDefault();
            string? nextName = null;
            if (next is not null)
            {
                nextName = await db.Users.AsNoTracking().Where(u => u.Id == next.ApproverUserId).Select(u => u.FullName).FirstAsync(token);
            }

            var employeeName = await db.Users.AsNoTracking().Where(u => u.Id == report.EmployeeUserId).Select(u => u.FullName).FirstAsync(token);
            var overridden = steps.Where(s => decision.StepChanges.TryGetValue(s.Id, out var st) && st == ApprovalStatus.Overridden).Select(s => s.ApproverUserId);
            var finance = decision.CompletesApproval
                ? await db.UserRoleGrants.AsNoTracking().Where(g => g.Role == UserRole.Finance && g.Status == RoleGrantStatus.Active).Select(g => g.UserId).ToListAsync(token)
                : [];
            var recipients = new List<long> { report.EmployeeUserId };
            if (onBehalfOf is not null)
            {
                recipients.Add(acting.ApproverUserId);
            }

            recipients.AddRange(overridden);
            recipients.AddRange(finance);
            if (next is not null)
            {
                recipients.Add(next.ApproverUserId);
            }

            var summary = action switch
            {
                ApprovalAction.Return => $"Returned by {actor}: {reason}",
                ApprovalAction.Reject => $"Rejected by {actor}: {reason}",
                ApprovalAction.Override => $"{actor} overrode the earlier step: {reason}",
                _ when decision.CompletesApproval => $"Approved by {actor}; sent to Finance for receipts",
                _ => $"Approved by {actor}; awaiting {nextName}",
            };
            var notification = new WorkflowEventNotification(auditEvent, reportId, report.ReferenceNo, employeeName,
                recipients.Where(id => id != user.UserId).Distinct().ToList(), summary,
                ActorName: actor, Note: reason, NextApproverName: nextName, Cycle: report.CurrentCycle, StepOrder: acting.StepOrder);
            return (report.Status, nextName, notification);
        }, ct);

        await NotifyAsync(outcome.notification, ct);
        var detail = await loader.LoadAsync(reportId, ct);
        return new ApprovalActionResultDto(reportId, outcome.Status, outcome.nextName, detail);
    }

    // ---------- Line adjustment ----------

    public async Task<ExpenseReportDetailDto> AdjustLineAsync(Guid reportId, long lineId, AdjustLineRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reason = Clean(request.Reason);
        var actingFor = (await delegations.ActingForAsync(ct)).Keys.ToHashSet();

        var notification = await tx.ExecuteAsync(async token =>
        {
            var report = await db.ExpenseReports.FirstOrDefaultAsync(r => r.Id == reportId, token)
                ?? throw new NotFoundException("Expense report not found.");
            var steps = await db.ApprovalSteps.AsNoTracking()
                .Where(s => s.ReportId == reportId && s.Cycle == report.CurrentCycle)
                .ToListAsync(token);

            var workflow = ToWorkflow(steps);
            if (ApprovalWorkflow.AdjustmentError(report.Status, workflow, report.EmployeeUserId, user.UserId, actingFor) is { } adjustError)
            {
                throw ApprovalWorkflow.ActorSteps(workflow, user.UserId, actingFor).Count > 0 && user.UserId != report.EmployeeUserId
                    ? new ConflictException(adjustError)
                    : new ForbiddenException(adjustError);
            }

            var line = await db.ExpenseLines.FirstOrDefaultAsync(l => l.Id == lineId && l.ReportId == reportId && l.IsActive, token)
                ?? throw new NotFoundException("Expense line not found.");
            if (ApprovalWorkflow.AdjustedAmountError(line.Amount, request.Amount, reason) is { } amountError)
            {
                throw new ValidationException("amount", amountError);
            }

            var before = new { line.Amount, line.TotalAmount };
            line.OriginalAmount ??= line.Amount;
            line.OriginalTotalAmount ??= line.TotalAmount;
            line.Amount = request.Amount;
            if (line.IsPerdiem && line.TotalDays is { } days)
            {
                var allowance = AllowanceCalculator.Compute(days, line.MinusDays ?? 0, request.Amount);
                line.Computation = allowance.ComputationText;
            }

            line.TotalAmount = ExpenseLineRules.Total(request.Amount, line.Multiplier, ExpenseLineRules.AllowsVat(line.InvoiceOrNo));
            var now = clock.UtcNow;
            line.AdjustedByUserId = user.UserId;
            line.AdjustedAt = now;
            line.AdjustmentReason = reason;
            line.UpdatedAt = now;
            report.UpdatedAt = now;
            db.SetRowVersion(line, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(ApprovalAuditEvents.LineAdjusted, reportId, user.UserId, JsonSerializer.Serialize(new
            {
                lineId,
                particulars = line.Particulars,
                before,
                after = new { line.Amount, line.TotalAmount },
                reason,
            })), token);

            var employeeName = await db.Users.AsNoTracking().Where(u => u.Id == report.EmployeeUserId).Select(u => u.FullName).FirstAsync(token);
            return new WorkflowEventNotification(ApprovalAuditEvents.LineAdjusted, reportId, report.ReferenceNo, employeeName,
                [report.EmployeeUserId], $"{user.FullName} lowered \"{line.Particulars}\" from {before.TotalAmount:N2} to {line.TotalAmount:N2}: {reason}");
        }, ct);

        await NotifyAsync(notification, ct);
        return await loader.LoadAsync(reportId, ct);
    }

    // ---------- Helpers ----------

    private static List<WorkflowStep> ToWorkflow(IEnumerable<ApprovalStep> steps) =>
        steps.Select(s => new WorkflowStep(s.Id, s.StepOrder, s.SignatoryLevel, s.ApproverUserId, s.Status)).ToList();

    private static void ThrowIfRefused(WorkflowDecision decision)
    {
        switch (decision.Refusal)
        {
            case WorkflowRefusal.None:
                return;
            case WorkflowRefusal.Forbidden:
                throw new ForbiddenException(decision.Reason!);
            case WorkflowRefusal.Conflict:
                throw new ConflictException(decision.Reason!);
            default:
                throw new ValidationException("note", decision.Reason!);
        }
    }

    private async Task NotifyAsync(WorkflowEventNotification notification, CancellationToken ct)
    {
        try
        {
            await notifier.NotifyWorkflowEventAsync(notification, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Workflow notification {Kind} failed for report {ReportId}", notification.Kind, notification.ReportId);
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
