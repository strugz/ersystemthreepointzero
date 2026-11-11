using System.Globalization;

namespace ERFWeb.Domain.Expenses;

/// <summary>What happens to a report filed after the weekly processing cutoff.</summary>
public enum LateFilingHandling
{
    /// <summary>The filer is told which Friday the report goes into; nothing else changes.</summary>
    NoticeOnly,

    /// <summary>As NoticeOnly, and the report is marked late for Finance.</summary>
    FlagForFinance,
}

/// <summary>
/// The Finance rules the System Administrator sets on the Rules page (Finance requirements of 2026-11-11), stored
/// as one JSON row. Every property has a default, so JSON saved before a rule existed still loads with that
/// rule's default.
/// </summary>
public sealed record FinanceRuleSet
{
    public static readonly FinanceRuleSet Default = new();

    /// <summary>Show the weekly processing cutoff notice when filing.</summary>
    public bool CutoffEnabled { get; init; } = true;

    /// <summary>Last weekday (Monday to Friday) on which a report still makes this Friday's processing.</summary>
    public DayOfWeek CutoffWeekday { get; init; } = DayOfWeek.Wednesday;

    /// <summary>Local time on <see cref="CutoffWeekday"/>, "HH:mm" (24-hour).</summary>
    public string CutoffTime { get; init; } = "12:00";

    public LateFilingHandling LateHandling { get; init; } = LateFilingHandling.NoticeOnly;
}

/// <summary>Validation of the Finance rules the administrator saves; field names match the Rules page.</summary>
public static class FinanceRuleSetRules
{
    /// <summary>Field errors keyed <c>financeRules.&lt;property&gt;</c>; empty when the rules can be saved.</summary>
    public static IReadOnlyDictionary<string, string> Validate(FinanceRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var errors = new Dictionary<string, string>();

        if (rules.CutoffWeekday is DayOfWeek.Saturday or DayOfWeek.Sunday || !Enum.IsDefined(rules.CutoffWeekday))
        {
            errors["financeRules.cutoffWeekday"] = "Choose a weekday from Monday to Friday.";
        }

        if (ParseTime(rules.CutoffTime) is null)
        {
            errors["financeRules.cutoffTime"] = "Enter a time as HH:mm, for example 12:00.";
        }

        if (!Enum.IsDefined(rules.LateHandling))
        {
            errors["financeRules.lateHandling"] = "Choose Notice only or Flag for Finance.";
        }

        return errors;
    }

    /// <summary>A 24-hour "HH:mm" time, or null when it is not one.</summary>
    public static TimeOnly? ParseTime(string? value) =>
        TimeOnly.TryParseExact(value?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : null;
}
