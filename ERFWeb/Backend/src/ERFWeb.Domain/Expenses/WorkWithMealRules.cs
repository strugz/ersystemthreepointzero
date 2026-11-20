using System.Globalization;
using ERFWeb.Domain.Reporting;

namespace ERFWeb.Domain.Expenses;

/// <summary>A line of another employee's report that tags the claimant as Work With.</summary>
public sealed record WorkWithTag(string FilerName, DateOnly Date, ExpenseCategory Category, IReadOnlyList<string> MealTypes);

/// <summary>A line the tagged employee claims (being saved, or on the report being filed).</summary>
public sealed record WorkWithClaim(DateOnly Date, ExpenseCategory Category, IReadOnlyList<string> MealTypes);

/// <summary>
/// Finance requirement 2: the filer who tags a Work With employee is liable for that employee's meal, so the tagged
/// employee cannot claim it (by date and meal; with <see cref="WorkWithScope.AllExpenses"/>, nothing on that date).
/// A tag counts as soon as the filer's line is saved, drafts included; a Rejected report never counts, and a removed
/// line or deleted report frees the tagged employee again.
/// </summary>
public static class WorkWithMealRules
{
    private static readonly char[] Separators = [',', '/', ';', '&'];

    /// <summary>The names in a Work With text ("A, B", a CRM list, "A and B"), trimmed, in order.</summary>
    public static IReadOnlyList<string> Names(string? workWith) =>
        string.IsNullOrWhiteSpace(workWith)
            ? []
            : workWith.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .SelectMany(n => n.Split(" and ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToList();

    public static bool Binds(ReportStatus status) => status != ReportStatus.Rejected;

    /// <summary>Why the claim is refused, or null when no tag covers it (or the rule is off).</summary>
    public static string? Problem(WorkWithClaim claim, IEnumerable<WorkWithTag> tags, FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(rules);
        if (!rules.WorkWithMealsEnabled)
        {
            return null;
        }

        var date = Date(claim.Date);
        foreach (var tag in tags)
        {
            var covered = Covered(tag, claim, rules);
            if (covered is null)
            {
                continue;
            }

            return rules.WorkWithScope == WorkWithScope.AllExpenses
                ? $"{tag.FilerName} tagged you as Work With on {date}, so you cannot claim expenses for that day."
                : $"{tag.FilerName} tagged you as Work With and claimed your {covered} on {date}.";
        }

        return null;
    }

    /// <summary>
    /// What of <paramref name="claim"/> the tag covers: the shared meals ("Lunch", "Lunch and Dinner"), "expenses" for
    /// <see cref="WorkWithScope.AllExpenses"/>, or null when it covers nothing (other date or meal, or the rule is off).
    /// </summary>
    public static string? Covered(WorkWithTag tag, WorkWithClaim claim, FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(rules);
        if (!rules.WorkWithMealsEnabled || tag.Date != claim.Date)
        {
            return null;
        }

        if (rules.WorkWithScope == WorkWithScope.AllExpenses)
        {
            return "expenses";
        }

        if (tag.Category != ExpenseCategory.Meals || claim.Category != ExpenseCategory.Meals)
        {
            return null;
        }

        var shared = Kinds(claim.MealTypes).Intersect(Kinds(tag.MealTypes)).Order().ToList();
        return shared.Count > 0 ? string.Join(" and ", shared.Select(MealName)) : null;
    }

    /// <summary>On the tagged employee's line: someone else paid for it as Work With.</summary>
    public static string PaidByWarning(string payerName, string? referenceNo, ReportStatus status, bool cleared, string covered, DateOnly date) =>
        $"{payerName} paid for {Thing(covered)} on {Date(date)} as Work With ({Where(referenceNo, status, cleared)}). {payerName} claims it; remove this line or lower it.";

    /// <summary>On the payer's line: the tagged employee claimed the same meal too.</summary>
    public static string AlsoClaimedWarning(string taggedName, string? referenceNo, ReportStatus status, bool cleared, string covered, DateOnly date) =>
        $"{taggedName} also claimed {Thing(covered)} on {Date(date)} ({Where(referenceNo, status, cleared)}). You paid as Work With, so you claim it; Finance sees both.";

    private static string Where(string? referenceNo, ReportStatus status, bool cleared)
    {
        var state = status switch
        {
            ReportStatus.New => "draft",
            ReportStatus.ForApproval => "for approval",
            ReportStatus.Returned => "returned",
            ReportStatus.Approved => cleared ? "approved, cleared for payment" : "approved",
            _ => status.ToString().ToLowerInvariant(),
        };
        return referenceNo is null ? state : $"{referenceNo}, {state}";
    }

    private static string Thing(string covered) => covered == "expenses" ? "the expenses" : $"this {covered}";

    private static string Date(DateOnly date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>The notice the filer sees while tagging someone; null when the rule is off.</summary>
    public static string? Notice(FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (!rules.WorkWithMealsEnabled)
        {
            return null;
        }

        return rules.WorkWithScope == WorkWithScope.AllExpenses
            ? "The people you tag cannot claim any expense on this date: you are liable for their meals."
            : "The people you tag cannot claim the meals you claim on this date: you are liable for their meals.";
    }

    private static IEnumerable<MealKind> Kinds(IEnumerable<string> mealTypes) =>
        mealTypes.Select(DailyMealClaimsBuilder.Kind).Where(k => k != MealKind.Other).Distinct();

    private static string MealName(MealKind kind) => kind == MealKind.OtMeal ? "OT meal" : kind.ToString();
}
