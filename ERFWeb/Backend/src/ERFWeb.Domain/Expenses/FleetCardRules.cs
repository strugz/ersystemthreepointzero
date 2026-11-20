using System.Globalization;

namespace ERFWeb.Domain.Expenses;

/// <summary>An active Gas line, as the filing check reads it.</summary>
public sealed record GasLineReason(DateOnly Date, string? Particulars);

/// <summary>
/// Finance rule: an employee with a company fleet card (<c>users.has_fleet_card</c>) is reimbursed for Gas only when
/// the card could not be used, so the Reason on their Gas line must be one of <see cref="FinanceRuleSet.FleetCardGasReasons"/>
/// (unless <see cref="FinanceRuleSet.FleetCardOtherReasonAllowed"/>). Vehicle emergency purchases and employees
/// without a card are not affected.
/// </summary>
public static class FleetCardRules
{
    public const int MaxReasons = 30;
    public const int MaxReasonLength = 80;

    public static readonly IReadOnlyList<string> DefaultReasons =
    [
        "Fleet card offline or system down",
        "Station card terminal offline",
        "No Shell station in the area",
        "Shell station out of fuel or closed",
        "Coding: issued vehicle coded, used another vehicle",
        "Fleet card limit reached",
        "Fleet card lost or stolen (reported)",
        "Fleet card expired or for renewal",
        "Fleet card damaged or cannot be read",
        "Vehicle not enrolled in the fleet card",
        "Emergency refuel, no Shell station within range",
    ];

    /// <summary>The reasons as saved: trimmed, single spaces, blanks and repeats (ignoring case) dropped, order kept.</summary>
    public static IReadOnlyList<string> NormalizeReasons(IEnumerable<string?>? reasons) =>
        (reasons ?? [])
            .Select(SingleSpaced)
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Why the reason list cannot be saved, or null when it can.</summary>
    public static string? ReasonsProblem(IReadOnlyList<string>? reasons, bool ruleEnabled)
    {
        if (reasons is null)
        {
            return "Add at least one reason.";
        }

        if (ruleEnabled && reasons.Count == 0)
        {
            return "Add at least one reason, or switch the fleet card rule off.";
        }

        if (reasons.Count > MaxReasons)
        {
            return $"Keep the list to {MaxReasons} reasons or fewer.";
        }

        if (reasons.Any(r => string.IsNullOrWhiteSpace(r) || r.Trim().Length > MaxReasonLength))
        {
            return $"Each reason must be 1 to {MaxReasonLength} characters.";
        }

        return reasons.Any(r => r.Contains(VehicleParticulars.Separator, StringComparison.Ordinal))
            ? "A reason cannot contain \" - \" (it separates the reason, vehicle model and plate no.)."
            : null;
    }

    /// <summary>The employee's Gas reasons are limited to the list.</summary>
    public static bool Restricts(FinanceRuleSet rules, bool hasFleetCard)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return hasFleetCard && rules.FleetCardRuleEnabled && !rules.FleetCardOtherReasonAllowed;
    }

    /// <summary>The reasons the line dialog offers a fleet card holder, or null when the rule does not apply to them.</summary>
    public static IReadOnlyList<string>? ReasonsFor(FinanceRuleSet rules, bool hasFleetCard)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return hasFleetCard && rules.FleetCardRuleEnabled ? rules.FleetCardGasReasons : null;
    }

    /// <summary>Why this line cannot be saved under the fleet card rule, or null when it can.</summary>
    public static string? LineProblem(ExpenseCategory category, string? particulars, FinanceRuleSet rules, bool hasFleetCard) =>
        category == ExpenseCategory.Gas && Restricts(rules, hasFleetCard) && !IsListed(particulars, rules)
            ? "You have a fleet card, so Gas is reimbursed only when the card could not be used. Pick one of the listed reasons."
            : null;

    /// <summary>One filing blocker per Gas line whose reason is not on the list (lines saved before the card or the rule).</summary>
    public static IReadOnlyList<string> FilingProblems(IEnumerable<GasLineReason> gasLines, FinanceRuleSet rules, bool hasFleetCard)
    {
        ArgumentNullException.ThrowIfNull(gasLines);
        if (!Restricts(rules, hasFleetCard))
        {
            return [];
        }

        return gasLines
            .Where(l => !IsListed(l.Particulars, rules))
            .OrderBy(l => l.Date)
            .Select(l => $"Gas on {l.Date.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}: you have a fleet card, so pick one of the listed fleet card reasons on the line.")
            .ToList();
    }

    private static bool IsListed(string? particulars, FinanceRuleSet rules)
    {
        var reason = SingleSpaced(VehicleParticulars.Reason(particulars));
        return rules.FleetCardGasReasons.Any(r => string.Equals(SingleSpaced(r), reason, StringComparison.OrdinalIgnoreCase));
    }

    private static string SingleSpaced(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
