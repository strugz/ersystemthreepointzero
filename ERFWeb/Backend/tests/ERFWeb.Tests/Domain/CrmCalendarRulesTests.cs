using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class CrmCalendarRulesTests
{
    private static readonly DateOnly From = new(2026, 11, 16);
    private static readonly DateOnly To = new(2026, 11, 20);

    [Fact]
    public void PeriodDays_ListsEveryDay_BothEndsIncluded() =>
        Assert.Equal([From, From.AddDays(1), From.AddDays(2), From.AddDays(3), To], CrmCalendarRules.PeriodDays(From, To));

    [Fact]
    public void PeriodDays_IsEmpty_WhenTheEndIsBeforeTheStart() =>
        Assert.Empty(CrmCalendarRules.PeriodDays(To, From));

    [Fact]
    public void FitsCrmRange_AllowsSixtyTwoDays_NotSixtyThree()
    {
        Assert.True(CrmCalendarRules.FitsCrmRange(From, From.AddDays(61)));
        Assert.False(CrmCalendarRules.FitsCrmRange(From, From.AddDays(62)));
        Assert.False(CrmCalendarRules.FitsCrmRange(To, From));
    }

    [Fact]
    public void OneDayActivity_CoversItsDate() =>
        Assert.Equal([From.AddDays(1)], CrmCalendarRules.DaysCovered(From.AddDays(1), From.AddDays(1), From, To));

    [Fact]
    public void ActivityWithoutAnEnd_CoversItsDate() =>
        Assert.Equal([From], CrmCalendarRules.DaysCovered(From, null, From, To));

    [Fact]
    public void OvernightActivity_CoversEveryDayToItsEnd() =>
        Assert.Equal([From.AddDays(2), From.AddDays(3)], CrmCalendarRules.DaysCovered(From.AddDays(2), From.AddDays(3), From, To));

    [Theory]
    [InlineData("VL", true)]
    [InlineData("sl", true)]
    [InlineData(" Holiday ", true)]
    [InlineData("Convention", false)]
    [InlineData("MDMPI Office", false)]
    [InlineData("EnRoute", false)]
    [InlineData("Others", false)]
    [InlineData(null, false)]
    public void IsTimeOff_OnlyForLeaveAndHolidays(string? activityType, bool expected) =>
        Assert.Equal(expected, CrmCalendarRules.IsTimeOff(activityType));

    [Fact]
    public void ActivityRunningPastThePeriod_IsCutToThePeriod()
    {
        Assert.Equal([To], CrmCalendarRules.DaysCovered(To, To.AddDays(2), From, To));
        Assert.Equal([From], CrmCalendarRules.DaysCovered(From.AddDays(-2), From, From, To));
    }
}
