using ERFWeb.Application.Features.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// The report owner's CRM activities on one day (the expense line's date, <c>?date=YYYY-MM-DD</c>), for
/// pre-filling expense lines. The
/// server calls the CRM with the owner's own CRM link; the browser never does. 503
/// (<c>crm_not_configured</c> / <c>crm_unavailable</c>) when the CRM cannot answer.
/// </summary>
[ApiController]
[Route("api/reports/{reportId:guid}/crm-activities")]
[Authorize]
[Produces("application/json")]
public sealed class CrmActivitiesController(ICrmActivityPickService activities) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CrmActivityPickListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CrmActivityPickListDto>> List(Guid reportId, [FromQuery] DateOnly date, CancellationToken ct = default) =>
        Ok(await activities.ListForDateAsync(reportId, date, ct));
}
