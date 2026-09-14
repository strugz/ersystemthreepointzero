using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class ApprovalSequenceTests
{
    private static ApprovalStepState Step(int order, ApprovalStatus status) => new(order, status);

    [Fact]
    public void FirstStep_CanApprove_WhenPending()
    {
        var steps = new[] { Step(1, ApprovalStatus.Pending), Step(2, ApprovalStatus.Pending) };
        Assert.True(ApprovalSequence.CanApprove(steps, 1));
    }

    [Fact]
    public void SecondStep_CannotApprove_BeforeFirst()
    {
        var steps = new[] { Step(1, ApprovalStatus.Pending), Step(2, ApprovalStatus.Pending) };
        Assert.False(ApprovalSequence.CanApprove(steps, 2));
    }

    [Fact]
    public void SecondStep_CanApprove_AfterFirstApproved()
    {
        var steps = new[] { Step(1, ApprovalStatus.Approved), Step(2, ApprovalStatus.Pending) };
        Assert.True(ApprovalSequence.CanApprove(steps, 2));
    }

    [Fact]
    public void AlreadyApprovedStep_CannotApproveAgain()
    {
        var steps = new[] { Step(1, ApprovalStatus.Approved), Step(2, ApprovalStatus.Pending) };
        Assert.False(ApprovalSequence.CanApprove(steps, 1));
    }

    [Fact]
    public void CurrentStepOrder_IsLowestPending()
    {
        var steps = new[] { Step(1, ApprovalStatus.Approved), Step(2, ApprovalStatus.Pending), Step(3, ApprovalStatus.Pending) };
        Assert.Equal(2, ApprovalSequence.CurrentStepOrder(steps));
    }

    [Fact]
    public void IsFullyApproved_TrueWhenAllApproved()
    {
        var steps = new[] { Step(1, ApprovalStatus.Approved), Step(2, ApprovalStatus.Approved) };
        Assert.True(ApprovalSequence.IsFullyApproved(steps));
    }

    [Fact]
    public void IsFullyApproved_FalseWhenAnyPending()
    {
        var steps = new[] { Step(1, ApprovalStatus.Approved), Step(2, ApprovalStatus.Pending) };
        Assert.False(ApprovalSequence.IsFullyApproved(steps));
    }
}
