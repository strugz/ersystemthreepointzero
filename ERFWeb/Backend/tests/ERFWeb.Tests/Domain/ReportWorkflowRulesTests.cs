using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class ReportWorkflowRulesTests
{
    [Theory]
    [InlineData(ReportStatus.New, true)]
    [InlineData(ReportStatus.Returned, true)]
    [InlineData(ReportStatus.ForApproval, false)]
    [InlineData(ReportStatus.Approved, false)]
    public void CanEdit_And_CanFile_OnlyWhileNotInApproval(ReportStatus status, bool expected)
    {
        Assert.Equal(expected, ReportWorkflowRules.CanEdit(status));
        Assert.Equal(expected, ReportWorkflowRules.CanFile(status));
    }

    [Theory]
    [InlineData(ReportStatus.New, false)]
    [InlineData(ReportStatus.Returned, false)]
    [InlineData(ReportStatus.ForApproval, true)]
    [InlineData(ReportStatus.Approved, false)]
    public void CanReopen_OnlyFromPipelineStates(ReportStatus status, bool expected) =>
        Assert.Equal(expected, ReportWorkflowRules.CanReopen(status, anyApproverActed: false));

    [Fact]
    public void CanReopen_NotAfterAnApproverActed() =>
        Assert.False(ReportWorkflowRules.CanReopen(ReportStatus.ForApproval, anyApproverActed: true));

    [Fact]
    public void CanDelete_OnlyNeverFiledNewReport()
    {
        Assert.True(ReportWorkflowRules.CanDelete(ReportStatus.New, null));
        Assert.False(ReportWorkflowRules.CanDelete(ReportStatus.New, DateTimeOffset.UtcNow));
        Assert.False(ReportWorkflowRules.CanDelete(ReportStatus.Returned, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void FilingBlockers_ReadyReport_HasNone()
    {
        var blockers = ReportWorkflowRules.FilingBlockers(
            ReportStatus.New, activeLineCount: 3, employeeHasSignature: true, approverCount: 2,
            purpose: "reimbursement", ReportType.Reimbursement, cashAdvanceAmount: 0m, revolvingFundAmount: 0m);

        Assert.Empty(blockers);
    }

    [Fact]
    public void FilingBlockers_ListsEveryMissingPrecondition()
    {
        var blockers = ReportWorkflowRules.FilingBlockers(
            ReportStatus.ForApproval, activeLineCount: 0, employeeHasSignature: false, approverCount: 0,
            purpose: null, ReportType.CashAdvanceLiquidation, cashAdvanceAmount: 0m, revolvingFundAmount: 0m);

        Assert.Equal(6, blockers.Count);
        Assert.Contains(blockers, b => b.Contains("No expense data", StringComparison.Ordinal));
        Assert.Contains(blockers, b => b.Contains("signature", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(blockers, b => b.Contains("approval route", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(blockers, b => b.Contains("cash advance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilingBlockers_CashAdvanceOnlyRequiredForLiquidation()
    {
        var reimbursement = ReportWorkflowRules.FilingBlockers(ReportStatus.Returned, 1, true, 1, "trip", ReportType.Reimbursement, 0m, 0m);
        var liquidation = ReportWorkflowRules.FilingBlockers(ReportStatus.Returned, 1, true, 1, "trip", ReportType.CashAdvanceLiquidation, 0m, 0m);

        Assert.Empty(reimbursement);
        Assert.Single(liquidation);
    }

    [Fact]
    public void FilingBlockers_RevolvingFundRequiredOnlyForReplenishment()
    {
        var missing = ReportWorkflowRules.FilingBlockers(ReportStatus.New, 1, true, 1, "fund", ReportType.RevolvingFund, 0m, 0m);
        var present = ReportWorkflowRules.FilingBlockers(ReportStatus.New, 1, true, 1, "fund", ReportType.RevolvingFund, 0m, 5000m);
        var liquidation = ReportWorkflowRules.FilingBlockers(ReportStatus.New, 1, true, 1, "trip", ReportType.CashAdvanceLiquidation, 3000m, 0m);

        Assert.Single(missing);
        Assert.Contains("revolving fund", missing[0], StringComparison.OrdinalIgnoreCase);
        Assert.Empty(present);
        Assert.Empty(liquidation);
    }

    [Theory]
    [InlineData(ReportType.CashAdvanceLiquidation, true, false)]
    [InlineData(ReportType.RevolvingFund, false, true)]
    [InlineData(ReportType.Reimbursement, false, false)]
    public void CashAdvanceAndRevolvingFund_BelongToOneTypeEach(ReportType type, bool cash, bool revolving)
    {
        Assert.Equal(cash, ReportWorkflowRules.UsesCashAdvance(type));
        Assert.Equal(revolving, ReportWorkflowRules.UsesRevolvingFund(type));
    }
}
