using ERFWeb.Domain;
using ERFWeb.Domain.Finance;
using ERFWeb.Domain.Reporting;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class ExpenseSummaryBuilderTests
{
    private static SummaryLine Line(long employee, string name, int day, ExpenseCategory category, decimal amount, string? reference = "ERF-1") =>
        new(employee, name, "Sales", reference, ReportStatus.Approved, new DateOnly(2026, 10, day), category, "x", "MDMPI", amount);

    [Fact]
    public void Build_TotalsPerEmployeeAndCategory()
    {
        var summary = ExpenseSummaryBuilder.Build(
        [
            Line(2, "Zed", 3, ExpenseCategory.Meals, 250m),
            Line(1, "Ann", 1, ExpenseCategory.Toll, 84m),
            Line(1, "Ann", 2, ExpenseCategory.Toll, 84m),
            Line(1, "Ann", 2, ExpenseCategory.Parking, 40m),
        ]);

        Assert.Equal(458m, summary.Total);
        Assert.Equal(168m, summary.ByCategory[ExpenseCategory.Toll]);
        Assert.Equal(["Ann", "Zed"], summary.Employees.Select(e => e.EmployeeName));

        var ann = summary.Employees[0];
        Assert.Equal(3, ann.LineCount);
        Assert.Equal(208m, ann.Total);
        Assert.Equal(40m, ann.ByCategory[ExpenseCategory.Parking]);
    }

    [Fact]
    public void Build_EveryCategoryIsPresent_EvenWhenUnused()
    {
        var summary = ExpenseSummaryBuilder.Build([Line(1, "Ann", 1, ExpenseCategory.Meals, 10m)]);

        Assert.Equal(Enum.GetValues<ExpenseCategory>().Length, summary.ByCategory.Count);
        Assert.Equal(0m, summary.ByCategory[ExpenseCategory.Representation]);
        Assert.Equal(0m, summary.Employees[0].ByCategory[ExpenseCategory.Representation]);
    }

    [Fact]
    public void Build_OrdersLinesByDateThenEmployee()
    {
        var summary = ExpenseSummaryBuilder.Build(
        [
            Line(2, "Zed", 1, ExpenseCategory.Meals, 1m),
            Line(1, "Ann", 2, ExpenseCategory.Meals, 1m),
            Line(1, "Ann", 1, ExpenseCategory.Meals, 1m),
        ]);

        Assert.Equal([(1, "Ann"), (1, "Zed"), (2, "Ann")], summary.Lines.Select(l => (l.ExpenseDate.Day, l.EmployeeName)));
    }

    [Fact]
    public void Build_Empty_HasZeroTotals()
    {
        var summary = ExpenseSummaryBuilder.Build([]);

        Assert.Empty(summary.Employees);
        Assert.Equal(0m, summary.Total);
    }

    [Fact]
    public void PeriodError_RejectsReversedAndOverlongPeriods()
    {
        var start = new DateOnly(2026, 1, 1);
        Assert.Null(ExpenseSummaryBuilder.PeriodError(start, start));
        Assert.Null(ExpenseSummaryBuilder.PeriodError(start, start.AddDays(ExpenseSummaryBuilder.MaxPeriodDays - 1)));
        Assert.NotNull(ExpenseSummaryBuilder.PeriodError(start, start.AddDays(ExpenseSummaryBuilder.MaxPeriodDays)));
        Assert.NotNull(ExpenseSummaryBuilder.PeriodError(start, start.AddDays(-1)));
    }

    [Theory]
    [InlineData(0, AgingBucket.Days0To7)]
    [InlineData(7, AgingBucket.Days0To7)]
    [InlineData(8, AgingBucket.Days8To15)]
    [InlineData(15, AgingBucket.Days8To15)]
    [InlineData(16, AgingBucket.Days16To30)]
    [InlineData(30, AgingBucket.Days16To30)]
    [InlineData(31, AgingBucket.Days31To60)]
    [InlineData(60, AgingBucket.Days31To60)]
    [InlineData(61, AgingBucket.Over60Days)]
    public void FinanceAging_BucketsAreInclusive(int days, AgingBucket expected) =>
        Assert.Equal(expected, FinanceAging.BucketFor(days));
}
