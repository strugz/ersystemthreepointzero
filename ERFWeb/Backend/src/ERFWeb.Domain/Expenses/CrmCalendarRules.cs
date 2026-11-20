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
