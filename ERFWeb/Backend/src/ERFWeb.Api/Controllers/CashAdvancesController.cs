using ERFWeb.Application.Features.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>Rows of the "CASH ADVANCE RECEIVED" block. Only valid on a Liquidation of Cash Advance report that is still editable.</summary>
[ApiController]
[Route("api/reports/{reportId:guid}/cash-advances")]
[Authorize]
[Produces("application/json")]
public sealed class CashAdvancesController(ICashAdvanceService cashAdvances) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CashAdvanceLineDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashAdvanceLineDto>> Add(Guid reportId, [FromBody] SaveCashAdvanceLineRequest request, CancellationToken ct)
    {
        var line = await cashAdvances.AddAsync(reportId, request, ct);
        return Created($"/api/reports/{reportId}/cash-advances/{line.Id}", line);
    }

    /// <summary>Replaces a row. Requires <c>rowVersion</c>; stale → 409.</summary>
    [HttpPut("{lineId:long}")]
    [ProducesResponseType<CashAdvanceLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashAdvanceLineDto>> Update(Guid reportId, long lineId, [FromBody] SaveCashAdvanceLineRequest request, CancellationToken ct) =>
        Ok(await cashAdvances.UpdateAsync(reportId, lineId, request, ct));

    [HttpDelete("{lineId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(Guid reportId, long lineId, CancellationToken ct)
    {
        await cashAdvances.RemoveAsync(reportId, lineId, ct);
        return NoContent();
    }
}
