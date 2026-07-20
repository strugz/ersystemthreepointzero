namespace ERSystem.Web.Domain.ApprovalReminders;

public static class ApprovalReminderSchedule
{
    public static int GetDueReminderNumber(
        DateTime activeAtUtc,
        DateTime nowUtc,
        TimeZoneInfo timeZone,
        int initialDelayDays,
        int repeatIntervalDays)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialDelayDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repeatIntervalDays);

        var activeLocalDate = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(activeAtUtc), timeZone).Date;
        var currentLocalDate = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(nowUtc), timeZone).Date;
        var elapsedCalendarDays = (currentLocalDate - activeLocalDate).Days;

        if (elapsedCalendarDays < initialDelayDays)
        {
            return 0;
        }

        return ((elapsedCalendarDays - initialDelayDays) / repeatIntervalDays) + 1;
    }

    public static DateTime GetNextRunUtc(DateTime nowUtc, TimeZoneInfo timeZone, TimeOnly runAtLocalTime)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var currentLocal = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(nowUtc), timeZone);
        var candidateLocal = DateTime.SpecifyKind(
            currentLocal.Date.Add(runAtLocalTime.ToTimeSpan()),
            DateTimeKind.Unspecified);

        if (candidateLocal <= currentLocal)
        {
            candidateLocal = candidateLocal.AddDays(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(candidateLocal, timeZone);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
