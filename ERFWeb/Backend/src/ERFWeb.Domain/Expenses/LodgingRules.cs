using System.Globalization;

namespace ERFWeb.Domain.Expenses;

/// <summary>
/// Lodging claims (Finance requirement 5): the line amount is per night and the multiplier is the nights. The rate
/// is the claimant's rate profile Lodging line or one flat amount, as the Finance rules say; with the cap on, a
/// per-night amount above the rate is refused. No rate set (0) means no cap.
/// </summary>
public static class LodgingRules
{
    /// <summary>The per-night lodging rate: the flat amount, or <paramref name="profileRate"/> from the rate profile.</summary>
    public static decimal Rate(FinanceRuleSet rules, decimal profileRate)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return rules.LodgingRateBasis == LodgingRateBasis.Flat ? rules.LodgingFlatRate : profileRate;
    }

    /// <summary>Why <paramref name="perNight"/> cannot be claimed; null when it can.</summary>
    public static string? AmountProblem(decimal perNight, decimal rate, FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return rules.LodgingCapAtRate && rate > 0 && perNight > rate
            ? string.Create(CultureInfo.InvariantCulture, $"The lodging rate is {rate:N2} a night; enter at most that.")
            : null;
    }

    /// <summary>The computation shown on the line, e.g. "(3 Nights) * 1500".</summary>
    public static string ComputationText(int nights, decimal perNight) =>
        string.Create(CultureInfo.InvariantCulture, $"({nights} {(nights == 1 ? "Night" : "Nights")}) * {perNight:0.##}");
}
