namespace ERFWeb.Domain.Reporting;

/// <summary>One expense line as it enters the expense summary (amount is the line total incl. VAT).</summary>
public sealed record SummaryLine(
    long EmployeeUserId,
    string EmployeeName,
    string? DepartmentName,
    string? ReferenceNo,
    ReportStatus ReportStatus,
    DateOnly ExpenseDate,
    ExpenseCategory Category,
    string? Particulars,
    string? Location,
    decimal Amount);

/// <summary>One employee's totals for the period, per R47 category.</summary>
public sealed record SummaryEmployeeTotals(
    long EmployeeUserId,
    string EmployeeName,
    string? DepartmentName,
    int LineCount,
    IReadOnlyDictionary<ExpenseCategory, decimal> ByCategory,
    decimal Total);

/// <summary>The expense summary: the lines (date, then employee), employee totals, and grand totals.</summary>
public sealed record ExpenseSummary(
    IReadOnlyList<SummaryLine> Lines,
    IReadOnlyList<SummaryEmployeeTotals> Employees,
    IReadOnlyDictionary<ExpenseCategory, decimal> ByCategory,
    decimal Total);

/// <summary>
/// Builds the expense summary (plan Phase 7, the web counterpart of the desktop
/// sp2_rptExpenseSummary): expense lines whose date falls in the period, totalled per employee and
/// per category. Every category is present in each breakdown, zero when unused, so exports always
/// carry the same columns.
/// </summary>
public static class ExpenseSummaryBuilder
{
    /// <summary>The widest period one summary may cover.</summary>
    public const int MaxPeriodDays = 366;

    public static readonly IReadOnlyList<ExpenseCategory> Categories = Enum.GetValues<ExpenseCategory>();

    /// <summary>Null when <paramref name="from"/>..<paramref name="to"/> is a usable period.</summary>
    public static string? PeriodError(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            return "The end date must be on or after the start date.";
        }

        return to.DayNumber - from.DayNumber + 1 > MaxPeriodDays
            ? $"A summary covers at most {MaxPeriodDays} days. Narrow the period."
            : null;
    }

    public static ExpenseSummary Build(IEnumerable<SummaryLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var ordered = lines
            .OrderBy(l => l.ExpenseDate)
            .ThenBy(l => l.EmployeeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.ReferenceNo, StringComparer.Ordinal)
            .ToList();

        var employees = ordered
            .GroupBy(l => l.EmployeeUserId)
            .Select(g =>
            {
                var first = g.First();
                var byCategory = Totals(g);
                return new SummaryEmployeeTotals(g.Key, first.EmployeeName, first.DepartmentName, g.Count(), byCategory, byCategory.Values.Sum());
            })
            .OrderBy(e => e.EmployeeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.EmployeeUserId)
            .ToList();

        var grand = Totals(ordered);
        return new ExpenseSummary(ordered, employees, grand, grand.Values.Sum());
    }

    private static Dictionary<ExpenseCategory, decimal> Totals(IEnumerable<SummaryLine> lines)
    {
        var totals = Categories.ToDictionary(c => c, _ => 0m);
        foreach (var line in lines)
        {
            totals[line.Category] += line.Amount;
        }

        return totals;
    }
}
