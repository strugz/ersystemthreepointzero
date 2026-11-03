using ERFWeb.Domain.Administration;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class SignatureApprovalRulesTests
{
    private static readonly DateTimeOffset Uploaded = new(2026, 11, 3, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Status_FollowsImageAndApproval()
    {
        Assert.Equal(SignatureStatus.None, SignatureApprovalRules.Status(false, null));
        Assert.Equal(SignatureStatus.Pending, SignatureApprovalRules.Status(true, null));
        Assert.Equal(SignatureStatus.Approved, SignatureApprovalRules.Status(true, Uploaded));
    }

    [Fact]
    public void IsSelfReview_WhenReviewerOwnsTheSignature()
    {
        Assert.True(SignatureApprovalRules.IsSelfReview(7, 7));
        Assert.False(SignatureApprovalRules.IsSelfReview(7, 8));
    }

    [Fact]
    public void ReviewBlocker_NoneForThePendingImageReviewed() =>
        Assert.Null(SignatureApprovalRules.ReviewBlocker(true, Uploaded, null, Uploaded));

    [Fact]
    public void ReviewBlocker_RefusesMissingReplacedOrApprovedImage()
    {
        Assert.Equal("No signature is on file.", SignatureApprovalRules.ReviewBlocker(false, null, null, Uploaded));
        Assert.Equal("The signature was replaced. Review the new one.",
            SignatureApprovalRules.ReviewBlocker(true, Uploaded.AddMinutes(5), null, Uploaded));
        Assert.Equal("This signature is already approved.", SignatureApprovalRules.ReviewBlocker(true, Uploaded, Uploaded, Uploaded));
    }
}
