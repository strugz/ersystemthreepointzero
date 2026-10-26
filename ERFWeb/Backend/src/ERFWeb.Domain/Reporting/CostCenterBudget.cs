namespace ERFWeb.Domain.Reporting;

/// <summary>Spending of one employee in one category, inside one department's year (amounts incl. VAT).</summary>
public sealed record BudgetSpend(long DepartmentId, long EmployeeUserId, string EmployeeName, ExpenseCategory Category, int LineCount, decimal Amount);

/// <summary>One employee's share of a cost center's year, per R47 category.</summary>
public sealed record BudgetEmployeeTotals(
    long EmployeeUserId,
    string EmployeeName,
    int LineCount,
    IReadOnlyDictionary<ExpenseCategory, decimal> ByCategory,
    decimal Total);

/// <summary>
/// Where a cost center stands against its yearly budget. Remaining and PercentUsed are null while no
/// budget is set; PercentUsed is also null for a zero budget, which is over as soon as anything is spent.
/// </summary>
public sealed record BudgetStanding(decimal? Budget, decimal Used, decimal? Remaining, decimal? PercentUsed, bool OverBudget);

/// <summary>
/// Cost center budget monitoring. A cost center is one department's spending code with one overall
/// budget per calendar year; spending is every expense line dated in that year on a filed or approved
/// report of that department. Monitoring only: being over budget never blocks filing or approval.
/// </summary>
public static class CostCenterBudgetRules
{
    public const int MinYear = 2000;
    public const int MaxYear = 2100;

    /// <summary>The largest amount numeric(14,2) holds.</summary>
    public const decimal MaxAmount = 999_999_999_999.99m;

    public static string? YearError(int year) =>
        year is < MinYear or > MaxYear ? $"The year must be from {MinYear} to {MaxYear}." : null;

    public static string? AmountError(decimal amount)
    {
        if (amount < 0)
        {
            return "The budget cannot be negative.";
        }

        if (amount > MaxAmount)
        {
            return "The budget is too large.";
        }

        return decimal.Round(amount, 2) != amount ? "The budget takes at most two decimal places." : null;
    }

    /// <summary>The first and last day of the calendar year.</summary>
    public static (DateOnly From, DateOnly To) Period(int year) => (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

    public static BudgetStanding Standing(decimal? budget, decimal used)
    {
        if (budget is not { } b)
        {
            return new BudgetStanding(null, used, null, null, false);
        }

        var percent = b == 0 ? (decimal?)null : decimal.Round(used / b * 100, 1, MidpointRounding.AwayFromZero);
        return new BudgetStanding(b, used, b - used, percent, used > b);
    }

    /// <summary>Employee totals (every category present, zero when unused), largest spender first.</summary>
    public static IReadOnlyList<BudgetEmployeeTotals> Employees(IEnumerable<BudgetSpend> spend)
    {
        ArgumentNullException.ThrowIfNull(spend);
        return spend
            .GroupBy(s => s.EmployeeUserId)
            .Select(g =>
            {
                var byCategory = ByCategory(g);
                return new BudgetEmployeeTotals(g.Key, g.First().EmployeeName, g.Sum(s => s.LineCount), byCategory, byCategory.Values.Sum());
            })
            .OrderByDescending(e => e.Total)
            .ThenBy(e => e.EmployeeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.EmployeeUserId)
            .ToList();
    }

    public static IReadOnlyDictionary<ExpenseCategory, decimal> ByCategory(IEnumerable<BudgetSpend> spend)
    {
        ArgumentNullException.ThrowIfNull(spend);
        var totals = ExpenseSummaryBuilder.Categories.ToDictionary(c => c, _ => 0m);
        foreach (var s in spend)
        {
            totals[s.Category] += s.Amount;
        }

        return totals;
    }
}
