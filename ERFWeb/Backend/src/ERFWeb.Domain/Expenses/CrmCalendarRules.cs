namespace ERFWeb.Domain.Expenses;

/// <summary>
/// The claimant's CRM calendar on the review page: one row per day of the report period, and each CRM activity on
/// every day it covers (a multi-day activity runs from its date to the date it ended, as the CRM team agreed for
/// overnight work).
/// </summary>
public static class CrmCalendarRules
{
    /// <summary>The CRM lists at most this many days in one call.</summary>
    public const int MaxDays = 62;

    /// <summary>
    /// The CRM's other-activity types that mean the employee was off work (vacation leave, sick leave, holiday), so a
    /// signatory is warned about expense lines on that day. Convention, MDMPI Office, EnRoute and Others can carry costs.
    /// </summary>
    private static readonly string[] TimeOffTypes = ["VL", "SL", "Holiday"];

    public static bool IsTimeOff(string? activityType) =>
        activityType is not null && TimeOffTypes.Contains(activityType.Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool FitsCrmRange(DateOnly from, DateOnly to) => to >= from && to.DayNumber - from.DayNumber + 1 <= MaxDays;

    /// <summary>Every day from <paramref name="from"/> to <paramref name="to"/>, both included.</summary>
    public static IReadOnlyList<DateOnly> PeriodDays(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            return [];
        }

        return Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(from.AddDays).ToList();
    }

    /// <summary>The days of the period an activity covers, from <paramref name="activityDate"/> to <paramref name="endDate"/>.</summary>
    public static IEnumerable<DateOnly> DaysCovered(DateOnly activityDate, DateOnly? endDate, DateOnly from, DateOnly to)
    {
        var last = endDate is { } end && end > activityDate ? end : activityDate;
        var first = activityDate < from ? from : activityDate;
        if (last > to)
        {
            last = to;
        }

        return PeriodDays(first, last);
    }
}
