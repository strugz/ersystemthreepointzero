namespace ERSystem.Web.Domain.ApprovalReminders;

public static class ApprovalReminderSchedule
{
    public static int GetLatestDueReminderNumber(
        DateTime activeAtUtc,
        DateTime nowUtc,
        TimeZoneInfo timeZone,
        int initialDelayDays,
        DayOfWeek reminderDayOfWeek)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialDelayDays);

        var activeLocalDate = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(activeAtUtc), timeZone).Date;
        var currentLocalDate = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(nowUtc), timeZone).Date;
        var initialReminderDate = activeLocalDate.AddDays(initialDelayDays);

        if (currentLocalDate < initialReminderDate)
        {
            return 0;
        }

        var firstWeeklyReminderDate = GetNextDay(initialReminderDate, reminderDayOfWeek);
        if (currentLocalDate < firstWeeklyReminderDate)
        {
            return 1;
        }

        return ((currentLocalDate - firstWeeklyReminderDate).Days / 7) + 2;
    }

    public static int GetElapsedCalendarDays(
        DateTime activeAtUtc,
        DateTime nowUtc,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var activeLocalDate = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(activeAtUtc), timeZone).Date;
        var currentLocalDate = TimeZoneInfo.ConvertTimeFromUtc(AsUtc(nowUtc), timeZone).Date;
        return Math.Max(0, (currentLocalDate - activeLocalDate).Days);
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

    private static DateTime GetNextDay(DateTime date, DayOfWeek dayOfWeek)
    {
        var daysUntil = ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(daysUntil == 0 ? 7 : daysUntil);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
