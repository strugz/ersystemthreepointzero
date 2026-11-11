using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class FilingCutoffRulesTests
{
    private static readonly TimeSpan Manila = TimeSpan.FromHours(8);

    // 2026-11-13 is a Friday; the cutoff week runs Saturday 7 to Friday 13 November.
    private static readonly DateOnly ThisFriday = new(2026, 11, 13);
    private static readonly DateOnly NextFriday = new(2026, 11, 20);

    private static DateTimeOffset Local(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0, Manila);

    [Theory]
    [InlineData(9, 8, 0, false)]     // Monday
    [InlineData(10, 17, 0, false)]   // Tuesday
    [InlineData(11, 11, 59, false)]  // Wednesday, a minute before
    [InlineData(11, 12, 0, true)]    // Wednesday, at the cutoff
    [InlineData(12, 9, 0, true)]     // Thursday
    [InlineData(13, 23, 59, true)]   // Friday night
    public void DefaultCutoff_IsWednesdayNoon(int day, int hour, int minute, bool late)
    {
        var status = FilingCutoffRules.Evaluate(Local(day, hour, minute), Manila, FinanceRuleSet.Default)!;

        Assert.Equal(late, status.IsLate);
        Assert.Equal(late ? NextFriday : ThisFriday, status.ProcessingDate);
    }

    [Theory]
    [InlineData(7)] // Saturday
    [InlineData(8)] // Sunday
    public void Weekend_GoesIntoTheComingFriday(int day)
    {
        var status = FilingCutoffRules.Evaluate(Local(day, 10), Manila, FinanceRuleSet.Default)!;

        Assert.False(status.IsLate);
        Assert.Equal(ThisFriday, status.ProcessingDate);
    }

    [Fact]
    public void UsesBusinessLocalTime()
    {
        // 04:30 UTC on Wednesday is 12:30 in Manila: past the noon cutoff.
        var status = FilingCutoffRules.Evaluate(new DateTimeOffset(2026, 11, 11, 4, 30, 0, TimeSpan.Zero), Manila, FinanceRuleSet.Default)!;

        Assert.True(status.IsLate);
    }

    [Fact]
    public void CustomCutoff_TuesdayFivePm()
    {
        var rules = FinanceRuleSet.Default with { CutoffWeekday = DayOfWeek.Tuesday, CutoffTime = "17:00" };

        Assert.False(FilingCutoffRules.Evaluate(Local(10, 16, 59), Manila, rules)!.IsLate);
        Assert.True(FilingCutoffRules.Evaluate(Local(10, 17, 0), Manila, rules)!.IsLate);
        Assert.Equal("Tuesday 5:00 PM", FilingCutoffRules.CutoffText(rules));
    }

    [Fact]
    public void Off_GivesNoNotice()
    {
        var rules = FinanceRuleSet.Default with { CutoffEnabled = false };

        Assert.Null(FilingCutoffRules.Evaluate(Local(12, 9), Manila, rules));
        Assert.False(FilingCutoffRules.FlagAsLate(Local(12, 9), Manila, rules with { LateHandling = LateFilingHandling.FlagForFinance }));
    }

    [Fact]
    public void FlagAsLate_OnlyWithFlagForFinance()
    {
        Assert.False(FilingCutoffRules.FlagAsLate(Local(12, 9), Manila, FinanceRuleSet.Default));

        var flag = FinanceRuleSet.Default with { LateHandling = LateFilingHandling.FlagForFinance };
        Assert.True(FilingCutoffRules.FlagAsLate(Local(12, 9), Manila, flag));
        Assert.False(FilingCutoffRules.FlagAsLate(Local(10, 9), Manila, flag));
    }

    [Theory]
    [InlineData(DayOfWeek.Saturday, "12:00", "financeRules.cutoffWeekday")]
    [InlineData(DayOfWeek.Sunday, "12:00", "financeRules.cutoffWeekday")]
    [InlineData(DayOfWeek.Wednesday, "25:00", "financeRules.cutoffTime")]
    [InlineData(DayOfWeek.Wednesday, "noon", "financeRules.cutoffTime")]
    public void Validate_RefusesWeekendsAndBadTimes(DayOfWeek day, string time, string field)
    {
        var errors = FinanceRuleSetRules.Validate(FinanceRuleSet.Default with { CutoffWeekday = day, CutoffTime = time });

        Assert.Contains(field, errors.Keys);
    }

    [Fact]
    public void Validate_AcceptsTheDefault() => Assert.Empty(FinanceRuleSetRules.Validate(FinanceRuleSet.Default));
}
