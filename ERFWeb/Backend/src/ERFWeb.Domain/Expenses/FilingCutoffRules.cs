using System.Globalization;

namespace ERFWeb.Domain.Expenses;

/// <summary>The Friday a report filed now is processed in, and whether it missed the earlier one.</summary>
public sealed record FilingCutoffStatus(DateOnly ProcessingDate, bool IsLate, string CutoffText);

/// <summary>
/// Weekly processing cutoff (Finance requirement 1): Finance processes reports every Friday. A report filed after
/// the cutoff (default Wednesday 12:00, business local time) and before Friday ends is processed the Friday after.
/// A notice only: it never blocks filing and never moves the report.
/// </summary>
public static class FilingCutoffRules
{
    public const DayOfWeek ProcessingDay = DayOfWeek.Friday;

    /// <summary>The processing Friday for a report filed at <paramref name="now"/>, or null when the cutoff is off.</summary>
    public static FilingCutoffStatus? Evaluate(DateTimeOffset now, TimeSpan utcOffset, FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (!rules.CutoffEnabled || FinanceRuleSetRules.Validate(rules).Count > 0)
        {
            return null;
        }

        var local = now.ToOffset(utcOffset).DateTime;
        var today = DateOnly.FromDateTime(local);

        // This week's Friday (today on a Friday); on Saturday and Sunday the coming Friday.
        var friday = today.AddDays(((int)ProcessingDay - (int)today.DayOfWeek + 7) % 7);
        var cutoffDate = friday.AddDays((int)rules.CutoffWeekday - (int)ProcessingDay);
        var cutoff = cutoffDate.ToDateTime(FinanceRuleSetRules.ParseTime(rules.CutoffTime)!.Value);

        var late = local >= cutoff;
        return new FilingCutoffStatus(late ? friday.AddDays(7) : friday, late, CutoffText(rules));
    }

    /// <summary>"Wednesday 12:00 PM".</summary>
    public static string CutoffText(FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var time = FinanceRuleSetRules.ParseTime(rules.CutoffTime) ?? FinanceRuleSetRules.ParseTime(FinanceRuleSet.Default.CutoffTime)!.Value;
        return $"{rules.CutoffWeekday} {time.ToString("h:mm tt", CultureInfo.InvariantCulture)}";
    }

    /// <summary>Whether a report filed at <paramref name="now"/> is stored as filed late for Finance.</summary>
    public static bool FlagAsLate(DateTimeOffset now, TimeSpan utcOffset, FinanceRuleSet rules) =>
        rules is { LateHandling: LateFilingHandling.FlagForFinance } && Evaluate(now, utcOffset, rules) is { IsLate: true };
}
