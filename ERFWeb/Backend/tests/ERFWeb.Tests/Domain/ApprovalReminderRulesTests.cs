using ERFWeb.Domain.Approval;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class ApprovalReminderRulesTests
{
    // 2026-10-23 is a Friday.
    private static readonly DateOnly Friday = new(2026, 10, 23);

    [Fact]
    public void WorkingDays_SkipWeekends()
    {
        Assert.Equal(0, ApprovalReminderRules.WorkingDaysBetween(Friday, Friday));
        Assert.Equal(0, ApprovalReminderRules.WorkingDaysBetween(Friday, Friday.AddDays(2)));
        Assert.Equal(1, ApprovalReminderRules.WorkingDaysBetween(Friday, Friday.AddDays(3)));
        Assert.Equal(5, ApprovalReminderRules.WorkingDaysBetween(Friday, Friday.AddDays(7)));
    }

    [Fact]
    public void WorkingDays_FiledOnASaturday_StartCountingMonday()
    {
        Assert.Equal(1, ApprovalReminderRules.WorkingDaysBetween(Friday.AddDays(1), Friday.AddDays(3)));
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(2, null)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    public void Reminder_FirstAtTheSla_ThenEveryTwoWorkingDays(int waited, int? expected)
    {
        Assert.Equal(expected, ApprovalReminderRules.ReminderNumberDue(waited, slaDays: 3));
    }

    [Fact]
    public void Reminder_ZeroSla_TurnsRemindersOff()
    {
        Assert.Null(ApprovalReminderRules.ReminderNumberDue(30, slaDays: 0));
    }

    [Theory]
    [InlineData(5, 3, 3, false)]
    [InlineData(6, 3, 3, true)]
    [InlineData(3, 3, 0, true)]
    [InlineData(2, 3, 0, false)]
    [InlineData(30, 0, 3, false)]
    public void Escalation_AfterTheSlaPlusTheEscalationDays(int waited, int sla, int after, bool expected)
    {
        Assert.Equal(expected, ApprovalReminderRules.EscalationDue(waited, sla, after));
    }

    private static readonly TimeSpan Manila = TimeSpan.FromHours(8);
    private static readonly TimeOnly Eight = new(8, 0);

    [Fact]
    public void NextRun_LaterTheSameWorkingDay()
    {
        var now = new DateTimeOffset(2026, 10, 26, 7, 0, 0, Manila); // Monday 07:00
        Assert.Equal(new DateTimeOffset(2026, 10, 26, 8, 0, 0, Manila), ApprovalReminderRules.NextRun(now, Manila, Eight));
    }

    [Fact]
    public void NextRun_AfterFridaysRun_IsMonday()
    {
        var now = new DateTimeOffset(2026, 10, 23, 8, 0, 0, Manila); // Friday, the run itself
        Assert.Equal(new DateTimeOffset(2026, 10, 26, 8, 0, 0, Manila), ApprovalReminderRules.NextRun(now, Manila, Eight));
    }

    [Fact]
    public void NextRun_UsesTheBusinessDate_NotUtc()
    {
        var now = new DateTimeOffset(2026, 10, 25, 23, 30, 0, TimeSpan.Zero); // Monday 07:30 in Manila
        Assert.Equal(new DateTimeOffset(2026, 10, 26, 8, 0, 0, Manila), ApprovalReminderRules.NextRun(now, Manila, Eight));
    }

    [Theory]
    [InlineData(26, 9, true)]   // Monday, after the run time
    [InlineData(26, 7, false)]  // Monday, before it
    [InlineData(24, 9, false)]  // Saturday
    public void RunDueToday_OnlyOnWorkingDaysAfterTheRunTime(int day, int hour, bool expected)
    {
        var now = new DateTimeOffset(2026, 10, day, hour, 0, 0, Manila);
        Assert.Equal(expected, ApprovalReminderRules.RunDueToday(now, Manila, Eight));
    }
}
