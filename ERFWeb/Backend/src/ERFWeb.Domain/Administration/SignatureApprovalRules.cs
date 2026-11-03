namespace ERFWeb.Domain.Administration;

/// <summary>Where a person's signature image stands: none on file, waiting for Finance, or approved to print.</summary>
public enum SignatureStatus
{
    None,
    Pending,
    Approved,
}

/// <summary>
/// Finance approves a signature image before the R47 may print it (the e-signature consent rule).
/// A new upload starts over as Pending; nobody approves or rejects their own signature.
/// </summary>
public static class SignatureApprovalRules
{
    public static SignatureStatus Status(bool hasImage, DateTimeOffset? approvedAt) =>
        !hasImage ? SignatureStatus.None
        : approvedAt is null ? SignatureStatus.Pending
        : SignatureStatus.Approved;

    public const string SelfReviewMessage = "You cannot review your own signature. Another Finance user must review it.";

    /// <summary>Nobody approves or rejects their own signature, as nobody approves their own report (9.1).</summary>
    public static bool IsSelfReview(long reviewerUserId, long ownerUserId) => reviewerUserId == ownerUserId;

    /// <summary>
    /// Why the signature cannot be approved or rejected now, or null when it can. <paramref name="reviewedUploadedAt"/>
    /// is the upload the reviewer looked at; a different one means the image was replaced since.
    /// </summary>
    public static string? ReviewBlocker(bool hasImage, DateTimeOffset? uploadedAt, DateTimeOffset? approvedAt, DateTimeOffset reviewedUploadedAt)
    {
        if (!hasImage)
        {
            return "No signature is on file.";
        }

        if (uploadedAt != reviewedUploadedAt)
        {
            return "The signature was replaced. Review the new one.";
        }

        return approvedAt is not null ? "This signature is already approved." : null;
    }
}
