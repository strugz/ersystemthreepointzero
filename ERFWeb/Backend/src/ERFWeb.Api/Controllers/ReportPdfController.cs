using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportPdfController(
    IReportAccessPolicy access,
    IExpenseReportDocumentLoader loader,
    IExpenseReportPdfRenderer renderer,
    IClock clock) : ControllerBase
{
    /// <summary>
    /// Streams the R47 "EXPENSE REPORT" PDF for a report. Inline by default;
    /// pass <c>?download=true</c> for an attachment.
    /// </summary>
    [HttpGet("{reportId:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(Guid reportId, [FromQuery] bool download = false, CancellationToken ct = default)
    {
        if (!await access.CanViewAsync(reportId, ct))
        {
            throw new ForbiddenException("You are not allowed to view this report.");
        }

        var document = await loader.LoadAsync(reportId, ct);
        var pdf = renderer.Render(document, clock.UtcNow);
        var fileName = $"ExpenseReport-{reportId:N}.pdf";

        if (download)
        {
            return File(pdf, "application/pdf", fileName);
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
        return File(pdf, "application/pdf");
    }
}
