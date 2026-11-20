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

/// <summary>Where the lodging rate comes from (Finance question 5).</summary>
public enum LodgingRateBasis
{
    /// <summary>The Lodging line of the claimant's rate profile in force on the expense date.</summary>
    RateProfile,

    /// <summary>One amount per night for everyone (<see cref="FinanceRuleSet.LodgingFlatRate"/>).</summary>
    Flat,
}

/// <summary>How Gas and Vehicle emergency purchase lines are verified (Finance questions 6 and 7).</summary>
public enum CategoryVerification
{
    /// <summary>No check beyond the normal approval.</summary>
    None,

    /// <summary>On final approval each line gets a verification request; Finance cannot clear until it is closed.</summary>
    Person,

    /// <summary>The line needs its receipt (gas receipt, vehicle purchase receipt) attached to the report.</summary>
    Document,

    /// <summary>Person and Document.</summary>
    Both,
}

/// <summary>What a Work With tag stops the tagged employee from claiming on that date (Finance question 3).</summary>
public enum WorkWithScope
{
    /// <summary>The meals the filer claimed on the tagging Meals line.</summary>
    MealsOnly,

    /// <summary>Every expense on that date.</summary>
    AllExpenses,
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

    /// <summary>Employees may claim Sponsorship.</summary>
    public bool SponsorshipEnabled { get; init; } = true;

    /// <summary>A report with a Sponsorship line cannot be filed without an Activity Request Form attachment.</summary>
    public bool SponsorshipRequiresDocument { get; init; } = true;

    /// <summary>Employees may claim Demo.</summary>
    public bool DemoEnabled { get; init; } = true;

    /// <summary>A report with a Demo line cannot be filed without an ISO-registered attendance sheet attachment.</summary>
    public bool DemoRequiresDocument { get; init; } = true;

    /// <summary>Document code of the ISO attendance sheet (printed in its header); an upload must show it or be confirmed.</summary>
    public string DemoFormCode { get; init; } = "R94";

    /// <summary>A Representation line needs the food or meal receipt attached (Representation cannot be switched off).</summary>
    public bool RepresentationRequiresDocument { get; init; } = true;

    /// <summary>Employees may claim Lodging.</summary>
    public bool LodgingEnabled { get; init; } = true;

    public LodgingRateBasis LodgingRateBasis { get; init; } = LodgingRateBasis.RateProfile;

    /// <summary>Per-night amount when <see cref="LodgingRateBasis"/> is Flat.</summary>
    public decimal LodgingFlatRate { get; init; }

    /// <summary>A per-night amount above the lodging rate is refused (no rate set means no cap).</summary>
    public bool LodgingCapAtRate { get; init; } = true;

    /// <summary>A Lodging line needs the official receipt from the hotel or lodging attached.</summary>
    public bool LodgingRequiresDocument { get; init; } = true;

    /// <summary>Employees may claim Gas.</summary>
    public bool GasEnabled { get; init; } = true;

    public CategoryVerification GasVerification { get; init; } = CategoryVerification.Both;

    /// <summary>Employees may claim an emergency purchase for the issued vehicle.</summary>
    public bool VehicleEmergencyEnabled { get; init; } = true;

    public CategoryVerification VehicleEmergencyVerification { get; init; } = CategoryVerification.Both;

    /// <summary>Document code of the ISO Job Order (JO); a Job Order upload must show it or be confirmed.</summary>
    public string JobOrderFormCode { get; init; } = "R42";

    /// <summary>A filed Work With tag stops the tagged employee claiming what the filer is liable for (requirement 2).</summary>
    public bool WorkWithMealsEnabled { get; init; } = true;

    public WorkWithScope WorkWithScope { get; init; } = WorkWithScope.MealsOnly;
}

/// <summary>Validation of the Finance rules the administrator saves; field names match the Rules page.</summary>
public static class FinanceRuleSetRules
{
    public const decimal MaxLodgingFlatRate = 1_000_000m;
    public const int MaxFormCodeLength = 20;

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

        if (!Enum.IsDefined(rules.LodgingRateBasis))
        {
            errors["financeRules.lodgingRateBasis"] = "Choose Rate profile or Flat amount.";
        }

        if (rules.LodgingFlatRate is < 0 or > MaxLodgingFlatRate)
        {
            errors["financeRules.lodgingFlatRate"] = "Enter an amount from 0 to 1,000,000.";
        }
        else if (rules.LodgingRateBasis == LodgingRateBasis.Flat && rules.LodgingFlatRate == 0)
        {
            errors["financeRules.lodgingFlatRate"] = "Enter the flat amount per night.";
        }

        if (!Enum.IsDefined(rules.GasVerification))
        {
            errors["financeRules.gasVerification"] = "Choose how Gas is verified.";
        }

        if (!Enum.IsDefined(rules.VehicleEmergencyVerification))
        {
            errors["financeRules.vehicleEmergencyVerification"] = "Choose how a vehicle emergency purchase is verified.";
        }

        if (!Enum.IsDefined(rules.WorkWithScope))
        {
            errors["financeRules.workWithScope"] = "Choose Meals only or All expenses.";
        }

        if (!IsFormCode(rules.DemoFormCode))
        {
            errors["financeRules.demoFormCode"] = "Enter the form code, for example R94 (letters, digits and -, up to 20).";
        }

        if (!IsFormCode(rules.JobOrderFormCode))
        {
            errors["financeRules.jobOrderFormCode"] = "Enter the form code, for example R42 (letters, digits and -, up to 20).";
        }

        return errors;
    }

    private static bool IsFormCode(string? value)
    {
        var code = value?.Trim() ?? string.Empty;
        return code.Length > 0 && code.Length <= MaxFormCodeLength && code.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
    }

    /// <summary>A 24-hour "HH:mm" time, or null when it is not one.</summary>
    public static TimeOnly? ParseTime(string? value) =>
        TimeOnly.TryParseExact(value?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : null;
}
