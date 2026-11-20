using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class FleetCardRulesTests
{
    private static readonly FinanceRuleSet Rules = FinanceRuleSet.Default;
    private const string Listed = "No Shell station in the area - Toyota Vios - ABC 1234";
    private const string Purpose = "Client visit - Toyota Vios - ABC 1234";

    [Fact]
    public void Restricts_OnlyCardHoldersWhileTheRuleIsOnAndTypingIsOff()
    {
        Assert.True(FleetCardRules.Restricts(Rules, hasFleetCard: true));
        Assert.False(FleetCardRules.Restricts(Rules, hasFleetCard: false));
        Assert.False(FleetCardRules.Restricts(Rules with { FleetCardRuleEnabled = false }, hasFleetCard: true));
        Assert.False(FleetCardRules.Restricts(Rules with { FleetCardOtherReasonAllowed = true }, hasFleetCard: true));
    }

    [Fact]
    public void ReasonsFor_CardHoldersOnly_EvenWhenTypingIsAllowed()
    {
        Assert.Equal(Rules.FleetCardGasReasons, FleetCardRules.ReasonsFor(Rules with { FleetCardOtherReasonAllowed = true }, true));
        Assert.Null(FleetCardRules.ReasonsFor(Rules, false));
        Assert.Null(FleetCardRules.ReasonsFor(Rules with { FleetCardRuleEnabled = false }, true));
    }

    [Theory]
    [InlineData(Listed, true)]
    [InlineData("no shell  station in the area - Toyota Vios - ABC 1234", true)]
    [InlineData(Purpose, false)]
    [InlineData("", false)]
    public void LineProblem_GasNeedsAListedReason(string particulars, bool allowed) =>
        Assert.Equal(allowed, FleetCardRules.LineProblem(ExpenseCategory.Gas, particulars, Rules, true) is null);

    [Fact]
    public void LineProblem_IgnoresOtherCategoriesAndEmployeesWithoutACard()
    {
        Assert.Null(FleetCardRules.LineProblem(ExpenseCategory.VehicleEmergency, Purpose, Rules, true));
        Assert.Null(FleetCardRules.LineProblem(ExpenseCategory.Gas, Purpose, Rules, false));
    }

    [Fact]
    public void FilingProblems_OnePerUnlistedLine_ByDate()
    {
        var lines = new[]
        {
            new GasLineReason(new DateOnly(2026, 11, 18), Purpose),
            new GasLineReason(new DateOnly(2026, 11, 17), Listed),
            new GasLineReason(new DateOnly(2026, 11, 16), "Delivery - Toyota Vios - ABC 1234"),
        };

        var problems = FleetCardRules.FilingProblems(lines, Rules, true);

        Assert.Equal(2, problems.Count);
        Assert.StartsWith("Gas on 16 Nov 2026", problems[0], StringComparison.Ordinal);
        Assert.StartsWith("Gas on 18 Nov 2026", problems[1], StringComparison.Ordinal);
        Assert.Empty(FleetCardRules.FilingProblems(lines, Rules, false));
    }

    [Fact]
    public void NormalizeReasons_TrimsAndDropsBlanksAndRepeats() =>
        Assert.Equal(["Card offline", "No Shell"], FleetCardRules.NormalizeReasons([" Card  offline ", "", null, "card offline", "No Shell"]));

    [Fact]
    public void Validate_ChecksTheReasonList()
    {
        Assert.Empty(FinanceRuleSetRules.Validate(Rules));
        Assert.Empty(FinanceRuleSetRules.Validate(Rules with { FleetCardRuleEnabled = false, FleetCardGasReasons = [] }));
        Assert.Contains("financeRules.fleetCardGasReasons", FinanceRuleSetRules.Validate(Rules with { FleetCardGasReasons = [] }).Keys);
        Assert.Contains("financeRules.fleetCardGasReasons", FinanceRuleSetRules.Validate(Rules with { FleetCardGasReasons = ["Card - offline"] }).Keys);
        Assert.Contains("financeRules.fleetCardGasReasons",
            FinanceRuleSetRules.Validate(Rules with { FleetCardGasReasons = [new string('A', FleetCardRules.MaxReasonLength + 1)] }).Keys);
    }
}
