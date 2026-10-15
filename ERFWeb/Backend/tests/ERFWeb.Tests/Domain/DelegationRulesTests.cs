using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class DelegationRulesTests
{
    private const long Ana = 100;    // claimant
    private const long Maria = 1;    // Level 1
    private const long Ramon = 2;    // Level 2
    private const long Pedro = 3;    // another Level 1
    private const long Lucia = 4;    // another Level 2
    private static readonly DateOnly Today = new(2026, 10, 15);

    private static DelegationProposal Own(long signatory, int? level, long @delegate, int? delegateLevel, int startOffset = 0, int days = 5) =>
        new(signatory, signatory, level, @delegate, delegateLevel, Today.AddDays(startOffset), Today.AddDays(startOffset + days - 1), false);

    private static DelegationVerdict Check(DelegationProposal p, params DelegationWindow[] existing) =>
        DelegationRules.Check(p, existing, Today);

    [Fact]
    public void Level1_may_delegate_to_another_level1_or_a_level2()
    {
        Assert.True(Check(Own(Maria, 1, Pedro, 1)).IsAllowed);
        Assert.True(Check(Own(Maria, 1, Lucia, 2)).IsAllowed);
        Assert.False(Check(Own(Maria, 1, Pedro, 1)).IsForced);
    }

    [Fact]
    public void Level2_cannot_delegate_to_a_level1()
    {
        var v = Check(Own(Ramon, 2, Maria, 1));
        Assert.Equal(WorkflowRefusal.Invalid, v.Refusal);
        Assert.Contains("Level 2", v.Reason);
    }

    [Fact]
    public void Delegate_must_be_a_signatory_and_not_the_signatory()
    {
        Assert.Equal(WorkflowRefusal.Invalid, Check(Own(Maria, 1, Ana, null)).Refusal);
        Assert.Equal(WorkflowRefusal.Invalid, Check(Own(Maria, 1, Maria, 1)).Refusal);
        Assert.Equal(WorkflowRefusal.Invalid, Check(Own(Ana, null, Maria, 1)).Refusal);
    }

    [Fact]
    public void Dates_must_be_in_order_not_past_and_within_the_limit()
    {
        var reversed = new DelegationProposal(Maria, Maria, 1, Pedro, 1, Today.AddDays(3), Today.AddDays(1), false);
        Assert.Equal(WorkflowRefusal.Invalid, Check(reversed).Refusal);
        Assert.Equal(WorkflowRefusal.Invalid, Check(Own(Maria, 1, Pedro, 1, startOffset: -10, days: 3)).Refusal);
        Assert.Equal(WorkflowRefusal.Invalid, Check(Own(Maria, 1, Pedro, 1, days: DelegationRules.MaxDays + 1)).Refusal);
        Assert.True(Check(Own(Maria, 1, Pedro, 1, startOffset: -2, days: 5)).IsAllowed);
    }

    [Fact]
    public void Overlapping_live_delegation_is_a_conflict_but_cancelled_or_ended_ones_are_not()
    {
        var live = new DelegationWindow(7, Maria, Lucia, Today.AddDays(2), Today.AddDays(8), false);
        Assert.Equal(WorkflowRefusal.Conflict, Check(Own(Maria, 1, Pedro, 1), live).Refusal);

        var cancelled = live with { IsCancelled = true };
        var ended = new DelegationWindow(8, Maria, Lucia, Today.AddDays(-9), Today.AddDays(-1), false);
        var someoneElse = live with { SignatoryUserId = Pedro };
        Assert.True(Check(Own(Maria, 1, Pedro, 1), cancelled, ended, someoneElse).IsAllowed);
    }

    [Fact]
    public void Level2_may_assign_for_a_level1_they_are_over()
    {
        var forced = new DelegationProposal(Ramon, Maria, 1, Pedro, 1, Today, Today.AddDays(4), ActorIsLevel2Over: true);
        var v = Check(forced);
        Assert.True(v.IsAllowed);
        Assert.True(v.IsForced);
    }

    [Fact]
    public void Assigning_for_someone_else_needs_level2_over_a_level1()
    {
        var notOver = new DelegationProposal(Ramon, Maria, 1, Pedro, 1, Today, Today.AddDays(4), ActorIsLevel2Over: false);
        Assert.Equal(WorkflowRefusal.Forbidden, Check(notOver).Refusal);

        var forALevel2 = new DelegationProposal(Lucia, Ramon, 2, Lucia, 2, Today, Today.AddDays(4), ActorIsLevel2Over: true);
        Assert.Equal(WorkflowRefusal.Forbidden, Check(forALevel2).Refusal);
    }

    [Fact]
    public void Level2_cannot_assign_themselves_as_the_level1_delegate()
    {
        var self = new DelegationProposal(Ramon, Maria, 1, Ramon, 2, Today, Today.AddDays(4), ActorIsLevel2Over: true);
        var v = Check(self);
        Assert.Equal(WorkflowRefusal.Invalid, v.Refusal);
        Assert.Contains("override", v.Reason);
    }

    [Fact]
    public void Acting_for_covers_only_in_force_delegations_and_is_not_transitive()
    {
        DelegationWindow[] all =
        [
            new(1, Maria, Pedro, Today.AddDays(-1), Today.AddDays(1), false),   // in force
            new(2, Ramon, Pedro, Today.AddDays(1), Today.AddDays(3), false),    // scheduled
            new(3, Lucia, Pedro, Today.AddDays(-1), Today.AddDays(1), true),    // cancelled
            new(4, Pedro, Ana, Today, Today, false),                             // Pedro's own delegate
        ];

        var forPedro = DelegationRules.ActingFor(all, Pedro, Today);
        Assert.Equal(new Dictionary<long, long> { [Maria] = 1 }, forPedro);

        var forAna = DelegationRules.ActingFor(all, Ana, Today);
        Assert.Equal([Pedro], forAna.Keys);
    }

    [Fact]
    public void Signatory_or_creator_may_cancel_until_it_ends()
    {
        var d = new DelegationWindow(1, Maria, Pedro, Today, Today.AddDays(3), false);
        Assert.Null(DelegationRules.CancelError(d, Maria, Maria, Today));
        Assert.Null(DelegationRules.CancelError(d, Ramon, Ramon, Today));
        Assert.NotNull(DelegationRules.CancelError(d, Pedro, Maria, Today));
        Assert.NotNull(DelegationRules.CancelError(d with { IsCancelled = true }, Maria, Maria, Today));
        Assert.NotNull(DelegationRules.CancelError(d, Maria, Maria, Today.AddDays(4)));
    }

    [Fact]
    public void State_follows_the_dates()
    {
        var d = new DelegationWindow(1, Maria, Pedro, Today.AddDays(1), Today.AddDays(3), false);
        Assert.Equal(DelegationState.Scheduled, DelegationRules.StateOn(d, Today));
        Assert.Equal(DelegationState.Active, DelegationRules.StateOn(d, Today.AddDays(2)));
        Assert.Equal(DelegationState.Ended, DelegationRules.StateOn(d, Today.AddDays(4)));
        Assert.Equal(DelegationState.Cancelled, DelegationRules.StateOn(d with { IsCancelled = true }, Today.AddDays(2)));
    }

    // ---------- Approval workflow with a delegate ----------

    private static List<WorkflowStep> TwoSteps(ApprovalStatus first = ApprovalStatus.Pending) =>
    [
        new(11, 1, 1, Maria, first),
        new(12, 2, 2, Ramon, ApprovalStatus.Pending),
    ];

    private static WorkflowDecision Act(List<WorkflowStep> steps, long actor, ApprovalAction action, IReadOnlySet<long>? actingFor, string? reason = null) =>
        ApprovalWorkflow.Decide(ReportStatus.ForApproval, steps, Ana, actor, action, reason, actingFor);

    [Fact]
    public void Delegate_approves_the_signatorys_step()
    {
        var d = Act(TwoSteps(), Pedro, ApprovalAction.Approve, new HashSet<long> { Maria });
        Assert.True(d.IsAllowed);
        Assert.Equal(11, d.ActingStepId);
    }

    [Fact]
    public void Without_a_delegation_the_same_person_is_refused()
    {
        Assert.Equal(WorkflowRefusal.Forbidden, Act(TwoSteps(), Pedro, ApprovalAction.Approve, null).Refusal);
    }

    [Fact]
    public void Delegate_follows_the_step_order()
    {
        var d = Act(TwoSteps(), Lucia, ApprovalAction.Approve, new HashSet<long> { Ramon });
        Assert.Equal(WorkflowRefusal.Conflict, d.Refusal);

        var overridden = Act(TwoSteps(), Lucia, ApprovalAction.Override, new HashSet<long> { Ramon }, "Maria away");
        Assert.True(overridden.IsAllowed);
        Assert.True(overridden.CompletesApproval);
    }

    [Fact]
    public void An_approver_on_the_report_never_fills_another_step_through_a_delegation()
    {
        // Ramon is the Level 2 on this report and also Maria's delegate: he may not approve her step.
        var d = Act(TwoSteps(), Ramon, ApprovalAction.Approve, new HashSet<long> { Maria });
        Assert.Equal(WorkflowRefusal.Conflict, d.Refusal);
    }

    [Fact]
    public void Delegate_cannot_act_on_their_own_report()
    {
        var d = ApprovalWorkflow.Decide(ReportStatus.ForApproval, TwoSteps(), Pedro, Pedro, ApprovalAction.Approve, null, new HashSet<long> { Maria });
        Assert.Equal(WorkflowRefusal.Forbidden, d.Refusal);
    }

    [Fact]
    public void Delegate_of_the_level1_may_adjust_before_the_first_approval()
    {
        Assert.Null(ApprovalWorkflow.AdjustmentError(ReportStatus.ForApproval, TwoSteps(), Ana, Pedro, new HashSet<long> { Maria }));
        Assert.NotNull(ApprovalWorkflow.AdjustmentError(ReportStatus.ForApproval, TwoSteps(), Ana, Pedro, null));
        Assert.NotNull(ApprovalWorkflow.AdjustmentError(ReportStatus.ForApproval, TwoSteps(), Ana, Ramon, new HashSet<long> { Maria }));
    }

    [Fact]
    public void Permissions_reflect_the_delegation()
    {
        var p = ApprovalWorkflow.Permissions(ReportStatus.ForApproval, TwoSteps(), Ana, Pedro, new HashSet<long> { Maria });
        Assert.True(p.CanApprove);
        Assert.True(p.CanReturn);
        Assert.False(p.CanOverride);
        Assert.True(p.CanAdjustLines);
    }
}
