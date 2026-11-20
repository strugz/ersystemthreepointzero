using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class WorkWithMealRulesTests
{
    private static readonly DateOnly Day = new(2026, 11, 10);
    private static readonly FinanceRuleSet On = FinanceRuleSet.Default;
    private static readonly WorkWithTag LunchTag = new("Hospital A Filer", Day, ExpenseCategory.Meals, ["Lunch"]);

    private static WorkWithClaim Meal(params string[] types) => new(Day, ExpenseCategory.Meals, types);

    [Fact]
    public void Defaults_AreOn_AndMealsOnly()
    {
        Assert.True(On.WorkWithMealsEnabled);
        Assert.Equal(WorkWithScope.MealsOnly, On.WorkWithScope);
    }

    [Fact]
    public void MealsOnly_RefusesTheSameMeal_OnTheSameDate()
    {
        var problem = WorkWithMealRules.Problem(Meal("Lunch", "Dinner"), [LunchTag], On);
        Assert.Equal("Hospital A Filer tagged you as Work With and claimed your Lunch on 10 Nov 2026.", problem);
    }

    [Fact]
    public void MealsOnly_AllowsOtherMeals_OtherDates_AndOtherCategories()
    {
        Assert.Null(WorkWithMealRules.Problem(Meal("Dinner"), [LunchTag], On));
        Assert.Null(WorkWithMealRules.Problem(Meal("Lunch") with { Date = Day.AddDays(1) }, [LunchTag], On));
        Assert.Null(WorkWithMealRules.Problem(new WorkWithClaim(Day, ExpenseCategory.Transportation, []), [LunchTag], On));
        Assert.Null(WorkWithMealRules.Problem(Meal("Lunch"), [LunchTag with { Category = ExpenseCategory.Transportation }], On));
    }

    [Fact]
    public void MealsOnly_MatchesOtMealSpellings()
    {
        var tag = LunchTag with { MealTypes = ["OT Meal"] };
        Assert.Equal(
            "Hospital A Filer tagged you as Work With and claimed your OT meal on 10 Nov 2026.",
            WorkWithMealRules.Problem(Meal("OTMeal"), [tag], On));
    }

    [Fact]
    public void AllExpenses_RefusesAnyLineThatDate()
    {
        var rules = On with { WorkWithScope = WorkWithScope.AllExpenses };
        var tag = LunchTag with { Category = ExpenseCategory.Transportation, MealTypes = [] };
        Assert.Equal(
            "Hospital A Filer tagged you as Work With on 10 Nov 2026, so you cannot claim expenses for that day.",
            WorkWithMealRules.Problem(new WorkWithClaim(Day, ExpenseCategory.Parking, []), [tag], rules));
    }

    [Fact]
    public void Off_AllowsEverything_AndHasNoNotice()
    {
        var rules = On with { WorkWithMealsEnabled = false };
        Assert.Null(WorkWithMealRules.Problem(Meal("Lunch"), [LunchTag], rules));
        Assert.Null(WorkWithMealRules.Notice(rules));
        Assert.NotNull(WorkWithMealRules.Notice(On));
    }

    [Theory]
    [InlineData(ReportStatus.New, true)]
    [InlineData(ReportStatus.ForApproval, true)]
    [InlineData(ReportStatus.Returned, true)]
    [InlineData(ReportStatus.Approved, true)]
    [InlineData(ReportStatus.Rejected, false)]
    public void EveryReportButRejectedBinds(ReportStatus status, bool binds) => Assert.Equal(binds, WorkWithMealRules.Binds(status));

    [Fact]
    public void Names_SplitsPickerCrmAndTypedLists()
    {
        Assert.Equal(["Dela Cruz", "Juan Santos", "Ana Reyes"], WorkWithMealRules.Names("Dela Cruz, Juan Santos/Ana Reyes"));
        Assert.Equal(["Dela Cruz", "Ana Reyes"], WorkWithMealRules.Names("Dela Cruz and Ana Reyes"));
        Assert.Empty(WorkWithMealRules.Names("  "));
    }

    [Fact]
    public void Covered_NamesTheSharedMeals_OrExpensesForAllExpenses()
    {
        Assert.Equal("Lunch", WorkWithMealRules.Covered(LunchTag, Meal("Breakfast", "Lunch"), On));
        Assert.Null(WorkWithMealRules.Covered(LunchTag, Meal("Dinner"), On));
        Assert.Equal("expenses", WorkWithMealRules.Covered(LunchTag, Meal("Dinner"), On with { WorkWithScope = WorkWithScope.AllExpenses }));
    }

    [Fact]
    public void Warnings_NameTheOtherPersonReportAndState()
    {
        Assert.Equal(
            "Hospital A Filer paid for this Lunch on 10 Nov 2026 as Work With (ERF-2026-000456, for approval). Hospital A Filer claims it; remove this line or lower it.",
            WorkWithMealRules.PaidByWarning("Hospital A Filer", "ERF-2026-000456", ReportStatus.ForApproval, false, "Lunch", Day));
        Assert.Equal(
            "Dela Cruz also claimed this Lunch on 10 Nov 2026 (ERF-2026-000123, approved, cleared for payment). You paid as Work With, so you claim it; Finance sees both.",
            WorkWithMealRules.AlsoClaimedWarning("Dela Cruz", "ERF-2026-000123", ReportStatus.Approved, true, "Lunch", Day));
        Assert.Contains("(draft)", WorkWithMealRules.AlsoClaimedWarning("Dela Cruz", null, ReportStatus.New, false, "Lunch", Day), StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownScope_IsRefused() =>
        Assert.Equal(["financeRules.workWithScope"], FinanceRuleSetRules.Validate(On with { WorkWithScope = (WorkWithScope)9 }).Keys);

    [Fact]
    public void FilingBlockers_IncludeWorkWithProblems()
    {
        var blockers = ReportWorkflowRules.FilingBlockers(
            ReportStatus.New, 1, 1, "Field work", ReportType.Reimbursement, 0m, 0m, workWithProblems: ["covered"]);
        Assert.Equal(["covered"], blockers);
    }
}
