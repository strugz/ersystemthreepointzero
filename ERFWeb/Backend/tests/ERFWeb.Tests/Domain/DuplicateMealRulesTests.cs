using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class DuplicateMealRulesTests
{
    private static readonly DateOnly Day = new(2026, 11, 17);

    [Fact]
    public void SameMealTwice_OnThisReport_IsRefused() =>
        Assert.Equal(
            "You already claimed Lunch on 17 Nov 2026 on this report. Each meal is claimed once a day.",
            DuplicateMealRules.Problem(Day, ["Lunch", "Dinner"], [new ClaimedMeals(null, true, ["Lunch"])]));

    [Fact]
    public void SameMeal_OnAnotherReport_NamesIt() =>
        Assert.Equal(
            "You already claimed Lunch and Dinner on 17 Nov 2026 on ERF-2026-000004. Each meal is claimed once a day.",
            DuplicateMealRules.Problem(Day, ["Dinner", "Lunch"], [new ClaimedMeals("ERF-2026-000004", false, ["Lunch", "Dinner"])]));

    [Fact]
    public void OtMealSpellings_AreTheSameMeal() =>
        Assert.NotNull(DuplicateMealRules.Problem(Day, ["OT Meal"], [new ClaimedMeals(null, true, ["OTMeal"])]));

    [Fact]
    public void OtherMeals_AndLinesWithoutAMealType_AreAllowed()
    {
        Assert.Null(DuplicateMealRules.Problem(Day, ["Dinner"], [new ClaimedMeals(null, true, ["Lunch"])]));
        Assert.Null(DuplicateMealRules.Problem(Day, [], [new ClaimedMeals(null, true, ["Lunch"])]));
        Assert.Null(DuplicateMealRules.Problem(Day, ["Lunch"], [new ClaimedMeals(null, true, [])]));
    }

    [Fact]
    public void FilingBlockers_IncludeMealProblems() =>
        Assert.Equal(["twice"], ReportWorkflowRules.FilingBlockers(
            ReportStatus.New, 1, 1, "Field work", ReportType.Reimbursement, 0m, 0m, mealProblems: ["twice"]));
}
