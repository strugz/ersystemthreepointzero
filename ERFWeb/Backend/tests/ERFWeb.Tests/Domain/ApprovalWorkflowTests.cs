using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Finance;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class ApprovalWorkflowTests
{
    private const long Ana = 100;    // claimant
    private const long Maria = 1;    // Level 1
    private const long Ramon = 2;    // Level 2
    private const long Stranger = 9;

    private static List<WorkflowStep> TwoSteps(ApprovalStatus first = ApprovalStatus.Pending, ApprovalStatus second = ApprovalStatus.Pending) =>
    [
        new(11, 1, 1, Maria, first),
        new(12, 2, 2, Ramon, second),
    ];

    private static WorkflowDecision Act(List<WorkflowStep> steps, long actor, ApprovalAction action, string? reason = null, ReportStatus status = ReportStatus.ForApproval) =>
        ApprovalWorkflow.Decide(status, steps, Ana, actor, action, reason);

    [Fact]
    public void Level1_approves_first_and_report_stays_for_approval()
    {
        var d = Act(TwoSteps(), Maria, ApprovalAction.Approve);
        Assert.True(d.IsAllowed);
        Assert.Equal(ApprovalStatus.Approved, d.StepChanges[11]);
        Assert.Null(d.NewReportStatus);
    }

    [Fact]
    public void Last_approval_approves_the_report()
    {
        var d = Act(TwoSteps(first: ApprovalStatus.Approved), Ramon, ApprovalAction.Approve);
        Assert.True(d.CompletesApproval);
    }

    [Fact]
    public void Level2_acting_first_is_out_of_order()
    {
        var d = Act(TwoSteps(), Ramon, ApprovalAction.Approve);
        Assert.Equal(WorkflowRefusal.Conflict, d.Refusal);
    }

    [Fact]
    public void Level2_override_marks_the_pending_level1_and_completes()
    {
        var d = Act(TwoSteps(), Ramon, ApprovalAction.Override, "Maria on leave; checked receipts myself");
        Assert.True(d.IsAllowed);
        Assert.Equal(ApprovalStatus.Overridden, d.StepChanges[11]);
        Assert.Equal(ApprovalStatus.Approved, d.StepChanges[12]);
        Assert.True(d.CompletesApproval);
    }

    [Fact]
    public void Override_needs_a_reason_and_a_level2()
    {
        Assert.Equal(WorkflowRefusal.Invalid, Act(TwoSteps(), Ramon, ApprovalAction.Override).Refusal);
        Assert.Equal(WorkflowRefusal.Forbidden, Act(TwoSteps(), Maria, ApprovalAction.Override, "x").Refusal);
    }

    [Fact]
    public void Return_supersedes_later_steps_and_returns_the_report()
    {
        var d = Act(TwoSteps(), Maria, ApprovalAction.Return, "Attach the toll receipt");
        Assert.Equal(ApprovalStatus.Returned, d.StepChanges[11]);
        Assert.Equal(ApprovalStatus.Superseded, d.StepChanges[12]);
        Assert.Equal(ReportStatus.Returned, d.NewReportStatus);
    }

    [Fact]
    public void Reject_is_final()
    {
        var d = Act(TwoSteps(first: ApprovalStatus.Approved), Ramon, ApprovalAction.Reject, "Personal expense");
        Assert.Equal(ApprovalStatus.Rejected, d.StepChanges[12]);
        Assert.Equal(ReportStatus.Rejected, d.NewReportStatus);
    }

    [Theory]
    [InlineData(ApprovalAction.Return)]
    [InlineData(ApprovalAction.Reject)]
    public void Return_and_reject_need_a_reason(ApprovalAction action) =>
        Assert.Equal(WorkflowRefusal.Invalid, Act(TwoSteps(), Maria, action, "  ").Refusal);

    [Fact]
    public void Claimant_and_strangers_cannot_act()
    {
        var own = ApprovalWorkflow.Decide(ReportStatus.ForApproval, [new(11, 1, 1, Ana, ApprovalStatus.Pending)], Ana, Ana, ApprovalAction.Approve, null);
        Assert.Equal(WorkflowRefusal.Forbidden, own.Refusal);
        Assert.Equal(WorkflowRefusal.Forbidden, Act(TwoSteps(), Stranger, ApprovalAction.Approve).Refusal);
    }

    [Fact]
    public void Acting_twice_or_after_the_report_moved_on_is_a_conflict()
    {
        Assert.Equal(WorkflowRefusal.Conflict, Act(TwoSteps(first: ApprovalStatus.Approved), Maria, ApprovalAction.Approve).Refusal);
        Assert.Equal(WorkflowRefusal.Conflict, Act(TwoSteps(), Maria, ApprovalAction.Approve, status: ReportStatus.Returned).Refusal);
    }

    [Fact]
    public void Permissions_match_the_actions()
    {
        var maria = ApprovalWorkflow.Permissions(ReportStatus.ForApproval, TwoSteps(), Ana, Maria);
        Assert.True(maria.CanApprove && maria.CanReturn && maria.CanReject && maria.CanAdjustLines);
        Assert.False(maria.CanOverride);

        var ramon = ApprovalWorkflow.Permissions(ReportStatus.ForApproval, TwoSteps(), Ana, Ramon);
        Assert.False(ramon.CanApprove || ramon.CanAdjustLines);
        Assert.True(ramon.CanOverride);

        var ana = ApprovalWorkflow.Permissions(ReportStatus.ForApproval, TwoSteps(), Ana, Ana);
        Assert.False(ana.CanApprove || ana.CanOverride || ana.CanAdjustLines);
    }

    [Fact]
    public void Amounts_lock_after_the_first_approval()
    {
        Assert.Null(ApprovalWorkflow.AdjustmentError(ReportStatus.ForApproval, TwoSteps(), Ana, Maria));
        Assert.NotNull(ApprovalWorkflow.AdjustmentError(ReportStatus.ForApproval, TwoSteps(first: ApprovalStatus.Approved), Ana, Ramon));
        Assert.NotNull(ApprovalWorkflow.AdjustmentError(ReportStatus.ForApproval, TwoSteps(), Ana, Ramon));
    }

    [Theory]
    [InlineData(500, 400, "Over the meal cap", true)]
    [InlineData(500, 500, "same", false)]
    [InlineData(500, 600, "more", false)]
    [InlineData(500, 400, " ", false)]
    [InlineData(500, -1, "negative", false)]
    public void Adjustment_only_lowers_with_a_reason(double current, double next, string reason, bool ok) =>
        Assert.Equal(ok, ApprovalWorkflow.AdjustedAmountError((decimal)current, (decimal)next, reason) is null);
}

public class FinanceRulesTests
{
    [Theory]
    [InlineData(FinanceStatus.Pending, FinanceStatus.ReceiptsIncomplete, "Missing toll receipt", true)]
    [InlineData(FinanceStatus.Pending, FinanceStatus.ReceiptsIncomplete, null, false)]
    [InlineData(FinanceStatus.Pending, FinanceStatus.ReceiptsReceived, null, true)]
    [InlineData(FinanceStatus.ReceiptsIncomplete, FinanceStatus.Pending, null, true)]
    [InlineData(FinanceStatus.ReceiptsIncomplete, FinanceStatus.ReceiptsReceived, null, true)]
    [InlineData(FinanceStatus.ReceiptsReceived, FinanceStatus.Pending, null, false)]
    [InlineData(FinanceStatus.ReceiptsReceived, FinanceStatus.ReceiptsIncomplete, "x", false)]
    [InlineData(FinanceStatus.Pending, FinanceStatus.Pending, null, false)]
    [InlineData(FinanceStatus.Pending, FinanceStatus.ClearedForPayment, null, false)]
    public void Receipt_transitions(FinanceStatus from, FinanceStatus to, string? remarks, bool ok) =>
        Assert.Equal(ok, FinanceRules.ReceiptTransitionError(from, to, remarks) is null);

    [Theory]
    [InlineData(FinanceStatus.ReceiptsReceived, true)]
    [InlineData(FinanceStatus.Pending, false)]
    [InlineData(FinanceStatus.ReceiptsIncomplete, false)]
    [InlineData(FinanceStatus.ClearedForPayment, false)]
    public void Clearing_needs_all_receipts(FinanceStatus status, bool ok) =>
        Assert.Equal(ok, FinanceRules.ClearError(status, null) is null);

    [Fact]
    public void Follow_ups_only_while_receipts_are_outstanding()
    {
        Assert.True(FinanceRules.AwaitsReceipts(FinanceStatus.Pending));
        Assert.True(FinanceRules.AwaitsReceipts(FinanceStatus.ReceiptsIncomplete));
        Assert.False(FinanceRules.AwaitsReceipts(FinanceStatus.ReceiptsReceived));
    }
}
