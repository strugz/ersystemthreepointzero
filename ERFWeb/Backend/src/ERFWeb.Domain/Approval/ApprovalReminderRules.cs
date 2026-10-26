namespace ERFWeb.Domain.Approval;

/// <summary>
/// SLA reminders for approval steps (plan 13.1: "Overdue Level 1 → reminder, then Level 2 notified").
/// A step's clock starts when it becomes the approver's turn and counts working days (Monday to
/// Friday) in the business time zone; there is no holiday calendar. Once a step has waited its
/// level's SLA, the approver is reminded, and again every <see cref="RepeatEveryWorkingDays"/>
/// working days until they act. A Level 1 step that is still waiting
/// <c>escalationAfterDays</c> working days past its SLA is reported once to the report's Level 2.
/// An SLA of 0 turns reminders off for that level.
/// </summary>
public static class ApprovalReminderRules
{
    public const int RepeatEveryWorkingDays = 2;

    /// <summary>Working days after <paramref name="since"/> up to and including <paramref name="today"/>.</summary>
    public static int WorkingDaysBetween(DateOnly since, DateOnly today)
    {
        var days = 0;
        for (var d = since.AddDays(1); d <= today; d = d.AddDays(1))
        {
            if (IsWorkingDay(d))
            {
                days++;
            }
        }

        return days;
    }

    public static bool IsWorkingDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    /// <summary>
    /// The reminder the approver is due today (1 for the first), or null when the step is not yet
    /// overdue or reminders are off. A run that was missed does not send the reminders it skipped:
    /// only the current number is due.
    /// </summary>
    public static int? ReminderNumberDue(int workingDaysWaited, int slaDays)
    {
        if (slaDays <= 0 || workingDaysWaited < slaDays)
        {
            return null;
        }

        return 1 + ((workingDaysWaited - slaDays) / RepeatEveryWorkingDays);
    }

    /// <summary>True when a waiting Level 1 step should now be reported to the report's Level 2.</summary>
    public static bool EscalationDue(int workingDaysWaited, int slaDaysLevel1, int escalationAfterDays) =>
        slaDaysLevel1 > 0 && workingDaysWaited >= slaDaysLevel1 + Math.Max(0, escalationAfterDays);

    /// <summary>The first run strictly after <paramref name="now"/>: <paramref name="runAt"/> local time on a working day.</summary>
    public static DateTimeOffset NextRun(DateTimeOffset now, TimeSpan utcOffset, TimeOnly runAt)
    {
        var date = DateOnly.FromDateTime(now.ToOffset(utcOffset).DateTime);
        while (true)
        {
            var candidate = new DateTimeOffset(date.ToDateTime(runAt), utcOffset);
            if (candidate > now && IsWorkingDay(date))
            {
                return candidate;
            }

            date = date.AddDays(1);
        }
    }

    /// <summary>True on a working day once <paramref name="runAt"/> has passed: a service started late still runs today.</summary>
    public static bool RunDueToday(DateTimeOffset now, TimeSpan utcOffset, TimeOnly runAt)
    {
        var local = now.ToOffset(utcOffset);
        return IsWorkingDay(DateOnly.FromDateTime(local.DateTime)) && TimeOnly.FromDateTime(local.DateTime) >= runAt;
    }
}
