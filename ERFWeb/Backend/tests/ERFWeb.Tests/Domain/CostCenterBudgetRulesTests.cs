using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class CostCenterBudgetRulesTests
{
    private static BudgetSpend Spend(long employee, string name, ExpenseCategory category, decimal amount, int lines = 1) =>
        new(10, employee, name, category, lines, amount);

    [Fact]
    public void Standing_UnderBudget_ShowsRemainingAndPercent()
    {
        var s = CostCenterBudgetRules.Standing(1_200_000m, 834_500m);

        Assert.Equal(365_500m, s.Remaining);
        Assert.Equal(69.5m, s.PercentUsed);
        Assert.False(s.OverBudget);
    }

    [Fact]
    public void Standing_OverBudget_HasNegativeRemaining()
    {
        var s = CostCenterBudgetRules.Standing(300_000m, 312_000m);

        Assert.Equal(-12_000m, s.Remaining);
        Assert.Equal(104m, s.PercentUsed);
        Assert.True(s.OverBudget);
    }

    [Fact]
    public void Standing_ExactlyOnBudget_IsNotOver()
    {
        Assert.False(CostCenterBudgetRules.Standing(500m, 500m).OverBudget);
    }

    [Fact]
    public void Standing_NoBudget_ShowsOnlyWhatWasUsed()
    {
        var s = CostCenterBudgetRules.Standing(null, 250m);

        Assert.Null(s.Budget);
        Assert.Equal(250m, s.Used);
        Assert.Null(s.Remaining);
        Assert.Null(s.PercentUsed);
        Assert.False(s.OverBudget);
    }

    [Fact]
    public void Standing_ZeroBudget_IsOverOnceAnythingIsSpent()
    {
        Assert.False(CostCenterBudgetRules.Standing(0m, 0m).OverBudget);
        var s = CostCenterBudgetRules.Standing(0m, 1m);
        Assert.True(s.OverBudget);
        Assert.Null(s.PercentUsed);
    }

    [Theory]
    [InlineData(2026, true)]
    [InlineData(2000, true)]
    [InlineData(2100, true)]
    [InlineData(1999, false)]
    [InlineData(2101, false)]
    public void YearError_AcceptsOnlyTheSupportedRange(int year, bool valid)
    {
        Assert.Equal(valid, CostCenterBudgetRules.YearError(year) is null);
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("1200000.50", true)]
    [InlineData("-1", false)]
    [InlineData("10.555", false)]
    [InlineData("1000000000000", false)]
    public void AmountError_RejectsNegativeTooLargeOrFractionalCents(string amount, bool valid)
    {
        Assert.Equal(valid, CostCenterBudgetRules.AmountError(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)) is null);
    }

    [Fact]
    public void Period_IsTheCalendarYear()
    {
        Assert.Equal((new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), CostCenterBudgetRules.Period(2026));
    }

    [Fact]
    public void Employees_TotalsPerEmployee_LargestSpenderFirst()
    {
        var employees = CostCenterBudgetRules.Employees(
        [
            Spend(1, "Ann", ExpenseCategory.Meals, 100m, 2),
            Spend(1, "Ann", ExpenseCategory.Toll, 50m),
            Spend(2, "Zed", ExpenseCategory.Transportation, 400m, 3),
        ]);

        Assert.Equal(["Zed", "Ann"], employees.Select(e => e.EmployeeName));
        var ann = employees[1];
        Assert.Equal(150m, ann.Total);
        Assert.Equal(3, ann.LineCount);
        Assert.Equal(0m, ann.ByCategory[ExpenseCategory.Representation]);
    }

    [Fact]
    public void ByCategory_HasEveryCategory()
    {
        var totals = CostCenterBudgetRules.ByCategory([Spend(1, "Ann", ExpenseCategory.Meals, 10m), Spend(2, "Zed", ExpenseCategory.Meals, 5m)]);

        Assert.Equal(Enum.GetValues<ExpenseCategory>().Length, totals.Count);
        Assert.Equal(15m, totals[ExpenseCategory.Meals]);
    }
}
