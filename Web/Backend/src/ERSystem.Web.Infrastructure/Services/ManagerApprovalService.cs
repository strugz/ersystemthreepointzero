using System.Data;
using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.ManagerApprovals;
using ERSystem.Web.Application.Features.ReportReview;
using ERSystem.Web.Domain.Common;
using ERSystem.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERSystem.Web.Infrastructure.Services;

public sealed class ManagerApprovalService(
    IDbContextFactory<LegacyErDbContext> contextFactory,
    IRowVersionCodec rowVersions,
    IClock clock,
    IWorkflowAuditWriter auditWriter,
    IReportAuthorizationService authorization) : IManagerApprovalService
{
    public async Task<PagedResult<ManagerReportListItemDto>> GetReportsAsync(
        int managerUserId, ManagerReportQuery query, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var pending = !string.Equals(query.Status, "processed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(query.Status, "completed", StringComparison.OrdinalIgnoreCase);

        var source =
            from approval in db.ApprovalTransactions.AsNoTracking()
            join report in db.Reports.AsNoTracking() on approval.ReportId equals report.Id
            join user in db.Users.AsNoTracking() on report.UserId equals user.UserId
            join department in db.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            where approval.ApproverUserId == managerUserId &&
                  !db.ApprovalTransactions.Any(laterCycle =>
                      laterCycle.ReportId == approval.ReportId && laterCycle.ApprovalCycle > approval.ApprovalCycle)
            select new { report, approval, user, department };

        if (pending)
        {
            source = source.Where(x =>
                x.approval.Status == ApprovalTransactionStates.Pending &&
                x.report.ReportFileStatus == ReportStates.Filed &&
                !db.ApprovalTransactions.Any(previous =>
                    previous.ReportId == x.approval.ReportId && previous.ApprovalCycle == x.approval.ApprovalCycle &&
                    previous.StepOrder < x.approval.StepOrder && previous.Status != ApprovalTransactionStates.Approved));
        }
        else
        {
            source = source.Where(x =>
                x.approval.Status == ApprovalTransactionStates.Approved ||
                x.approval.Status == ApprovalTransactionStates.Returned);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x =>
                (x.user.FullName != null && x.user.FullName.Contains(search)) ||
                x.report.Id.Contains(search) ||
                (x.report.ErfReferenceNumber != null && x.report.ErfReferenceNumber.Contains(search)) ||
                (x.report.ReportDescription != null && x.report.ReportDescription.Contains(search)));
        }
        if (query.DepartmentId.HasValue) source = source.Where(x => x.user.DepartmentId == query.DepartmentId);
        if (!string.IsNullOrWhiteSpace(query.ReportType))
        {
            var reportType = query.ReportType.Trim();
            source = source.Where(x => x.report.ReportType != null && x.report.ReportType.Trim() == reportType);
        }
        if (query.DateFrom.HasValue) source = source.Where(x => x.report.ReportDateFrom >= query.DateFrom);
        if (query.DateTo.HasValue) source = source.Where(x => x.report.ReportDateTo <= query.DateTo);

        var orderedSource = query.SortBy?.ToLowerInvariant() switch
        {
            "erfreferencenumber" or "erfreference" or "report" => query.SortDirection == SortDirection.Descending
                ? source.OrderByDescending(x => x.report.ErfReferenceNumber).ThenBy(x => x.report.Id)
                : source.OrderBy(x => x.report.ErfReferenceNumber).ThenBy(x => x.report.Id),
            "employee" or "employeename" => query.SortDirection == SortDirection.Descending
                ? source.OrderByDescending(x => x.user.FullName).ThenBy(x => x.report.Id)
                : source.OrderBy(x => x.user.FullName).ThenBy(x => x.report.Id),
            "datefrom" => query.SortDirection == SortDirection.Ascending
                ? source.OrderBy(x => x.report.ReportDateFrom).ThenBy(x => x.report.Id)
                : source.OrderByDescending(x => x.report.ReportDateFrom).ThenBy(x => x.report.Id),
            _ => source.OrderByDescending(x => x.report.ReportDateFrom).ThenBy(x => x.report.Id)
        };
        var rows = await orderedSource
            .Select(x => new
            {
                x.report.Id,
                ErfReferenceNumber = x.report.ErfReferenceNumber ?? string.Empty,
                EmployeeUserId = x.user.UserId ?? 0,
                EmployeeName = x.user.FullName ?? string.Empty,
                Department = x.department != null ? x.department.Name ?? string.Empty : string.Empty,
                x.report.ReportDateFrom,
                x.report.ReportDateTo,
                Description = x.report.ReportDescription ?? string.Empty,
                ReportType = x.report.ReportType ?? string.Empty,
                CurrentStep = x.approval.StepOrder,
                TotalSteps = db.ApprovalTransactions.Count(a =>
                    a.ReportId == x.approval.ReportId && a.ApprovalCycle == x.approval.ApprovalCycle),
                x.report.ReportFileStatus,
                x.report.ReportPrintStatus,
                x.report.RowVersion
            }).ToListAsync(cancellationToken);

        var items = rows.Select(x => new ManagerReportListItemDto(
            x.Id, x.ErfReferenceNumber.Trim(), x.EmployeeUserId, x.EmployeeName.Trim(), x.Department.Trim(),
            x.ReportDateFrom, x.ReportDateTo,
            x.Description.Trim(), TextNormalization.TrimLegacy(x.ReportType), x.CurrentStep, x.TotalSteps,
            ResolveReportStatus(x.ReportFileStatus, x.ReportPrintStatus), rowVersions.Encode(x.RowVersion))).ToArray();
        return items.ToInMemoryPagedResult(query);
    }

    public async Task<ManagerReportDetailDto> GetReportAsync(int managerUserId, string reportId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var header = await (
            from approval in db.ApprovalTransactions.AsNoTracking()
            join report in db.Reports.AsNoTracking() on approval.ReportId equals report.Id
            join user in db.Users.AsNoTracking() on report.UserId equals user.UserId
            join department in db.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            where report.Id == reportId && approval.ApproverUserId == managerUserId &&
                  approval.Status != ApprovalTransactionStates.Superseded
            orderby approval.ApprovalCycle descending
            select new { report, approval, user, department }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("The report was not found or is not assigned to the current manager.");

        var review = await ReportReviewDataLoader.LoadAsync(
            db, reportId, header.approval.ApprovalCycle, cancellationToken);

        return new ManagerReportDetailDto(
            header.report.Id, header.user.UserId ?? 0, header.user.FullName?.Trim() ?? string.Empty,
            header.department?.Name?.Trim() ?? string.Empty, header.report.ReportDateFrom, header.report.ReportDateTo,
            header.report.ReportDescription?.Trim() ?? string.Empty, TextNormalization.TrimLegacy(header.report.ReportType),
            header.report.ErfReferenceNumber?.Trim() ?? string.Empty, review.Expenses, review.CashAdvance,
            review.Attachments, review.ApprovalTrail, header.approval.StepOrder, review.ApprovalTrail.Count,
            ResolveReportStatus(header.report.ReportFileStatus, header.report.ReportPrintStatus),
            rowVersions.Encode(header.report.RowVersion));
    }

    public async Task<AttachmentContentDto> GetAttachmentAsync(int managerUserId, int attachmentId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var attachment = await db.ScannedReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken)
            ?? throw new NotFoundException("The receipt attachment was not found.");
        await authorization.EnsureManagerCanAccessAsync(managerUserId, attachment.ReportId, cancellationToken);
        return new AttachmentContentDto(attachment.OriginalFileName, attachment.ContentType, attachment.ReceiptContent);
    }

    public async Task<WorkflowActionResult> ApproveAsync(
        int managerUserId, string reportId, ApproveReportRequest request, Guid correlationId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var report = await db.Reports.SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken)
                ?? throw new NotFoundException("The report was not found.");
            if (!rowVersions.Matches(report.RowVersion, request.RowVersion)) throw new ConflictException("The report changed. Refresh and try again.");
            if (report.ReportFileStatus != ReportStates.Filed) throw new ConflictException("The report is no longer pending approval.");

            var approval = await db.ApprovalTransactions
                .Where(x => x.ReportId == reportId && x.ApproverUserId == managerUserId)
                .OrderByDescending(x => x.ApprovalCycle).FirstOrDefaultAsync(cancellationToken)
                ?? throw new ForbiddenException("The current manager has no approval transaction for this report.");
            if (approval.Status != ApprovalTransactionStates.Pending)
                throw new ConflictException("This approval transaction is no longer pending.");
            var previousIncomplete = await db.ApprovalTransactions.AsNoTracking().AnyAsync(x =>
                x.ReportId == reportId && x.ApprovalCycle == approval.ApprovalCycle &&
                x.StepOrder < approval.StepOrder && x.Status != ApprovalTransactionStates.Approved, cancellationToken);
            if (previousIncomplete)
                throw new ConflictException("A previous approver must complete the report first.");

            var previousState = ResolveReportStatus(report.ReportFileStatus, report.ReportPrintStatus);
            var newNumber = (report.ReportNumberStatus ?? 0) + 1;
            var isFinal = !await db.ApprovalTransactions.AsNoTracking().AnyAsync(x =>
                x.ReportId == reportId && x.ApprovalCycle == approval.ApprovalCycle &&
                x.StepOrder > approval.StepOrder, cancellationToken);
            report.ReportNumberStatus = newNumber;
            report.ReportEndorseStatus = ReportStates.EndorseApproved;
            approval.Status = ApprovalTransactionStates.Approved;
            approval.ActionedAtUtc = clock.UtcNow;
            approval.ActionRemarks = null;

            if (!isFinal)
            {
                report.ReportPrintStatus = ReportStates.Filed;
                report.ReportFileStatus = ReportStates.Filed;
                report.ReportReserveStatus1 = managerUserId.ToString();
                db.ReportAuthorities.Add(new ReportAuthorityEntity
                {
                    ReportId = reportId,
                    SignId = managerUserId,
                    UserId = report.UserId,
                    AuthoritySignature = (await db.Users.AsNoTracking().SingleAsync(x => x.UserId == managerUserId, cancellationToken)).Signature
                });
            }
            else
            {
                report.ReportPrintStatus = ReportStates.Approved;
                report.ReportFileStatus = ReportStates.Approved;
                report.ReportReserveStatus2 = managerUserId.ToString();
                var finance = await db.FinanceTracking.SingleOrDefaultAsync(x => x.ReportId == reportId, cancellationToken);
                if (finance is null)
                {
                    finance = new ReportFinanceTrackingEntity { ReportId = reportId, FinanceStatus = FinanceStates.Pending };
                    db.FinanceTracking.Add(finance);
                }
                finance.ScannedReceiptsDeletedDate ??= clock.UtcNow;
                report.ReportAttachment = string.Empty;
            }

            var audit = CreateAudit(reportId, managerUserId, WorkflowEvents.ManagerApproved, previousState,
                isFinal ? "Approved" : "For Approval", null, correlationId);
            await db.SaveChangesAsync(cancellationToken);
            await auditWriter.WriteAsync(db.Database.GetDbConnection(), transaction.GetDbTransaction(), audit, cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_Notify {reportId}, {(isFinal ? "DONE" : "FILE")}", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new WorkflowActionResult(reportId, isFinal ? "Approved" : "For Approval", rowVersions.Encode(report.RowVersion));
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The report changed while it was being approved. Refresh and try again.");
        }
    }

    public async Task<WorkflowActionResult> ReturnAsync(
        int managerUserId, string reportId, ReturnReportRequest request, Guid correlationId, CancellationToken cancellationToken)
    {
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length is < 1 or > 1000)
            throw new ValidationException("A return reason between 1 and 1000 characters is required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var report = await db.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken)
            ?? throw new NotFoundException("The report was not found.");
        if (!rowVersions.Matches(report.RowVersion, request.RowVersion)) throw new ConflictException("The report changed. Refresh and try again.");
        if (report.ReportFileStatus != ReportStates.Filed) throw new ConflictException("The report is no longer pending approval.");

        var approval = await db.ApprovalTransactions.AsNoTracking()
            .Where(x => x.ReportId == reportId && x.ApproverUserId == managerUserId)
            .OrderByDescending(x => x.ApprovalCycle).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("The current manager has no approval transaction for this report.");
        if (approval.Status != ApprovalTransactionStates.Pending)
            throw new ConflictException("This approval transaction is no longer pending.");
        var previousIncomplete = await db.ApprovalTransactions.AsNoTracking().AnyAsync(x =>
            x.ReportId == reportId && x.ApprovalCycle == approval.ApprovalCycle &&
            x.StepOrder < approval.StepOrder && x.Status != ApprovalTransactionStates.Approved, cancellationToken);
        if (previousIncomplete)
            throw new ConflictException("A previous approver must complete the report first.");

        var legacyReason = reason.Length <= 255 ? reason : reason[..255];
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp2_LoadUserReportDetailsCancel {reportId}, {legacyReason}, {managerUserId}", cancellationToken);
        var audit = CreateAudit(reportId, managerUserId, WorkflowEvents.ManagerReturned, "For Approval", "Returned", reason, correlationId);
        await auditWriter.WriteAsync(db.Database.GetDbConnection(), transaction.GetDbTransaction(), audit, cancellationToken);
        var updated = await db.Reports.AsNoTracking().SingleAsync(x => x.Id == reportId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new WorkflowActionResult(reportId, "Returned", rowVersions.Encode(updated.RowVersion));
    }

    private WorkflowAuditEntry CreateAudit(string reportId, int actor, string eventType, string previous, string next, string? remarks, Guid correlationId) =>
        new(reportId, actor, eventType, previous, next, remarks, clock.UtcNow, correlationId);

    private static string ResolveReportStatus(string? fileStatus, string? printStatus) =>
        fileStatus == ReportStates.Approved && printStatus == ReportStates.Approved ? "Approved" : "For Approval";

}
