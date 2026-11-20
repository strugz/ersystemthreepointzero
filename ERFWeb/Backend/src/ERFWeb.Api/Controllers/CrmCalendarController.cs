using ERFWeb.Api.Security;
using ERFWeb.Application.Features.Crm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// The claimant's CRM activities on each day of the report period, for a signatory reviewing the report. The server
/// reads the claimant's CRM link and calls the CRM; the browser never does. 503 (<c>crm_not_configured</c> /
/// <c>crm_unavailable</c>) when the CRM cannot answer.
/// </summary>
[ApiController]
[Route("api/reports/{reportId:guid}/crm-calendar")]
[Authorize(Policy = Policies.Approver)]
[Produces("application/json")]
public sealed class CrmCalendarController(ICrmCalendarService calendar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CrmCalendarDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CrmCalendarDto>> Get(Guid reportId, CancellationToken ct = default) =>
        Ok(await calendar.GetForReportAsync(reportId, ct));
}
