using ERFWeb.Application.Features.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// The expense summary on screen and as CSV or PDF. Any signed-in user may ask; the service limits
/// the scope (Mine for everyone, Approvals for signatories, All for Finance) and returns 403 otherwise.
/// </summary>
[ApiController]
[Route("api/summaries")]
[Authorize]
public sealed class SummariesController(IExpenseSummaryService summaries, IExportService exports) : ControllerBase
{
    [HttpGet("expenses")]
    [Produces("application/json")]
    [ProducesResponseType<ExpenseSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ExpenseSummaryDto>> Expenses(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] SummaryScope scope = SummaryScope.Mine,
        [FromQuery] long? departmentId = null,
        [FromQuery] bool approvedOnly = false,
        CancellationToken ct = default) =>
        Ok(ExpenseSummaryDto.Create(await summaries.BuildAsync(new ExpenseSummaryQuery(from, to, scope, departmentId, approvedOnly), ct)));

    [HttpGet("expenses/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportExpenses(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] SummaryScope scope = SummaryScope.Mine,
        [FromQuery] long? departmentId = null,
        [FromQuery] bool approvedOnly = false,
        [FromQuery] ExportFormat format = ExportFormat.Csv,
        CancellationToken ct = default)
    {
        var file = await exports.ExpenseSummaryAsync(new ExpenseSummaryQuery(from, to, scope, departmentId, approvedOnly), format, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
