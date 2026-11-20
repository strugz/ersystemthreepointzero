using System.Globalization;
using ERFWeb.Domain.Reporting;

namespace ERFWeb.Domain.Expenses;

/// <summary>Another Meals line of the same employee on the same date; <see cref="ReferenceNo"/> null for this report.</summary>
public sealed record ClaimedMeals(string? ReferenceNo, bool SameReport, IReadOnlyList<string> MealTypes);

/// <summary>
/// An employee claims each meal (Breakfast, Lunch, Dinner, OT meal) at most once a day, across this report and their
/// other reports that are not Rejected. "OT Meal" and "OTMeal" are the same meal; a Meals line without a meal type is
/// not compared.
/// </summary>
public static class DuplicateMealRules
{
    /// <summary>Why the meals cannot be claimed on <paramref name="date"/>, or null when none was claimed already.</summary>
    public static string? Problem(DateOnly date, IEnumerable<string> mealTypes, IEnumerable<ClaimedMeals> claimed)
    {
        ArgumentNullException.ThrowIfNull(mealTypes);
        ArgumentNullException.ThrowIfNull(claimed);
        var wanted = Kinds(mealTypes).ToHashSet();
        if (wanted.Count == 0)
        {
            return null;
        }

        foreach (var other in claimed)
        {
            var shared = Kinds(other.MealTypes).Where(wanted.Contains).Order().ToList();
            if (shared.Count > 0)
            {
                var where = other.SameReport ? "on this report" : $"on {other.ReferenceNo ?? "another draft report"}";
                return $"You already claimed {string.Join(" and ", shared.Select(MealName))} on "
                    + $"{date.ToString("d MMM yyyy", CultureInfo.InvariantCulture)} {where}. Each meal is claimed once a day.";
            }
        }

        return null;
    }

    private static IEnumerable<MealKind> Kinds(IEnumerable<string> mealTypes) =>
        mealTypes.Select(DailyMealClaimsBuilder.Kind).Where(k => k != MealKind.Other).Distinct();

    private static string MealName(MealKind kind) => kind == MealKind.OtMeal ? "OT meal" : kind.ToString();
}
