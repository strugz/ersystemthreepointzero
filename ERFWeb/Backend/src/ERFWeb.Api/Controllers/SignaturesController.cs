using ERFWeb.Api.Security;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Finance's review of signature images: the R47 prints a person's image only after Finance approves it.
/// Nobody reviews their own (403); a reviewed image that was replaced or already decided answers 409.
/// </summary>
[ApiController]
[Route("api/signatures")]
[Authorize(Policy = Policies.Finance)]
[Produces("application/json")]
public sealed class SignaturesController(ISignatureApprovalService signatures) : ControllerBase
{
    /// <summary>Signatures waiting for review (<c>Pending</c>, the default) or already approved (<c>Approved</c>).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SignatureReviewItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SignatureReviewItemDto>>> List(
        [FromQuery] SignatureStatus status = SignatureStatus.Pending, CancellationToken ct = default) =>
        Ok(await signatures.ListAsync(status, ct));

    /// <summary>The signature image on file, for the reviewer's preview.</summary>
    [HttpGet("{userId:long}/image")]
    [Produces("image/png", "image/jpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Image(long userId, CancellationToken ct)
    {
        var image = await signatures.GetImageAsync(userId, ct);
        Response.Headers.CacheControl = "no-store";
        return File(image.Content, image.ContentType);
    }

    [HttpPost("{userId:long}/approve")]
    [ProducesResponseType<SignatureReviewItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SignatureReviewItemDto>> Approve(long userId, [FromBody] SignatureReviewRequest request, CancellationToken ct) =>
        Ok(await signatures.ApproveAsync(userId, request, ct));

    /// <summary>Rejects and removes the image; the person loads a new one through the System Administrator.</summary>
    [HttpPost("{userId:long}/reject")]
    [ProducesResponseType<SignatureReviewItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SignatureReviewItemDto>> Reject(long userId, [FromBody] SignatureReviewRequest request, CancellationToken ct) =>
        Ok(await signatures.RejectAsync(userId, request, ct));
}
