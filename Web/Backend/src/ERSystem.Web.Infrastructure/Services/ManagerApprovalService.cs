using System.Data;
using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.ManagerApprovals;
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
            from report in db.Reports.AsNoTracking()
            join assignment in db.UserAuthorities.AsNoTracking() on report.UserId equals assignment.UserId
            join user in db.Users.AsNoTracking() on report.UserId equals user.UserId
            join department in db.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            where assignment.AuthorityId == managerUserId
            select new { report, assignment, user, department };

        if (pending)
        {
            source = source.Where(x =>
                x.report.ReportFileStatus == ReportStates.Filed &&
                !db.ReportAuthorities.Any(a => a.ReportId == x.report.Id && a.SignId == managerUserId) &&
                x.report.ReportReserveStatus2 != managerUserId.ToString() &&
                !db.UserAuthorities.Any(previous =>
                    previous.UserId == x.report.UserId && previous.Sort < x.assignment.Sort &&
                    !db.ReportAuthorities.Any(completed => completed.ReportId == x.report.Id && completed.SignId == previous.AuthorityId)));
        }
        else
        {
            source = source.Where(x =>
                db.ReportAuthorities.Any(a => a.ReportId == x.report.Id && a.SignId == managerUserId) ||
                x.report.ReportReserveStatus2 == managerUserId.ToString());
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x =>
                (x.user.FullName != null && x.user.FullName.Contains(search)) ||
                x.report.Id.Contains(search) ||
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
                EmployeeUserId = x.user.UserId ?? 0,
                EmployeeName = x.user.FullName ?? string.Empty,
                Department = x.department != null ? x.department.Name ?? string.Empty : string.Empty,
                x.report.ReportDateFrom,
                x.report.ReportDateTo,
                Description = x.report.ReportDescription ?? string.Empty,
                ReportType = x.report.ReportType ?? string.Empty,
                CurrentStep = x.assignment.Sort ?? 0,
                TotalSteps = db.UserAuthorities.Count(a => a.UserId == x.report.UserId),
                x.report.ReportFileStatus,
                x.report.ReportPrintStatus,
                x.report.RowVersion
            }).ToListAsync(cancellationToken);

        var items = rows.Select(x => new ManagerReportListItemDto(
            x.Id, x.EmployeeUserId, x.EmployeeName.Trim(), x.Department.Trim(), x.ReportDateFrom, x.ReportDateTo,
            x.Description.Trim(), TextNormalization.TrimLegacy(x.ReportType), x.CurrentStep, x.TotalSteps,
            ResolveReportStatus(x.ReportFileStatus, x.ReportPrintStatus), rowVersions.Encode(x.RowVersion))).ToArray();
        return items.ToInMemoryPagedResult(query);
    }

    public async Task<ManagerReportDetailDto> GetReportAsync(int managerUserId, string reportId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var header = await (
            from report in db.Reports.AsNoTracking()
            join assignment in db.UserAuthorities.AsNoTracking() on report.UserId equals assignment.UserId
            join user in db.Users.AsNoTracking() on report.UserId equals user.UserId
            join department in db.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            where report.Id == reportId && assignment.AuthorityId == managerUserId
            select new { report, assignment, user, department }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("The report was not found or is not assigned to the current manager.");

        var expenseRows = await db.Expenses.AsNoTracking().Where(x => x.ReportId == reportId)
            .OrderBy(x => x.Sort).ThenBy(x => x.TransactionDate).ToListAsync(cancellationToken);
        var expenses = expenseRows.Select(x => new ExpenseLineDto(
            x.Id, x.TransactionDate, x.Particulars?.Trim() ?? string.Empty, x.Category?.Trim() ?? string.Empty,
            x.Location?.Trim() ?? string.Empty, ToMoney(x.Amount), ToMoney(x.TotalAmount), x.Remarks?.Trim() ?? string.Empty)).ToArray();

        var cash = await db.CashAdvances.AsNoTracking().FirstOrDefaultAsync(x => x.ReportId == reportId, cancellationToken);
        var cashDto = cash is null ? null : new CashAdvanceDto(
            cash.Amount.HasValue ? ToMoney(cash.Amount) : null, cash.Date?.Trim() ?? string.Empty,
            cash.ReferenceDocument?.Trim() ?? string.Empty, cash.ReferenceNumber?.Trim() ?? string.Empty,
            cash.RevolvingFund?.Trim() ?? string.Empty);

        var attachments = await db.ScannedReceipts.AsNoTracking().Where(x => x.ReportId == reportId).OrderBy(x => x.Id)
            .Select(x => new ReceiptAttachmentDto(x.Id, x.OriginalFileName, x.ContentType, x.FileSizeBytes, x.CreatedDate))
            .ToListAsync(cancellationToken);
        var assignments = await db.UserAuthorities.AsNoTracking().Where(x => x.UserId == header.report.UserId).OrderBy(x => x.Sort).ToListAsync(cancellationToken);
        var approvals = await db.ReportAuthorities.AsNoTracking().Where(x => x.ReportId == reportId).ToListAsync(cancellationToken);
        var approverIds = assignments.Where(x => x.AuthorityId.HasValue).Select(x => x.AuthorityId!.Value).ToArray();
        var approvers = await db.Users.AsNoTracking().Where(x => x.UserId.HasValue && approverIds.Contains(x.UserId.Value))
            .ToDictionaryAsync(x => x.UserId!.Value, x => x.FullName ?? x.Username ?? string.Empty, cancellationToken);
        var auditDates = await db.WorkflowAudits.AsNoTracking().Where(x => x.ReportId == reportId && x.EventType == WorkflowEvents.ManagerApproved)
            .GroupBy(x => x.ActorUserId).Select(x => new { UserId = x.Key, Date = x.Max(a => a.OccurredAtUtc) })
            .ToDictionaryAsync(x => x.UserId, x => (DateTime?)x.Date, cancellationToken);

        var trail = assignments.Select(x =>
        {
            var approverId = x.AuthorityId ?? 0;
            var approved = approvals.Any(a => a.SignId == approverId) || header.report.ReportReserveStatus2 == approverId.ToString();
            return new ApprovalTrailItemDto(approverId, approvers.GetValueOrDefault(approverId, x.AuthorityName ?? string.Empty).Trim(),
                x.Sort ?? 0, auditDates.GetValueOrDefault(approverId), approved ? "Approved" : "Pending");
        }).ToArray();

        return new ManagerReportDetailDto(
            header.report.Id, header.user.UserId ?? 0, header.user.FullName?.Trim() ?? string.Empty,
            header.department?.Name?.Trim() ?? string.Empty, header.report.ReportDateFrom, header.report.ReportDateTo,
            header.report.ReportDescription?.Trim() ?? string.Empty, TextNormalization.TrimLegacy(header.report.ReportType),
            header.report.ErfReferenceNumber?.Trim() ?? string.Empty, expenses, cashDto, attachments, trail,
            header.assignment.Sort ?? 0, assignments.Count, ResolveReportStatus(header.report.ReportFileStatus, header.report.ReportPrintStatus),
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

            var owner = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == report.UserId, cancellationToken)
                ?? throw new NotFoundException("The report owner was not found.");
            var assignments = await db.UserAuthorities.AsNoTracking().Where(x => x.UserId == report.UserId).OrderBy(x => x.Sort).ToListAsync(cancellationToken);
            var assignment = assignments.SingleOrDefault(x => x.AuthorityId == managerUserId)
                ?? throw new ForbiddenException("The current manager is not assigned to this report.");
            var approvals = await db.ReportAuthorities.AsNoTracking().Where(x => x.ReportId == reportId).ToListAsync(cancellationToken);
            if (approvals.Any(x => x.SignId == managerUserId) || report.ReportReserveStatus2 == managerUserId.ToString())
                throw new ConflictException("This approval was already completed.");
            var completedSorts = assignments.Where(a => approvals.Any(x => x.SignId == a.AuthorityId)).Select(a => a.Sort ?? 0).ToArray();
            if (!ApprovalSequence.CanApprove(assignment.Sort ?? 0, completedSorts))
                throw new ConflictException("A previous approver must complete the report first.");

            var previousState = ResolveReportStatus(report.ReportFileStatus, report.ReportPrintStatus);
            var newNumber = (report.ReportNumberStatus ?? 0) + 1;
            if (!owner.ReportNumberStatus.HasValue) throw new ConflictException("The report owner has no configured final approval step.");
            var isFinal = newNumber == owner.ReportNumberStatus.Value;
            report.ReportNumberStatus = newNumber;
            report.ReportEndorseStatus = ReportStates.EndorseApproved;

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
        var assigned = await db.UserAuthorities.AsNoTracking().AnyAsync(x => x.UserId == report.UserId && x.AuthorityId == managerUserId, cancellationToken);
        if (!assigned) throw new ForbiddenException("The current manager is not assigned to this report.");
        if (report.ReportFileStatus != ReportStates.Filed) throw new ConflictException("The report is no longer pending approval.");

        var assignments = await db.UserAuthorities.AsNoTracking().Where(x => x.UserId == report.UserId).OrderBy(x => x.Sort).ToListAsync(cancellationToken);
        var assignment = assignments.Single(x => x.AuthorityId == managerUserId);
        var approvals = await db.ReportAuthorities.AsNoTracking().Where(x => x.ReportId == reportId).ToListAsync(cancellationToken);
        if (approvals.Any(x => x.SignId == managerUserId) || report.ReportReserveStatus2 == managerUserId.ToString())
            throw new ConflictException("This manager action was already completed.");
        var completedSorts = assignments.Where(a => approvals.Any(x => x.SignId == a.AuthorityId)).Select(a => a.Sort ?? 0).ToArray();
        if (!ApprovalSequence.CanApprove(assignment.Sort ?? 0, completedSorts))
            throw new ConflictException("A previous approver must complete the report first.");

        var legacyReason = reason.Length <= 255 ? reason : reason[..255];
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp2_LoadUserReportDetailsCancel {reportId}, {legacyReason}", cancellationToken);
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

    private static decimal ToMoney(double? value) => Math.Round(Convert.ToDecimal(value ?? 0d), 2, MidpointRounding.AwayFromZero);
}
