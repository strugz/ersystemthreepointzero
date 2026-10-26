using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;

namespace ERFWeb.Application.Features.Reporting;

/// <summary>
/// A cost center budget request: spending dated in the calendar year, on reports filed (For Approval)
/// or approved, or approved only. Drafts, returned and rejected reports never count.
/// </summary>
public sealed record CostCenterBudgetQuery(int Year, bool ApprovedOnly = false);

/// <summary>One cost center's year: its budget, what its department spent, and who spent it.</summary>
public sealed record CostCenterBudgetRowDto(
    long CostCenterId,
    string Code,
    string Name,
    long DepartmentId,
    string DepartmentName,
    decimal? Budget,
    decimal Used,
    decimal? Remaining,
    decimal? PercentUsed,
    bool OverBudget,
    int LineCount,
    IReadOnlyDictionary<ExpenseCategory, decimal> ByCategory,
    IReadOnlyList<BudgetEmployeeTotals> Employees);

/// <summary>
/// The cost centers the caller may watch (every one for Finance; for a signatory, those of the
/// departments their scope covers today) with the totals across them.
/// </summary>
public sealed record CostCenterBudgetReportDto(
    int Year,
    bool ApprovedOnly,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<CostCenterBudgetRowDto> CostCenters,
    decimal TotalBudget,
    decimal TotalUsed);

public interface ICostCenterBudgetService
{
    Task<CostCenterBudgetReportDto> BuildAsync(CostCenterBudgetQuery query, CancellationToken ct = default);
}
