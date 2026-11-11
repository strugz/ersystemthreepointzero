using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>LINQ read model for the daily meal claims report (Finance requirement 3). Finance only.</summary>
public sealed class DailyMealClaimService(ErfDbContext db, ICurrentUser user, IClock clock) : IDailyMealClaimService
{
    public async Task<DailyMealClaimReport> BuildAsync(DailyMealClaimQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!user.IsInRole(UserRole.Finance))
        {
            throw new ForbiddenException("Only Finance can see the daily meal claims.");
        }

        if (ExpenseSummaryBuilder.PeriodError(query.From, query.To) is { } periodError)
        {
            throw new ValidationException("to", periodError);
        }

        var statuses = query.ApprovedOnly
            ? new[] { ReportStatus.Approved }
            : new[] { ReportStatus.ForApproval, ReportStatus.Approved };
        var departmentId = query.DepartmentId;

        var rows = await (
            from l in db.ExpenseLines.AsNoTracking()
            where l.IsActive && l.Category == ExpenseCategory.Meals && l.TransactionDate >= query.From && l.TransactionDate <= query.To
            join r in db.ExpenseReports.AsNoTracking() on l.ReportId equals r.Id
            where statuses.Contains(r.Status)
            join e in db.Users.AsNoTracking() on r.EmployeeUserId equals e.Id
            let reportDepartmentId = r.DepartmentId ?? e.DepartmentId
            where departmentId == null || reportDepartmentId == departmentId
            join dj in db.Departments.AsNoTracking() on reportDepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            orderby l.TransactionDate, l.Id
            select new { l.Id, EmployeeId = e.Id, e.FullName, DepartmentName = d != null ? d.Name : null, r.ReferenceNo, l.TransactionDate, l.TotalAmount })
            .Take(IExportService.MaxRows + 1)
            .ToListAsync(ct);

        if (rows.Count > IExportService.MaxRows)
        {
            throw new ValidationException("to", $"There are more than {IExportService.MaxRows:N0} meal lines. Narrow the period or pick a department.");
        }

        var lineIds = rows.Select(r => r.Id).ToList();
        var meals = (await db.ExpenseMealItems.AsNoTracking()
                .Where(m => lineIds.Contains(m.ExpenseLineId))
                .Select(m => new { m.ExpenseLineId, m.MealType })
                .ToListAsync(ct))
            .ToLookup(m => m.ExpenseLineId, m => m.MealType);

        var departmentName = departmentId is { } id
            ? await db.Departments.AsNoTracking().Where(d => d.Id == id).Select(d => d.Name).FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("Department not found.")
            : null;

        var claims = DailyMealClaimsBuilder.Build(rows.Select(r =>
            new MealClaimLine(r.EmployeeId, r.FullName, r.DepartmentName, r.ReferenceNo, r.TransactionDate, meals[r.Id].ToList(), r.TotalAmount)));
        return new DailyMealClaimReport(query, departmentName, clock.UtcNow, user.FullName, claims);
    }
}
