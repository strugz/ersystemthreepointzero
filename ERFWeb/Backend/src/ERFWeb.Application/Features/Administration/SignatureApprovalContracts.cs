using ERFWeb.Domain.Administration;

namespace ERFWeb.Application.Features.Administration;

/// <summary>One person's signature image in Finance's review list.</summary>
public sealed record SignatureReviewItemDto(
    long UserId,
    string FullName,
    string? DepartmentName,
    SignatureStatus Status,
    DateTimeOffset? UploadedAt,
    DateTimeOffset? ApprovedAt,
    string? ApprovedByName);

/// <summary>
/// Approve or reject the image Finance looked at. <c>UploadedAt</c> is that image's upload time; a newer
/// upload since then answers 409 so a replaced image is never approved unseen. A rejection removes the image.
/// </summary>
public sealed record SignatureReviewRequest(DateTimeOffset UploadedAt, string? Note = null);

/// <summary>
/// Finance's review of signature images before the R47 may print them (the e-signature consent rule).
/// Nobody reviews their own signature.
/// </summary>
public interface ISignatureApprovalService
{
    /// <summary>Signatures waiting for review (<c>Pending</c>) or already approved (<c>Approved</c>), by name.</summary>
    Task<IReadOnlyList<SignatureReviewItemDto>> ListAsync(SignatureStatus status, CancellationToken ct = default);

    /// <summary>The signature image on file, for the reviewer's preview; throws <c>NotFoundException</c> when there is none.</summary>
    Task<SignatureImageDto> GetImageAsync(long userId, CancellationToken ct = default);

    Task<SignatureReviewItemDto> ApproveAsync(long userId, SignatureReviewRequest request, CancellationToken ct = default);

    Task<SignatureReviewItemDto> RejectAsync(long userId, SignatureReviewRequest request, CancellationToken ct = default);
}
