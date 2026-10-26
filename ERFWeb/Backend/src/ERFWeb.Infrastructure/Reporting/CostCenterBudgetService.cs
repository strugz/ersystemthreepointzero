using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Entitlements;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// LINQ read model for cost center budget monitoring. Finance sees every active cost center; a
/// signatory sees those of the departments a Department or Exclusion scope assignment of theirs
/// covers today. Spending is counted by the department snapshotted on the report at filing, so a
/// later move of the employee never shifts it, and budgets set after filing still count it.
/// </summary>
public sealed class CostCenterBudgetService(ErfDbContext db, ICurrentUser user, IClock clock, EntitlementContext calendar)
    : ICostCenterBudgetService
{
    public async Task<CostCenterBudgetReportDto> BuildAsync(CostCenterBudgetQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (CostCenterBudgetRules.YearError(query.Year) is { } yearError)
        {
            throw new ValidationException("year", yearError);
        }

        var centers = db.CostCenters.AsNoTracking().Where(c => c.IsActive && c.DepartmentId != null);
        if (!user.IsInRole(UserRole.Finance))
        {
            if (!user.IsInRole(UserRole.SignatoryLevel1) && !user.IsInRole(UserRole.SignatoryLevel2))
            {
                throw new ForbiddenException("Only Finance and signatories can see cost center budgets.");
            }

            var me = user.UserId;
            var today = calendar.Today;
            var covered = db.ScopeAssignments.AsNoTracking()
                .Where(a => a.HolderUserId == me && a.IsActive && a.DepartmentId != null
                    && (a.AssignmentType == ScopeAssignmentType.Department || a.AssignmentType == ScopeAssignmentType.Exclusion)
                    && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today))
                .Select(a => a.DepartmentId);
            centers = centers.Where(c => covered.Contains(c.DepartmentId));
        }

        var rows = await (
            from c in centers
            join d in db.Departments.AsNoTracking() on c.DepartmentId equals (long?)d.Id
            join bj in db.CostCenterBudgets.AsNoTracking().Where(b => b.BudgetYear == query.Year) on c.Id equals bj.CostCenterId into bs
            from b in bs.DefaultIfEmpty()
            orderby c.Code
            select new { c.Id, c.Code, c.Name, DepartmentId = d.Id, DepartmentName = d.Name, Budget = b != null ? (decimal?)b.Amount : null })
            .ToListAsync(ct);

        var departmentIds = rows.Select(r => r.DepartmentId).ToList();
        var (periodFrom, periodTo) = CostCenterBudgetRules.Period(query.Year);
        var statuses = query.ApprovedOnly
            ? new[] { ReportStatus.Approved }
            : new[] { ReportStatus.ForApproval, ReportStatus.Approved };
        var grouped = await (
            from l in db.ExpenseLines.AsNoTracking()
            where l.IsActive && l.TransactionDate >= periodFrom && l.TransactionDate <= periodTo
            join r in db.ExpenseReports.AsNoTracking() on l.ReportId equals r.Id
            where statuses.Contains(r.Status) && r.DepartmentId != null && departmentIds.Contains(r.DepartmentId.Value)
            group l by new { DepartmentId = r.DepartmentId!.Value, r.EmployeeUserId, l.Category } into g
            select new { g.Key.DepartmentId, g.Key.EmployeeUserId, g.Key.Category, LineCount = g.Count(), Amount = g.Sum(x => x.TotalAmount) })
            .ToListAsync(ct);

        var employeeIds = grouped.Select(g => g.EmployeeUserId).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => employeeIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var spend = grouped
            .Select(g => new BudgetSpend(g.DepartmentId, g.EmployeeUserId, names.GetValueOrDefault(g.EmployeeUserId, string.Empty), g.Category, g.LineCount, g.Amount))
            .ToLookup(s => s.DepartmentId);

        var result = rows.Select(r =>
        {
            var mine = spend[r.DepartmentId].ToList();
            var standing = CostCenterBudgetRules.Standing(r.Budget, mine.Sum(s => s.Amount));
            return new CostCenterBudgetRowDto(r.Id, r.Code, r.Name, r.DepartmentId, r.DepartmentName,
                standing.Budget, standing.Used, standing.Remaining, standing.PercentUsed, standing.OverBudget,
                mine.Sum(s => s.LineCount), CostCenterBudgetRules.ByCategory(mine), CostCenterBudgetRules.Employees(mine));
        }).ToList();

        return new CostCenterBudgetReportDto(query.Year, query.ApprovedOnly, clock.UtcNow, result,
            result.Sum(r => r.Budget ?? 0m), result.Sum(r => r.Used));
    }
}
