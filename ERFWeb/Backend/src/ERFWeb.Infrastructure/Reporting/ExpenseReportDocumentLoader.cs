using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// LINQ read model for the R47 document: report header + employee, cash advance,
/// department signatories, and the active expense lines in print order.
/// </summary>
public sealed class ExpenseReportDocumentLoader(ErfDbContext db) : IExpenseReportDocumentLoader
{
    public async Task<ExpenseReportDocumentDto> LoadAsync(Guid reportId, CancellationToken ct = default)
    {
        var header = await (
            from r in db.ExpenseReports.AsNoTracking()
            where r.Id == reportId
            join u in db.Users.AsNoTracking() on r.EmployeeUserId equals u.Id
            join dsj in db.DepartmentSignatories.AsNoTracking() on u.DepartmentId equals (long?)dsj.DepartmentId into dss
            from ds in dss.DefaultIfEmpty()
            select new
            {
                r.Id,
                r.ReferenceNo,
                u.FullName,
                r.Purpose,
                r.RevolvingFundAmount,
                u.Signature,
                Endorser = ds != null ? ds.EndorserInitials : null,
                Reviewer = ds != null ? ds.ReviewerInitials : null,
                Approver = ds != null ? ds.ApproverInitials : null,
            }).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Expense report not found.");

        var cashAdvances = await db.CashAdvanceLines.AsNoTracking()
            .Where(c => c.ReportId == reportId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new R47CashAdvanceRow(c.AdvanceDate, c.ReferenceDocument, c.ReferenceNo, c.Amount))
            .ToListAsync(ct);

        var rawLines = await db.ExpenseLines.AsNoTracking()
            .Where(l => l.ReportId == reportId && l.IsActive)
            .OrderBy(l => l.SortOrder)
            .Select(l => new
            {
                l.TransactionDate,
                l.Particulars,
                l.InvoiceOrNo,
                l.ExpenseType,
                l.Category,
                l.Amount,
                l.VatAmount,
                l.TotalAmount,
                l.Location,
                l.Instrument,
                l.SerialNumber,
                l.ServiceNumber,
                l.WorkWith,
                l.SortOrder,
            })
            .ToListAsync(ct);

        var lines = rawLines.Select(l => new R47Line(
            l.TransactionDate,
            l.Particulars ?? string.Empty,
            l.InvoiceOrNo,
            l.ExpenseType,
            l.Category,
            l.Amount,
            l.VatAmount,
            l.TotalAmount,
            l.Location,
            l.Instrument,
            l.SerialNumber,
            l.ServiceNumber,
            l.WorkWith,
            l.SortOrder)).ToList();

        return new ExpenseReportDocumentDto(
            header.Id,
            header.ReferenceNo,
            header.FullName,
            header.Purpose ?? string.Empty,
            header.RevolvingFundAmount,
            cashAdvances,
            new R47Signatories(header.Endorser, header.Reviewer, header.Approver),
            header.Signature,
            lines);
    }
}
