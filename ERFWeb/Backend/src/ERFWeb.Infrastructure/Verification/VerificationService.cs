using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Application.Features.Verification;
using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Scope;
using ERFWeb.Domain.Verification;
using ERFWeb.Infrastructure.Entitlements;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Verification;

/// <summary>
/// Verification requests and queries (plan Phase 6, Flow 6). Each action runs in one serializable
/// transaction with its <c>workflow_audit</c> row and the request's row version (stale → 409);
/// notifications go out after commit. A Level 2 decision to return or reject is a post-approval
/// action: it changes the report status, drops it from the Finance queue, and closes its other
/// requests in the same transaction. Notifications (SMS and email) go only to the people each step
/// names; picking a request up notifies nobody.
/// </summary>
public sealed class VerificationService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    VerificationReader reader,
    ExpenseReportDetailLoader loader,
    EntitlementContext entitlements,
    IApprovalNotifier notifier,
    ILogger<VerificationService> logger) : IVerificationService
{
    // ---------- Finance raises ----------

    public async Task<ExpenseReportDetailDto> RaiseAsync(RaiseVerificationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reason = Clean(request.Reason);

        var notification = await tx.ExecuteAsync(async token =>
        {
            var report = await db.ExpenseReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.ReportId, token)
                ?? throw new NotFoundException("Expense report not found.");
            var line = await db.ExpenseLines.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == request.LineId && l.ReportId == report.Id && l.IsActive, token)
                ?? throw new NotFoundException("Expense line not found.");
            var finance = await db.FinanceTrackings.AsNoTracking()
                .Where(f => f.ReportId == report.Id).Select(f => (FinanceStatus?)f.Status).FirstOrDefaultAsync(token);
            var lineBusy = await db.VerificationRequests.AnyAsync(v => v.ExpenseLineId == line.Id && v.Status != VerificationStatus.Closed, token);

            Throw(VerificationRules.CanRaise(report.Status, finance, report.EmployeeUserId, user.UserId, lineBusy, reason), "reason");

            var now = clock.UtcNow;
            var entity = new VerificationRequest
            {
                ReportId = report.Id,
                ExpenseLineId = line.Id,
                Reason = reason!,
                Status = VerificationStatus.Open,
                RequestedByUserId = user.UserId,
                RequestedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.VerificationRequests.Add(entity);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(VerificationAuditEvents.Requested, report.Id, user.UserId,
                JsonSerializer.Serialize(new { requestId = entity.Id, lineId = line.Id, particulars = line.Particulars, reason })), token);

            // Only verifiers who may take it (9.16): the claimant hears as claimant, and an approver of the report is left out.
            var approvers = await reader.ApproversAsync(report.Id, token);
            var verifiers = (await ActiveHoldersAsync(UserRole.ExpenseVerifier, token)).Where(id => !approvers.Contains(id));
            return await NotificationAsync(VerificationAuditEvents.Requested, entity.Id, report, [report.EmployeeUserId, .. verifiers],
                $"Finance asked for \"{line.Particulars}\" to be verified. Clearance is paused until it is resolved.", token);
        }, ct);

        await NotifyAsync(notification, ct);
        return await loader.LoadAsync(request.ReportId, ct);
    }

    // ---------- Verifier ----------

    public async Task<PagedResult<VerificationQueueItemDto>> QueueAsync(VerificationQueueQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var me = user.UserId;
        var requests = db.VerificationRequests.AsNoTracking();
        requests = query.View switch
        {
            VerificationQueueView.Mine => requests.Where(v => v.VerifierUserId == me && v.Status == VerificationStatus.InProgress),
            VerificationQueueView.Done => requests.Where(v => v.VerifierUserId == me && v.Finding != null),
            _ => requests.Where(v => v.Status == VerificationStatus.Open),
        };

        var items = await reader.ItemsAsync(requests, ct);
        if (query.View == VerificationQueueView.Open)
        {
            // The pool hides requests the viewer may not take (9.16), so nobody is offered their own.
            var reportIds = items.Select(i => i.ReportId).Distinct().ToList();
            var claimants = await db.ExpenseReports.AsNoTracking().Where(r => reportIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.EmployeeUserId, ct);
            var approvedBy = await db.ApprovalSteps.AsNoTracking()
                .Where(s => reportIds.Contains(s.ReportId) && s.Status == ApprovalStatus.Approved && (s.ApproverUserId == me || s.ActedByUserId == me))
                .Select(s => s.ReportId).Distinct().ToListAsync(ct);
            var requesters = await db.VerificationRequests.AsNoTracking()
                .Where(v => v.Status == VerificationStatus.Open && v.RequestedByUserId == me).Select(v => v.Id).ToListAsync(ct);
            items = items.Where(i => claimants.GetValueOrDefault(i.ReportId) != me && !approvedBy.Contains(i.ReportId) && !requesters.Contains(i.Id)).ToList();
        }

        var ordered = query.View == VerificationQueueView.Done
            ? items.OrderByDescending(i => i.FoundAt).ToList()
            : items.OrderBy(i => i.RequestedAt).ToList();
        var page = ordered.Skip(query.Skip).Take(query.NormalizedPageSize).ToList();
        return new PagedResult<VerificationQueueItemDto>(page, ordered.Count, query.NormalizedPage, query.NormalizedPageSize);
    }

    public async Task<ExpenseReportDetailDto> PickUpAsync(long id, PickUpVerificationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reportId = await tx.ExecuteAsync(async token =>
        {
            var (v, report) = await LoadAsync(id, token);
            var approvers = await reader.ApproversAsync(report.Id, token);
            Throw(VerificationRules.CanPickUp(VerificationReader.ToState(v, report.EmployeeUserId), approvers, user.UserId), "request");

            var now = clock.UtcNow;
            v.Status = VerificationStatus.InProgress;
            v.VerifierUserId = user.UserId;
            v.PickedUpAt = now;
            v.UpdatedAt = now;
            db.SetRowVersion(v, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(VerificationAuditEvents.PickedUp, report.Id, user.UserId,
                JsonSerializer.Serialize(new { requestId = v.Id })), token);
            return report.Id;
        }, ct);

        return await loader.LoadAsync(reportId, ct);
    }

    public async Task<ExpenseReportDetailDto> RecordFindingAsync(long id, RecordFindingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var note = Clean(request.Note);

        var (reportId, notification) = await tx.ExecuteAsync(async token =>
        {
            var (v, report) = await LoadAsync(id, token);
            Throw(VerificationRules.CanRecordFinding(VerificationReader.ToState(v, report.EmployeeUserId), user.UserId, request.Finding, note), "note");

            var now = clock.UtcNow;
            v.Finding = request.Finding;
            v.FindingNote = note;
            v.FoundAt = now;
            v.Status = VerificationRules.StatusAfterFinding(request.Finding);
            if (v.Status == VerificationStatus.Closed)
            {
                v.ClosedAt = now;
            }

            v.UpdatedAt = now;
            db.SetRowVersion(v, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(VerificationAuditEvents.Found, report.Id, user.UserId,
                JsonSerializer.Serialize(new { requestId = v.Id, finding = request.Finding.ToString(), note })), token);

            // Finance hears every finding; the claimant only hears that the check is over.
            List<long> recipients = [v.RequestedByUserId];
            if (v.Status == VerificationStatus.Closed)
            {
                recipients.Add(report.EmployeeUserId);
            }

            var summary = request.Finding == VerificationFinding.NotVerified
                ? "The verifier could not verify a line. Finance will refer it to the Level 2 Signatory."
                : $"Verification completed: {(request.Finding == VerificationFinding.Verified ? "verified" : "verified with a note")}.";
            return (report.Id, await NotificationAsync(VerificationAuditEvents.Found, v.Id, report, recipients, summary, token));
        }, ct);

        await NotifyAsync(notification, ct);
        return await loader.LoadAsync(reportId, ct);
    }

    // ---------- Finance queries Level 2 ----------

    public async Task<ExpenseReportDetailDto> QueryAsync(long id, RaiseQueryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var note = Clean(request.Note);

        var (reportId, notification) = await tx.ExecuteAsync(async token =>
        {
            var (v, report) = await LoadAsync(id, token);
            Throw(VerificationRules.CanQuery(VerificationReader.ToState(v, report.EmployeeUserId), user.UserId, note), "note");

            var level2 = await Level2ForAsync(report, token)
                ?? throw new ConflictException("No Level 2 Signatory covers this employee. Set up the Level 2 scope first.");

            var now = clock.UtcNow;
            v.QueryToUserId = level2;
            v.QueriedByUserId = user.UserId;
            v.QueryNote = note;
            v.QueriedAt = now;
            v.Status = VerificationStatus.Queried;
            v.UpdatedAt = now;
            db.SetRowVersion(v, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(VerificationAuditEvents.QueryRaised, report.Id, user.UserId,
                JsonSerializer.Serialize(new { requestId = v.Id, queryTo = level2, note })), token);
            return (report.Id, await NotificationAsync(VerificationAuditEvents.QueryRaised, v.Id, report, [level2],
                $"Finance needs your decision on a line that could not be verified: {note}", token));
        }, ct);

        await NotifyAsync(notification, ct);
        return await loader.LoadAsync(reportId, ct);
    }

    // ---------- Level 2 decides ----------

    public async Task<PagedResult<VerificationQueueItemDto>> QueriesAsync(QueryListQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var me = user.UserId;
        var requests = db.VerificationRequests.AsNoTracking().Where(v => v.QueryToUserId == me);
        requests = query.View == QueryListView.Pending
            ? requests.Where(v => v.Status == VerificationStatus.Queried)
            : requests.Where(v => v.Disposition != null);

        var items = await reader.ItemsAsync(requests, ct);
        var ordered = query.View == QueryListView.Pending
            ? items.OrderBy(i => i.QueriedAt).ToList()
            : items.OrderByDescending(i => i.DecidedAt).ToList();
        var page = ordered.Skip(query.Skip).Take(query.NormalizedPageSize).ToList();
        return new PagedResult<VerificationQueueItemDto>(page, ordered.Count, query.NormalizedPage, query.NormalizedPageSize);
    }

    public async Task<ExpenseReportDetailDto> DecideAsync(long id, DecideQueryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var note = Clean(request.Note);

        var (reportId, notification) = await tx.ExecuteAsync(async token =>
        {
            var v = await db.VerificationRequests.FirstOrDefaultAsync(x => x.Id == id, token)
                ?? throw new NotFoundException("Verification request not found.");
            var report = await db.ExpenseReports.FirstAsync(r => r.Id == v.ReportId, token);
            Throw(VerificationRules.CanDecide(VerificationReader.ToState(v, report.EmployeeUserId), user.UserId, request.Disposition, note), "note");

            var now = clock.UtcNow;
            v.Disposition = request.Disposition;
            v.DispositionNote = note;
            v.DecidedByUserId = user.UserId;
            v.DecidedAt = now;
            v.Status = VerificationStatus.Closed;
            v.ClosedAt = now;
            v.UpdatedAt = now;
            db.SetRowVersion(v, request.RowVersion);

            if (request.Disposition != QueryDisposition.Resume)
            {
                if (report.Status != ReportStatus.Approved)
                {
                    throw new ConflictException("The report is no longer approved, so it cannot be returned or rejected from here.");
                }

                // Post-approval return or reject: the report leaves the Finance queue (a returned report
                // is tracked again when it is re-approved), and its other requests are closed with it.
                var returned = request.Disposition == QueryDisposition.Return;
                report.Status = returned ? ReportStatus.Returned : ReportStatus.Rejected;
                report.CancelNote = note;
                if (returned)
                {
                    report.ApprovedAt = null;
                }

                report.UpdatedAt = now;
                var tracking = await db.FinanceTrackings.FirstOrDefaultAsync(f => f.ReportId == report.Id, token);
                if (tracking is not null)
                {
                    db.FinanceTrackings.Remove(tracking);
                }

                var others = await db.VerificationRequests
                    .Where(x => x.ReportId == report.Id && x.Id != v.Id && x.Status != VerificationStatus.Closed)
                    .ToListAsync(token);
                foreach (var other in others)
                {
                    other.Status = VerificationStatus.Closed;
                    other.Disposition = request.Disposition;
                    other.DispositionNote = $"Closed because the report was {(returned ? "returned" : "rejected")} by the Level 2 Signatory.";
                    other.DecidedByUserId = user.UserId;
                    other.DecidedAt = now;
                    other.ClosedAt = now;
                    other.UpdatedAt = now;
                }
            }

            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(VerificationAuditEvents.QueryDecided, report.Id, user.UserId, JsonSerializer.Serialize(new
            {
                requestId = v.Id,
                disposition = request.Disposition.ToString(),
                note,
                reportStatus = report.Status.ToString(),
            })), token);

            var summary = request.Disposition switch
            {
                QueryDisposition.Return => $"Returned by {user.FullName} after verification: {note}",
                QueryDisposition.Reject => $"Rejected by {user.FullName} after verification: {note}",
                _ => $"{user.FullName} let the line through; Finance will carry on.",
            };
            List<long> recipients = [report.EmployeeUserId, v.RequestedByUserId];
            if (v.VerifierUserId is { } verifier)
            {
                recipients.Add(verifier);
            }

            return (report.Id, await NotificationAsync(VerificationAuditEvents.QueryDecided, v.Id, report, recipients, summary, token));
        }, ct);

        await NotifyAsync(notification, ct);
        return await loader.LoadAsync(reportId, ct);
    }

    // ---------- Helpers ----------

    private async Task<(VerificationRequest Request, ExpenseReport Report)> LoadAsync(long id, CancellationToken ct)
    {
        var v = await db.VerificationRequests.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Verification request not found.");
        var report = await db.ExpenseReports.AsNoTracking().FirstAsync(r => r.Id == v.ReportId, ct);
        return (v, report);
    }

    /// <summary>
    /// The Level 2 to query: the one who approved the report, or else whoever holds Level 2 scope
    /// over the employee today (for a route that had no Level 2 step). Never the claimant.
    /// </summary>
    private async Task<long?> Level2ForAsync(ExpenseReport report, CancellationToken ct)
    {
        var approved = await db.ApprovalSteps.AsNoTracking()
            .Where(s => s.ReportId == report.Id && s.Cycle == report.CurrentCycle && s.SignatoryLevel == 2 && s.Status == ApprovalStatus.Approved)
            .Select(s => (long?)s.ApproverUserId)
            .FirstOrDefaultAsync(ct);
        if (approved is { } id && id != report.EmployeeUserId)
        {
            return id;
        }

        var subject = await entitlements.SubjectAsync(report.EmployeeUserId, ct);
        if (subject is null)
        {
            return null;
        }

        var candidates = await entitlements.ScopeCandidatesAsync(validHoldersOnly: true, ct);
        var holder = ScopeRules.Resolve(candidates, subject.Scope, 2, entitlements.Today).HolderUserId;
        return holder is { } h && h != report.EmployeeUserId ? h : null;
    }

    private Task<List<long>> ActiveHoldersAsync(UserRole role, CancellationToken ct) =>
        db.UserRoleGrants.AsNoTracking()
            .Where(g => g.Role == role && g.Status == RoleGrantStatus.Active)
            .Select(g => g.UserId)
            .ToListAsync(ct);

    /// <summary>
    /// Addressed to <paramref name="recipients"/> (never the actor), who are texted and emailed. Each event
    /// happens once per request, so the kind and request id make the delivery key.
    /// </summary>
    private async Task<WorkflowEventNotification> NotificationAsync(
        string kind, long requestId, ExpenseReport report, IEnumerable<long> recipients, string summary, CancellationToken ct)
    {
        var employeeName = await db.Users.AsNoTracking().Where(u => u.Id == report.EmployeeUserId).Select(u => u.FullName).FirstAsync(ct);
        return new WorkflowEventNotification(kind, report.Id, report.ReferenceNo, employeeName,
            recipients.Where(id => id != user.UserId).Distinct().ToList(), summary, DeliveryKey: $"{kind}:{requestId}");
    }

    private async Task NotifyAsync(WorkflowEventNotification notification, CancellationToken ct)
    {
        try
        {
            await notifier.NotifyWorkflowEventAsync(notification, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Verification notification {Kind} failed for report {ReportId}", notification.Kind, notification.ReportId);
        }
    }

    private static void Throw(VerificationCheck check, string field)
    {
        switch (check.Refusal)
        {
            case WorkflowRefusal.None:
                return;
            case WorkflowRefusal.Forbidden:
                throw new ForbiddenException(check.Reason!);
            case WorkflowRefusal.Conflict:
                throw new ConflictException(check.Reason!);
            default:
                throw new ValidationException(field, check.Reason!);
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
