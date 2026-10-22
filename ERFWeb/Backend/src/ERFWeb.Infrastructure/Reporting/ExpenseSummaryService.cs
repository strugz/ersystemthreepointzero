using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// LINQ read model for the expense summary (plan Phase 7). Scope decides whose reports count:
/// your own; reports you sign for (a step you hold or acted on, delegated steps included); or, for
/// Finance, everyone's. A System Administrator holds no transacting role and gets only "Mine".
/// </summary>
public sealed class ExpenseSummaryService(ErfDbContext db, ICurrentUser user, IClock clock) : IExpenseSummaryService
{
    public async Task<ExpenseSummaryReport> BuildAsync(ExpenseSummaryQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (ExpenseSummaryBuilder.PeriodError(query.From, query.To) is { } periodError)
        {
            throw new ValidationException("to", periodError);
        }

        var me = user.UserId;
        var statuses = query.ApprovedOnly
            ? new[] { ReportStatus.Approved }
            : new[] { ReportStatus.ForApproval, ReportStatus.Approved };
        var reports = db.ExpenseReports.AsNoTracking().Where(r => statuses.Contains(r.Status));

        reports = query.Scope switch
        {
            SummaryScope.Mine => reports.Where(r => r.EmployeeUserId == me),
            SummaryScope.Approvals when user.IsInRole(UserRole.SignatoryLevel1) || user.IsInRole(UserRole.SignatoryLevel2) =>
                reports.Where(r => db.ApprovalSteps.Any(s => s.ReportId == r.Id && (s.ApproverUserId == me || s.ActedByUserId == me))),
            SummaryScope.All when user.IsInRole(UserRole.Finance) => reports,
            SummaryScope.Approvals => throw new ForbiddenException("Only signatories can summarize the reports they sign for."),
            _ => throw new ForbiddenException("Only Finance can summarize every employee's reports."),
        };

        var departmentId = query.DepartmentId;
        var rows = await (
            from l in db.ExpenseLines.AsNoTracking()
            where l.IsActive && l.TransactionDate >= query.From && l.TransactionDate <= query.To
            join r in reports on l.ReportId equals r.Id
            join e in db.Users.AsNoTracking() on r.EmployeeUserId equals e.Id
            where departmentId == null || e.DepartmentId == departmentId
            join dj in db.Departments.AsNoTracking() on e.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            orderby l.TransactionDate, l.Id
            select new SummaryLine(
                e.Id, e.FullName, d != null ? d.Name : null, r.ReferenceNo, r.Status,
                l.TransactionDate, l.Category, l.Particulars, l.Location, l.TotalAmount))
            .Take(IExportService.MaxRows + 1)
            .ToListAsync(ct);

        if (rows.Count > IExportService.MaxRows)
        {
            throw new ValidationException("to", $"The summary has more than {IExportService.MaxRows:N0} expense lines. Narrow the period or pick a department.");
        }

        var departmentName = departmentId is { } id
            ? await db.Departments.AsNoTracking().Where(d => d.Id == id).Select(d => d.Name).FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("Department not found.")
            : null;

        return new ExpenseSummaryReport(query, departmentName, clock.UtcNow, user.FullName, ExpenseSummaryBuilder.Build(rows));
    }
}
