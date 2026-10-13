using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Domain;
using ERFWeb.Domain.Finance;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Approvals;

/// <summary>
/// Finance physical-receipt queue (plan Phase 5). Approved reports land here as Pending. Finance
/// records the receipts (not received, incomplete, received once), clears the report for payment,
/// and sends follow-ups while receipts are outstanding. Finance never acts on its own report.
/// </summary>
public sealed class FinanceService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    IApprovalNotifier notifier,
    ILogger<FinanceService> logger) : IFinanceService
{
    public async Task<PagedResult<FinanceQueueItemDto>> QueueAsync(FinanceQueueQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var rows = Base();

        if (query.Status is { } status)
        {
            rows = rows.Where(x => x.Tracking.Status == status);
        }
        else if (query.OpenOnly)
        {
            rows = rows.Where(x => x.Tracking.Status != FinanceStatus.ClearedForPayment);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            rows = rows.Where(x => EF.Functions.ILike(x.EmployeeName, term)
                || (x.Report.ReferenceNo != null && EF.Functions.ILike(x.Report.ReferenceNo, term))
                || (x.Report.Purpose != null && EF.Functions.ILike(x.Report.Purpose, term)));
        }

        var total = await rows.CountAsync(ct);
        var page = await rows
            .OrderBy(x => x.Report.ApprovedAt)
            .Skip(query.Skip)
            .Take(query.NormalizedPageSize)
            .ToListAsync(ct);

        var items = await ProjectAsync(page, ct);
        return new PagedResult<FinanceQueueItemDto>(items, total, query.NormalizedPage, query.NormalizedPageSize);
    }

    public async Task<FinanceQueueItemDto> RecordReceiptsAsync(Guid reportId, RecordReceiptsRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var remarks = Clean(request.Remarks);

        var notification = await tx.ExecuteAsync(async token =>
        {
            var (tracking, report) = await LoadForActionAsync(reportId, token);
            if (FinanceRules.ReceiptTransitionError(tracking.Status, request.Status, remarks) is { } error)
            {
                // Already confirmed, or a repeat of the current status: a stale or duplicate action (409).
                throw tracking.Status is FinanceStatus.ReceiptsReceived or FinanceStatus.ClearedForPayment || tracking.Status == request.Status
                    ? new ConflictException(error)
                    : new ValidationException(request.Status == FinanceStatus.ReceiptsIncomplete ? "remarks" : "status", error);
            }

            var before = tracking.Status;
            var now = clock.UtcNow;
            tracking.Status = request.Status;
            // Remarks describe the status just recorded (e.g. what is missing); the audit trail keeps earlier ones.
            tracking.Remarks = remarks;
            if (request.Status == FinanceStatus.ReceiptsReceived)
            {
                tracking.PhysicalReceiptsReceived = true;
                tracking.ReceivedByUserId = user.UserId;
                tracking.ReceivedAt = now;
            }

            tracking.UpdatedAt = now;
            db.SetRowVersion(tracking, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(ApprovalAuditEvents.ReceiptsRecorded, reportId, user.UserId,
                JsonSerializer.Serialize(new { from = before.ToString(), to = request.Status.ToString(), remarks })), token);

            var label = request.Status switch
            {
                FinanceStatus.ReceiptsReceived => "All receipts received",
                FinanceStatus.ReceiptsIncomplete => $"Receipts incomplete: {remarks}",
                _ => "Receipts not received yet",
            };
            return await NotificationAsync(ApprovalAuditEvents.ReceiptsRecorded, report, label, token);
        }, ct);

        await NotifyAsync(notification, ct);
        return await GetAsync(reportId, ct);
    }

    public async Task<FinanceQueueItemDto> ClearAsync(Guid reportId, ClearForPaymentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var remarks = Clean(request.Remarks);

        var notification = await tx.ExecuteAsync(async token =>
        {
            var (tracking, report) = await LoadForActionAsync(reportId, token);
            if (FinanceRules.ClearError(tracking.Status, remarks) is { } error)
            {
                // Only the remarks length is a bad request; anything else is out of order (409).
                throw tracking.Status == FinanceStatus.ReceiptsReceived ? new ValidationException("remarks", error) : new ConflictException(error);
            }

            var now = clock.UtcNow;
            tracking.Status = FinanceStatus.ClearedForPayment;
            tracking.ClearedByUserId = user.UserId;
            tracking.ClearedAt = now;
            tracking.Remarks = remarks ?? tracking.Remarks;
            tracking.UpdatedAt = now;
            db.SetRowVersion(tracking, request.RowVersion);
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(ApprovalAuditEvents.Cleared, reportId, user.UserId,
                JsonSerializer.Serialize(new { remarks })), token);
            return await NotificationAsync(ApprovalAuditEvents.Cleared, report, "Cleared for payment", token);
        }, ct);

        await NotifyAsync(notification, ct);
        return await GetAsync(reportId, ct);
    }

    public async Task<FinanceQueueItemDto> FollowUpAsync(Guid reportId, FinanceFollowUpRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var message = Clean(request.Message);
        if (FinanceRules.RemarksError(message) is { } tooLong)
        {
            throw new ValidationException("message", tooLong);
        }

        var notification = await tx.ExecuteAsync(async token =>
        {
            var (tracking, report) = await LoadForActionAsync(reportId, token);
            if (!FinanceRules.AwaitsReceipts(tracking.Status))
            {
                throw new ConflictException("The receipts are no longer outstanding, so there is nothing to follow up.");
            }

            var now = clock.UtcNow;
            tracking.FollowUpCount += 1;
            tracking.LastFollowUpAt = now;
            tracking.UpdatedAt = now;
            await db.SaveWithConcurrencyAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry(ApprovalAuditEvents.FollowUp, reportId, user.UserId,
                JsonSerializer.Serialize(new { count = tracking.FollowUpCount, message })), token);
            return await NotificationAsync(ApprovalAuditEvents.FollowUp, report,
                message ?? "Please submit the original receipts for this report to Finance", token);
        }, ct);

        await NotifyAsync(notification, ct);
        return await GetAsync(reportId, ct);
    }

    // ---------- Helpers ----------

    private sealed class Row
    {
        public required FinanceTracking Tracking { get; init; }
        public required ExpenseReport Report { get; init; }
        public required string EmployeeName { get; init; }
        public string? DepartmentName { get; init; }
    }

    private IQueryable<Row> Base() =>
        from f in db.FinanceTrackings.AsNoTracking()
        join r in db.ExpenseReports.AsNoTracking() on f.ReportId equals r.Id
        where r.Status == ReportStatus.Approved
        join e in db.Users.AsNoTracking() on r.EmployeeUserId equals e.Id
        join dj in db.Departments.AsNoTracking() on e.DepartmentId equals (long?)dj.Id into ds
        from d in ds.DefaultIfEmpty()
        select new Row { Tracking = f, Report = r, EmployeeName = e.FullName, DepartmentName = d != null ? d.Name : null };

    private async Task<FinanceQueueItemDto> GetAsync(Guid reportId, CancellationToken ct)
    {
        var row = await Base().Where(x => x.Report.Id == reportId).ToListAsync(ct);
        return (await ProjectAsync(row, ct)).FirstOrDefault() ?? throw new NotFoundException("The report is not in the Finance queue.");
    }

    private async Task<IReadOnlyList<FinanceQueueItemDto>> ProjectAsync(IReadOnlyList<Row> rows, CancellationToken ct)
    {
        var ids = rows.Select(r => r.Report.Id).ToList();
        var totals = await db.ExpenseLines.AsNoTracking()
            .Where(l => ids.Contains(l.ReportId) && l.IsActive)
            .GroupBy(l => l.ReportId)
            .Select(g => new { g.Key, Total = g.Sum(l => l.TotalAmount) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);
        var attachments = await db.ReceiptAttachments.AsNoTracking()
            .Where(a => ids.Contains(a.ReportId))
            .GroupBy(a => a.ReportId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var versions = await db.FinanceTrackings.AsNoTracking()
            .Where(f => ids.Contains(f.ReportId))
            .Select(f => new { f.ReportId, RowVersion = EF.Property<uint>(f, DbContextExtensions.RowVersionProperty) })
            .ToDictionaryAsync(x => x.ReportId, x => x.RowVersion, ct);
        var peopleIds = rows.SelectMany(r => new[] { r.Tracking.ReceivedByUserId, r.Tracking.ClearedByUserId }).OfType<long>().Distinct().ToList();
        var people = await db.Users.AsNoTracking().Where(u => peopleIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var now = clock.UtcNow;
        return rows.Select(x => new FinanceQueueItemDto(
            x.Report.Id, x.Report.ReferenceNo, x.Report.EmployeeUserId, x.EmployeeName, x.DepartmentName, x.Report.ReportType,
            x.Report.Purpose, totals.GetValueOrDefault(x.Report.Id), x.Report.ApprovedAt, x.Tracking.Status, x.Tracking.Remarks,
            x.Tracking.ReceivedByUserId is { } rb ? people.GetValueOrDefault(rb) : null, x.Tracking.ReceivedAt,
            x.Tracking.ClearedByUserId is { } cb ? people.GetValueOrDefault(cb) : null, x.Tracking.ClearedAt,
            x.Tracking.FollowUpCount, x.Tracking.LastFollowUpAt, attachments.GetValueOrDefault(x.Report.Id),
            x.Report.ApprovedAt is { } at ? Math.Max(0, (int)(now - at).TotalDays) : 0,
            IsOwnReport: x.Report.EmployeeUserId == user.UserId,
            versions.GetValueOrDefault(x.Report.Id))).ToList();
    }

    private async Task<(FinanceTracking Tracking, ExpenseReport Report)> LoadForActionAsync(Guid reportId, CancellationToken ct)
    {
        var report = await db.ExpenseReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new NotFoundException("Expense report not found.");
        if (report.EmployeeUserId == user.UserId)
        {
            throw new ForbiddenException("You cannot process the receipts of your own report. Another Finance user must do it.");
        }

        if (report.Status != ReportStatus.Approved)
        {
            throw new ConflictException("Only approved reports are in the Finance queue.");
        }

        var tracking = await db.FinanceTrackings.FirstOrDefaultAsync(f => f.ReportId == reportId, ct)
            ?? throw new NotFoundException("The report is not in the Finance queue.");
        return (tracking, report);
    }

    private async Task<WorkflowEventNotification> NotificationAsync(string kind, ExpenseReport report, string summary, CancellationToken ct)
    {
        var employeeName = await db.Users.AsNoTracking().Where(u => u.Id == report.EmployeeUserId).Select(u => u.FullName).FirstAsync(ct);
        return new WorkflowEventNotification(kind, report.Id, report.ReferenceNo, employeeName, [report.EmployeeUserId], summary);
    }

    private async Task NotifyAsync(WorkflowEventNotification notification, CancellationToken ct)
    {
        try
        {
            await notifier.NotifyWorkflowEventAsync(notification, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Finance notification {Kind} failed for report {ReportId}", notification.Kind, notification.ReportId);
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
