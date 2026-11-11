namespace ERFWeb.Domain.Reporting;

/// <summary>The meal a claimed meal item is for.</summary>
public enum MealKind
{
    Breakfast,
    Lunch,
    Dinner,
    OtMeal,

    /// <summary>A Meals line with no meal item, or one whose type is not a known meal.</summary>
    Other,
}

/// <summary>One active Meals line as it enters the report, with the meal types of its items (may be empty).</summary>
public sealed record MealClaimLine(
    long EmployeeUserId,
    string EmployeeName,
    string? DepartmentName,
    string? ReferenceNo,
    DateOnly ExpenseDate,
    IReadOnlyList<string?> MealTypes,
    decimal Amount);

/// <summary>One employee's meal claims on one day, across all their reports.</summary>
public sealed record MealClaimRow(
    DateOnly Date,
    long EmployeeUserId,
    string EmployeeName,
    string? DepartmentName,
    IReadOnlyList<string> ReferenceNos,
    bool Breakfast,
    bool Lunch,
    bool Dinner,
    bool OtMeal,
    bool Other,
    int LineCount,
    decimal Amount);

/// <summary>One day: how many employees claimed each meal, and who.</summary>
public sealed record MealClaimDay(
    DateOnly Date,
    int EmployeeCount,
    int Breakfast,
    int Lunch,
    int Dinner,
    int OtMeal,
    int Other,
    decimal Amount,
    IReadOnlyList<MealClaimRow> Employees);

/// <summary>The daily meal claims report: days in date order, employees by name within a day.</summary>
public sealed record DailyMealClaims(IReadOnlyList<MealClaimDay> Days, int EmployeeDays, decimal Total);

/// <summary>
/// Daily meal claims (Finance requirement 3): for each day, the employees who claimed their meal budget and which
/// meals. Meal types are free text on the meal items; "OT Meal" (the editor) and "OTMeal" (rate lines) both count
/// as OT meal, matched without regard to case or spaces.
/// </summary>
public static class DailyMealClaimsBuilder
{
    public static MealKind Kind(string? mealType) =>
        (mealType ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant() switch
        {
            "BREAKFAST" => MealKind.Breakfast,
            "LUNCH" => MealKind.Lunch,
            "DINNER" => MealKind.Dinner,
            "OTMEAL" => MealKind.OtMeal,
            _ => MealKind.Other,
        };

    public static DailyMealClaims Build(IEnumerable<MealClaimLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var days = lines
            .GroupBy(l => l.ExpenseDate)
            .OrderBy(g => g.Key)
            .Select(day =>
            {
                var rows = day
                    .GroupBy(l => l.EmployeeUserId)
                    .Select(Row)
                    .OrderBy(r => r.EmployeeName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => r.EmployeeUserId)
                    .ToList();
                return new MealClaimDay(
                    day.Key,
                    rows.Count,
                    rows.Count(r => r.Breakfast),
                    rows.Count(r => r.Lunch),
                    rows.Count(r => r.Dinner),
                    rows.Count(r => r.OtMeal),
                    rows.Count(r => r.Other),
                    rows.Sum(r => r.Amount),
                    rows);
            })
            .ToList();

        return new DailyMealClaims(days, days.Sum(d => d.EmployeeCount), days.Sum(d => d.Amount));
    }

    private static MealClaimRow Row(IGrouping<long, MealClaimLine> employee)
    {
        var first = employee.First();
        var kinds = employee
            .SelectMany(l => l.MealTypes.Count == 0 ? [MealKind.Other] : l.MealTypes.Select(Kind))
            .ToHashSet();
        var references = employee
            .Select(l => l.ReferenceNo)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new MealClaimRow(
            first.ExpenseDate,
            employee.Key,
            first.EmployeeName,
            first.DepartmentName,
            references,
            kinds.Contains(MealKind.Breakfast),
            kinds.Contains(MealKind.Lunch),
            kinds.Contains(MealKind.Dinner),
            kinds.Contains(MealKind.OtMeal),
            kinds.Contains(MealKind.Other),
            employee.Count(),
            employee.Sum(l => l.Amount));
    }
}
