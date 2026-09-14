using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>Scanned-receipt attachments of a report.</summary>
[ApiController]
[Route("api/reports/{reportId:guid}/attachments")]
[Authorize]
public sealed class AttachmentsController(IReceiptAttachmentService attachments) : ControllerBase
{
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<AttachmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> List(Guid reportId, CancellationToken ct) =>
        Ok(await attachments.ListAsync(reportId, ct));

    /// <summary>Uploads one file (multipart/form-data, field name <c>file</c>). Limits come from <c>Attachments</c> in appsettings.json.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType<AttachmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid reportId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException("file", "Choose a file to upload.");
        }

        byte[] content;
        await using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream, ct);
            content = stream.ToArray();
        }

        var saved = await attachments.UploadAsync(reportId, new UploadAttachmentRequest(file.FileName, file.ContentType, content), ct);
        return Created($"/api/reports/{reportId}/attachments/{saved.Id}/content", saved);
    }

    /// <summary>Streams the file inline (view) or as a download.</summary>
    [HttpGet("{attachmentId:long}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Content(Guid reportId, long attachmentId, [FromQuery] bool download = false, CancellationToken ct = default)
    {
        var file = await attachments.DownloadAsync(reportId, attachmentId, ct);
        if (download)
        {
            return File(file.Content, file.ContentType, file.FileName);
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{file.FileName}\"";
        return File(file.Content, file.ContentType);
    }

    [HttpDelete("{attachmentId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid reportId, long attachmentId, CancellationToken ct)
    {
        await attachments.DeleteAsync(reportId, attachmentId, ct);
        return NoContent();
    }
}
