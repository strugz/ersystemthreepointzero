using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class AllowanceCalculatorTests
{
    [Fact]
    public void Compute_SubtractsExcludedDays()
    {
        var result = AllowanceCalculator.Compute(totalDays: 3, minusDays: 1, dailyAmount: 500m);

        Assert.Equal(2, result.Multiplier);
        Assert.Equal(1000m, result.TotalAmount);
        Assert.Equal("(3Days - 1Days) * 500", result.ComputationText);
    }

    [Fact]
    public void Compute_NoExcludedDays_OmitsSubtraction()
    {
        var result = AllowanceCalculator.Compute(totalDays: 2, minusDays: 0, dailyAmount: 250m);

        Assert.Equal(2, result.Multiplier);
        Assert.Equal(500m, result.TotalAmount);
        Assert.Equal("(2Days) * 250", result.ComputationText);
    }
}
