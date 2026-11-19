using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class LodgingRulesTests
{
    private static readonly FinanceRuleSet Defaults = FinanceRuleSet.Default;
    private static readonly FinanceRuleSet Flat = Defaults with { LodgingRateBasis = LodgingRateBasis.Flat, LodgingFlatRate = 1200m };

    [Fact]
    public void Rate_ComesFromTheProfile_ByDefault()
    {
        Assert.Equal(1500m, LodgingRules.Rate(Defaults, 1500m));
    }

    [Fact]
    public void Rate_IsTheFlatAmount_WhenFlat()
    {
        Assert.Equal(1200m, LodgingRules.Rate(Flat, 1500m));
    }

    [Fact]
    public void AmountAboveTheRate_IsRefused_WhenCapped()
    {
        Assert.Equal("The lodging rate is 1,500.00 a night; enter at most that.", LodgingRules.AmountProblem(1500.01m, 1500m, Defaults));
        Assert.Null(LodgingRules.AmountProblem(1500m, 1500m, Defaults));
    }

    [Fact]
    public void AmountAboveTheRate_IsAllowed_WhenNotCapped()
    {
        Assert.Null(LodgingRules.AmountProblem(5000m, 1500m, Defaults with { LodgingCapAtRate = false }));
    }

    [Fact]
    public void NoRateSet_MeansNoCap()
    {
        Assert.Null(LodgingRules.AmountProblem(5000m, 0m, Defaults));
    }

    [Theory]
    [InlineData(3, 1500.0, "(3 Nights) * 1500")]
    [InlineData(1, 1250.5, "(1 Night) * 1250.5")]
    public void ComputationText_NamesTheNights(int nights, double perNight, string expected)
    {
        Assert.Equal(expected, LodgingRules.ComputationText(nights, (decimal)perNight));
    }

    [Fact]
    public void FlatBasis_NeedsAnAmount()
    {
        var errors = FinanceRuleSetRules.Validate(Defaults with { LodgingRateBasis = LodgingRateBasis.Flat });

        Assert.Equal("Enter the flat amount per night.", errors["financeRules.lodgingFlatRate"]);
        Assert.Empty(FinanceRuleSetRules.Validate(Flat));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000001)]
    public void FlatRate_OutOfRange_IsRefused(int amount)
    {
        Assert.Contains("financeRules.lodgingFlatRate", FinanceRuleSetRules.Validate(Defaults with { LodgingFlatRate = amount }).Keys);
    }
}
