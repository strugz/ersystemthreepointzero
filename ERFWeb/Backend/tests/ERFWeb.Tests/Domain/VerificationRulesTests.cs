using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Verification;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class VerificationRulesTests
{
    private const long Ana = 100;      // claimant
    private const long Fe = 5;         // Finance, requester
    private const long Vic = 7;        // verifier
    private const long Maria = 1;      // Level 1 who approved
    private const long Ramon = 2;      // Level 2
    private static readonly HashSet<long> Approvers = [Maria, Ramon];

    private static VerificationState State(
        VerificationStatus status,
        long? verifier = null,
        VerificationFinding? finding = null,
        long? queryTo = null) =>
        new(status, Ana, Fe, verifier, finding, queryTo);

    private static VerificationCheck Raise(
        ReportStatus status = ReportStatus.Approved,
        FinanceStatus? finance = FinanceStatus.Pending,
        long actor = Fe,
        bool busy = false,
        string? reason = "Hotel booked for 3 nights, trip was 2 days") =>
        VerificationRules.CanRaise(status, finance, Ana, actor, busy, reason);

    [Fact]
    public void Finance_raises_on_an_approved_report_in_the_queue() =>
        Assert.True(Raise().IsAllowed);

    [Theory]
    [InlineData(ReportStatus.ForApproval)]
    [InlineData(ReportStatus.New)]
    [InlineData(ReportStatus.Returned)]
    [InlineData(ReportStatus.Rejected)]
    public void Verification_is_post_approval_only(ReportStatus status) =>
        Assert.Equal(WorkflowRefusal.Conflict, Raise(status: status).Refusal);

    [Fact]
    public void Not_after_clearance_or_twice_on_a_line()
    {
        Assert.Equal(WorkflowRefusal.Conflict, Raise(finance: FinanceStatus.ClearedForPayment).Refusal);
        Assert.Equal(WorkflowRefusal.Conflict, Raise(finance: null).Refusal);
        Assert.Equal(WorkflowRefusal.Conflict, Raise(busy: true).Refusal);
    }

    [Fact]
    public void Raising_needs_a_reason_and_not_your_own_report()
    {
        Assert.Equal(WorkflowRefusal.Invalid, Raise(reason: "  ").Refusal);
        Assert.Equal(WorkflowRefusal.Invalid, Raise(reason: new string('x', VerificationRules.MaxTextLength + 1)).Refusal);
        Assert.Equal(WorkflowRefusal.Forbidden, Raise(actor: Ana).Refusal);
    }

    [Fact]
    public void A_free_verifier_picks_up_an_open_request() =>
        Assert.True(VerificationRules.CanPickUp(State(VerificationStatus.Open), Approvers, Vic).IsAllowed);

    [Theory]
    [InlineData(Ana)]
    [InlineData(Maria)]
    [InlineData(Ramon)]
    [InlineData(Fe)]
    public void Nobody_verifies_their_own_report_or_one_they_approved(long actor) =>
        Assert.Equal(WorkflowRefusal.Forbidden, VerificationRules.CanPickUp(State(VerificationStatus.Open), Approvers, actor).Refusal);

    [Fact]
    public void A_picked_up_request_cannot_be_taken_again() =>
        Assert.Equal(WorkflowRefusal.Conflict, VerificationRules.CanPickUp(State(VerificationStatus.InProgress, Vic), Approvers, 8).Refusal);

    [Fact]
    public void Only_the_assigned_verifier_records_the_finding_once()
    {
        var inProgress = State(VerificationStatus.InProgress, Vic);
        Assert.True(VerificationRules.CanRecordFinding(inProgress, Vic, VerificationFinding.Verified, null).IsAllowed);
        Assert.Equal(WorkflowRefusal.Forbidden, VerificationRules.CanRecordFinding(inProgress, 8, VerificationFinding.Verified, null).Refusal);

        var found = State(VerificationStatus.Found, Vic, VerificationFinding.NotVerified);
        Assert.Equal(WorkflowRefusal.Conflict, VerificationRules.CanRecordFinding(found, Vic, VerificationFinding.Verified, null).Refusal);
    }

    [Theory]
    [InlineData(VerificationFinding.Verified, null, true)]
    [InlineData(VerificationFinding.VerifiedWithNote, null, false)]
    [InlineData(VerificationFinding.VerifiedWithNote, "Supplier confirmed by phone", true)]
    [InlineData(VerificationFinding.NotVerified, " ", false)]
    [InlineData(VerificationFinding.NotVerified, "No such booking", true)]
    public void A_note_is_needed_unless_simply_verified(VerificationFinding finding, string? note, bool ok) =>
        Assert.Equal(ok, VerificationRules.CanRecordFinding(State(VerificationStatus.InProgress, Vic), Vic, finding, note).IsAllowed);

    [Theory]
    [InlineData(VerificationFinding.Verified, VerificationStatus.Closed)]
    [InlineData(VerificationFinding.VerifiedWithNote, VerificationStatus.Closed)]
    [InlineData(VerificationFinding.NotVerified, VerificationStatus.Found)]
    public void Only_not_verified_goes_back_to_finance(VerificationFinding finding, VerificationStatus next) =>
        Assert.Equal(next, VerificationRules.StatusAfterFinding(finding));

    [Fact]
    public void Finance_queries_only_a_not_verified_finding()
    {
        var notVerified = State(VerificationStatus.Found, Vic, VerificationFinding.NotVerified);
        Assert.True(VerificationRules.CanQuery(notVerified, Fe, "Please decide").IsAllowed);
        Assert.Equal(WorkflowRefusal.Invalid, VerificationRules.CanQuery(notVerified, Fe, null).Refusal);
        Assert.Equal(WorkflowRefusal.Forbidden, VerificationRules.CanQuery(notVerified, Ana, "x").Refusal);

        var closed = State(VerificationStatus.Closed, Vic, VerificationFinding.Verified);
        Assert.Equal(WorkflowRefusal.Conflict, VerificationRules.CanQuery(closed, Fe, "x").Refusal);
    }

    [Theory]
    [InlineData(QueryDisposition.Resume, null, true)]
    [InlineData(QueryDisposition.Return, null, false)]
    [InlineData(QueryDisposition.Return, "Remove the extra night", true)]
    [InlineData(QueryDisposition.Reject, "", false)]
    [InlineData(QueryDisposition.Reject, "Personal trip", true)]
    public void Level2_decides_with_a_reason_to_return_or_reject(QueryDisposition disposition, string? note, bool ok)
    {
        var queried = State(VerificationStatus.Queried, Vic, VerificationFinding.NotVerified, Ramon);
        Assert.Equal(ok, VerificationRules.CanDecide(queried, Ramon, disposition, note).IsAllowed);
    }

    [Fact]
    public void Only_the_queried_level2_decides_and_only_once()
    {
        var queried = State(VerificationStatus.Queried, Vic, VerificationFinding.NotVerified, Ramon);
        Assert.Equal(WorkflowRefusal.Forbidden, VerificationRules.CanDecide(queried, Maria, QueryDisposition.Resume, null).Refusal);
        Assert.Equal(WorkflowRefusal.Forbidden, VerificationRules.CanDecide(queried, Fe, QueryDisposition.Reject, "x").Refusal);

        var decided = State(VerificationStatus.Closed, Vic, VerificationFinding.NotVerified, Ramon);
        Assert.Equal(WorkflowRefusal.Conflict, VerificationRules.CanDecide(decided, Ramon, QueryDisposition.Resume, null).Refusal);
    }

    [Theory]
    [InlineData(VerificationStatus.Open, true)]
    [InlineData(VerificationStatus.InProgress, true)]
    [InlineData(VerificationStatus.Found, true)]
    [InlineData(VerificationStatus.Queried, true)]
    [InlineData(VerificationStatus.Closed, false)]
    public void Anything_but_closed_holds_up_clearance(VerificationStatus status, bool unresolved) =>
        Assert.Equal(unresolved, VerificationRules.IsUnresolved(status));
}
