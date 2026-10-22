using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class FareLegRulesTests
{
    private static readonly FareModeRule Taxi = new(1, IsActive: true, RequiresDetail: false);
    private static readonly FareModeRule Retired = new(2, IsActive: false, RequiresDetail: false);
    private static readonly FareModeRule Others = new(3, IsActive: true, RequiresDetail: true);

    private static readonly IReadOnlyDictionary<long, FareModeRule> Modes =
        new Dictionary<long, FareModeRule> { [1] = Taxi, [2] = Retired, [3] = Others };

    private static readonly IReadOnlySet<long> None = new HashSet<long>();

    private static IReadOnlyDictionary<string, string[]> Validate(IReadOnlySet<long>? alreadyOnLine = null, params FareLegInput[] legs) =>
        FareLegRules.Validate(legs, Modes, alreadyOnLine ?? None);

    [Fact]
    public void Validate_AcceptsActiveModes() =>
        Assert.Empty(Validate(null, new FareLegInput(1, null, 230m), new FareLegInput(3, "Kalesa", 50m)));

    [Fact]
    public void Validate_RequiresAModeOnEveryLeg()
    {
        var errors = Validate(null, new FareLegInput(1, null, 100m), new FareLegInput(null, null, 50m));

        Assert.Equal(["transports[1].fareModeId"], errors.Keys);
    }

    [Fact]
    public void Validate_RejectsUnknownMode() =>
        Assert.Contains("transports[0].fareModeId", Validate(null, new FareLegInput(99, null, 10m)).Keys);

    [Fact]
    public void Validate_RejectsInactiveModeOnNewLeg() =>
        Assert.Contains("transports[0].fareModeId", Validate(null, new FareLegInput(2, null, 10m)).Keys);

    [Fact]
    public void Validate_KeepsInactiveModeTheLineAlreadyUsed() =>
        Assert.Empty(Validate(new HashSet<long> { 2 }, new FareLegInput(2, null, 10m)));

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_OthersNeedsTheTypedMode(string? detail) =>
        Assert.Contains("transports[0].fareModeDetail", Validate(null, new FareLegInput(3, detail, 10m)).Keys);

    [Fact]
    public void Validate_LimitsTheTypedMode() =>
        Assert.Contains("transports[0].fareModeDetail", Validate(null, new FareLegInput(3, new string('x', FareLegRules.MaxDetailLength + 1), 10m)).Keys);

    [Fact]
    public void Validate_RejectsNegativeFares() =>
        Assert.Contains("transports", Validate(null, new FareLegInput(1, null, -1m)).Keys);

    [Fact]
    public void NormalizeDetail_KeepsTextOnlyForModesThatAskForIt()
    {
        Assert.Equal("Kalesa", FareLegRules.NormalizeDetail(Others, "  Kalesa "));
        Assert.Null(FareLegRules.NormalizeDetail(Taxi, "ignored"));
        Assert.Null(FareLegRules.NormalizeDetail(Others, " "));
    }
}
