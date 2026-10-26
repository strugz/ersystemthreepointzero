using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class ExpenseLineRulesTests
{
    [Theory]
    [InlineData(250, 1, 250)]
    [InlineData(250, 3, 750)]
    [InlineData(333.33, 3, 1000)]      // per-diem: whole pesos, like the desktop
    [InlineData(84.60, 1, 85)]         // no invoice, multiplier 1 -> whole pesos
    [InlineData(2.5, 1, 2)]            // midpoint rounds to even (VB Math.Round)
    [InlineData(3.5, 1, 4)]
    public void Total_WithoutInvoice_RoundsToWholePesos(decimal amount, int multiplier, decimal expected) =>
        Assert.Equal(expected, ExpenseLineRules.Total(amount, multiplier, hasInvoice: false));

    [Theory]
    [InlineData(84.60, 1, 84.60)]      // receipt amount is exact
    [InlineData(171.25, 1, 171.25)]
    [InlineData(84.60, 2, 169)]        // invoice but multiplied -> whole pesos, like the desktop
    public void Total_WithInvoice_KeepsCentavosOnlyForSingleMultiplier(decimal amount, int multiplier, decimal expected) =>
        Assert.Equal(expected, ExpenseLineRules.Total(amount, multiplier, hasInvoice: true));

    [Fact]
    public void Total_RejectsMultiplierBelowOne() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ExpenseLineRules.Total(10m, 0, false));

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("2667789054", true)]
    public void AllowsVat_OnlyWithInvoice(string? invoice, bool expected) =>
        Assert.Equal(expected, ExpenseLineRules.AllowsVat(invoice));

    [Fact]
    public void IsCompleteOrder_AcceptsEveryActiveLineOnceInAnyOrder() =>
        Assert.True(ExpenseLineRules.IsCompleteOrder([3, 1, 2], [1, 2, 3]));

    [Theory]
    [InlineData(new long[] { 1, 2 })]          // a line added since the client loaded
    [InlineData(new long[] { 1, 2, 3, 4 })]    // a line removed since
    [InlineData(new long[] { 1, 1, 2 })]       // duplicate
    [InlineData(new long[] { 1, 2, 9 })]       // another report's line
    public void IsCompleteOrder_RejectsStaleOrForeignLists(long[] ordered) =>
        Assert.False(ExpenseLineRules.IsCompleteOrder(ordered, [1, 2, 3]));

    [Fact]
    public void Validate_ValidLocalLine_HasNoErrors()
    {
        var errors = ExpenseLineRules.Validate(
            new DateOnly(2026, 8, 17), "SUCAT - NICHOLS (TOLL)", 84m, 0m, "2667789054", 1,
            isPerdiem: false, totalDays: null, minusDays: null,
            new DateOnly(2026, 8, 16), new DateOnly(2026, 8, 31));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_VatWithoutInvoice_IsRejected()
    {
        var errors = ExpenseLineRules.Validate(
            new DateOnly(2026, 8, 17), "MEAL", 250m, 12m, null, 1, false, null, null, null, null);

        Assert.Contains("vatAmount", errors.Keys);
    }

    [Fact]
    public void Validate_PerdiemWithNonPositiveMultiplier_IsRejected()
    {
        var errors = ExpenseLineRules.Validate(
            new DateOnly(2026, 8, 17), "ALLOWANCE", 500m, 0m, null, 1, isPerdiem: true, totalDays: 3, minusDays: 3, null, null);

        Assert.Contains("minusDays", errors.Keys);
        Assert.Contains("0 or less than 0 multiplier is not allowed.", errors["minusDays"]);
    }

    [Fact]
    public void Validate_DateOutsideReportPeriod_IsRejected()
    {
        var errors = ExpenseLineRules.Validate(
            new DateOnly(2026, 9, 1), "MEAL", 250m, 0m, null, 1, false, null, null,
            new DateOnly(2026, 8, 16), new DateOnly(2026, 8, 31));

        Assert.Contains("transactionDate", errors.Keys);
    }

    [Fact]
    public void Validate_MissingParticularsAndZeroAmount_ReportsBoth()
    {
        var errors = ExpenseLineRules.Validate(new DateOnly(2026, 8, 17), " ", 0m, 0m, null, 1, false, null, null, null, null);

        Assert.Contains("particulars", errors.Keys);
        Assert.Contains("amount", errors.Keys);
    }
}
