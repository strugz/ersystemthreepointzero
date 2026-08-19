using System.Data;
using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.FinanceReceipts;
using ERSystem.Web.Domain.Common;
using ERSystem.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERSystem.Web.Infrastructure.Services;

public sealed class FinanceReceiptService(
    IDbContextFactory<LegacyErDbContext> contextFactory,
    IRowVersionCodec rowVersions,
    IClock clock,
    IWorkflowAuditWriter auditWriter) : IFinanceReceiptService
{
    public async Task<PagedResult<FinanceReceiptListItemDto>> GetReportsAsync(
        FinanceReceiptQuery query, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var source =
            from report in db.Reports.AsNoTracking()
            join finance in db.FinanceTracking.AsNoTracking() on report.Id equals finance.ReportId
            join user in db.Users.AsNoTracking() on report.UserId equals user.UserId
            join cash in db.CashAdvances.AsNoTracking() on report.Id equals cash.ReportId into cashRows
            from cash in cashRows.DefaultIfEmpty()
            where report.ReportFileStatus == ReportStates.Approved && report.ReportPrintStatus == ReportStates.Approved
            select new { report, finance, user, cash };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x =>
                (x.user.FullName != null && x.user.FullName.Contains(search)) || x.report.Id.Contains(search) ||
                (x.report.ErfReferenceNumber != null && x.report.ErfReferenceNumber.Contains(search)) ||
                (x.cash != null && x.cash.ReferenceNumber != null && x.cash.ReferenceNumber.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(query.FinanceStatus))
        {
            var status = query.FinanceStatus.Trim();
            source = source.Where(x => x.finance.FinanceStatus == status);
        }
        if (query.PhysicalReceiptsReceived.HasValue)
            source = source.Where(x => x.finance.PhysicalReceiptsReceived == query.PhysicalReceiptsReceived.Value);
        if (!string.IsNullOrWhiteSpace(query.ReportType))
        {
            var type = query.ReportType.Trim();
            source = source.Where(x => x.report.ReportType != null && x.report.ReportType.Trim() == type);
        }
        if (query.DateFrom.HasValue) source = source.Where(x => x.report.ReportDateFrom >= query.DateFrom);
        if (query.DateTo.HasValue) source = source.Where(x => x.report.ReportDateTo <= query.DateTo);

        var orderedSource = query.SortBy?.ToLowerInvariant() switch
        {
            "employee" or "employeename" => query.SortDirection == SortDirection.Descending
                ? source.OrderByDescending(x => x.user.FullName).ThenBy(x => x.report.Id)
                : source.OrderBy(x => x.user.FullName).ThenBy(x => x.report.Id),
            "status" or "financestatus" => query.SortDirection == SortDirection.Descending
                ? source.OrderByDescending(x => x.finance.FinanceStatus).ThenBy(x => x.report.Id)
                : source.OrderBy(x => x.finance.FinanceStatus).ThenBy(x => x.report.Id),
            _ => source.OrderByDescending(x => x.report.ReportDateFrom).ThenBy(x => x.report.Id)
        };

        var rows = await orderedSource
            .Select(x => new
            {
                x.report.Id,
                EmployeeUserId = x.user.UserId ?? 0,
                EmployeeName = x.user.FullName ?? string.Empty,
                x.report.ReportDateFrom,
                x.report.ReportDateTo,
                Description = x.report.ReportDescription ?? string.Empty,
                ReportType = x.report.ReportType ?? string.Empty,
                ErfReference = x.report.ErfReferenceNumber ?? string.Empty,
                CashReference = x.cash != null ? x.cash.ReferenceNumber ?? string.Empty : string.Empty,
                FinanceStatus = x.finance.FinanceStatus ?? FinanceStates.Pending,
                x.finance.PhysicalReceiptsReceived,
                x.finance.PhysicalReceiptsReceivedDate,
                x.finance.RowVersion
            }).ToListAsync(cancellationToken);

        var items = rows.Select(x => new FinanceReceiptListItemDto(
            x.Id, x.EmployeeUserId, x.EmployeeName.Trim(), x.ReportDateFrom, x.ReportDateTo, x.Description.Trim(),
            TextNormalization.TrimLegacy(x.ReportType), ResolveErfReference(x.ErfReference, x.CashReference), x.CashReference.Trim(),
            x.FinanceStatus.Trim(), x.PhysicalReceiptsReceived, ToUtc(x.PhysicalReceiptsReceivedDate), rowVersions.Encode(x.RowVersion))).ToArray();
        return items.ToInMemoryPagedResult(query);
    }

    public async Task<FinanceReceiptDetailDto> GetReportAsync(string reportId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var row = await (
            from report in db.Reports.AsNoTracking()
            join finance in db.FinanceTracking.AsNoTracking() on report.Id equals finance.ReportId
            join user in db.Users.AsNoTracking() on report.UserId equals user.UserId
            join department in db.Departments.AsNoTracking() on user.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            join cash in db.CashAdvances.AsNoTracking() on report.Id equals cash.ReportId into cashRows
            from cash in cashRows.DefaultIfEmpty()
            join receiver in db.Users.AsNoTracking() on finance.PhysicalReceiptsReceivedBy equals receiver.UserId into receivers
            from receiver in receivers.DefaultIfEmpty()
            where report.Id == reportId && report.ReportFileStatus == ReportStates.Approved && report.ReportPrintStatus == ReportStates.Approved
            select new { report, finance, user, department, cash, receiver }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("The approved Finance report was not found.");

        var cashReference = row.cash?.ReferenceNumber?.Trim() ?? string.Empty;
        return new FinanceReceiptDetailDto(
            row.report.Id, row.user.UserId ?? 0, row.user.FullName?.Trim() ?? string.Empty,
            row.report.ReportDateFrom, row.report.ReportDateTo, row.report.ReportDescription?.Trim() ?? string.Empty,
            TextNormalization.TrimLegacy(row.report.ReportType), ResolveErfReference(row.report.ErfReferenceNumber, cashReference),
            row.finance.FinanceStatus?.Trim() ?? FinanceStates.Pending, row.finance.PhysicalReceiptsReceived,
            row.finance.PhysicalReceiptsReceivedBy, row.receiver?.FullName?.Trim() ?? string.Empty,
            ToUtc(row.finance.PhysicalReceiptsReceivedDate), row.finance.FinanceRemarks?.Trim() ?? string.Empty,
            rowVersions.Encode(row.finance.RowVersion), row.department?.Name?.Trim() ?? string.Empty);
    }

    public async Task<ReceivePhysicalReceiptsResult> ReceiveAsync(
        int financeUserId, string reportId, ReceivePhysicalReceiptsRequest request, Guid correlationId, CancellationToken cancellationToken)
    {
        var remarks = request.Remarks?.Trim();
        if (remarks?.Length > 1000) throw new ValidationException("Remarks must be 1000 characters or fewer.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var report = await db.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken)
                ?? throw new NotFoundException("The report was not found.");
            if (report.ReportFileStatus != ReportStates.Approved || report.ReportPrintStatus != ReportStates.Approved)
                throw new ConflictException("The report has not completed manager approval.");
            var finance = await db.FinanceTracking.SingleOrDefaultAsync(x => x.ReportId == reportId, cancellationToken)
                ?? throw new NotFoundException("Finance tracking was not initialized for this report.");
            if (!rowVersions.Matches(finance.RowVersion, request.RowVersion))
                throw new ConflictException("The receipt record changed. Refresh and try again.");
            if (finance.PhysicalReceiptsReceived) throw new ConflictException("Physical receipts were already received.");

            var previous = finance.FinanceStatus?.Trim() ?? FinanceStates.Pending;
            finance.PhysicalReceiptsReceived = true;
            finance.PhysicalReceiptsReceivedBy = financeUserId;
            finance.PhysicalReceiptsReceivedDate = clock.UtcNow;
            finance.FinanceStatus = FinanceStates.ReceiptsReceived;
            finance.FinanceRemarks = string.IsNullOrEmpty(remarks) ? null : remarks;
            var audit = new WorkflowAuditEntry(reportId, financeUserId, WorkflowEvents.PhysicalReceiptsReceived,
                previous, FinanceStates.ReceiptsReceived, finance.FinanceRemarks, clock.UtcNow, correlationId);
            await db.SaveChangesAsync(cancellationToken);
            await auditWriter.WriteAsync(db.Database.GetDbConnection(), transaction.GetDbTransaction(), audit, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ReceivePhysicalReceiptsResult(reportId, FinanceStates.ReceiptsReceived, rowVersions.Encode(finance.RowVersion));
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The receipt record changed while it was being updated. Refresh and try again.");
        }
    }

    private static string ResolveErfReference(string? reportReference, string? cashReference) =>
        !string.IsNullOrWhiteSpace(reportReference) ? reportReference.Trim() :
        cashReference?.Trim().StartsWith("ER-", StringComparison.OrdinalIgnoreCase) == true ? cashReference.Trim() : string.Empty;

    private static decimal ToMoney(double value) => Math.Round(Convert.ToDecimal(value), 2, MidpointRounding.AwayFromZero);
    private static DateTime? ToUtc(DateTime? value) => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime() : null;
}
