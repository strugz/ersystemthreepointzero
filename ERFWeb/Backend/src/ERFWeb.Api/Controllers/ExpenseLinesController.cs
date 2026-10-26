using ERFWeb.Application.Features.Expenses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>Expense line items of a report. Editable only while the report is New/Returned.</summary>
[ApiController]
[Route("api/reports/{reportId:guid}/lines")]
[Authorize]
[Produces("application/json")]
public sealed class ExpenseLinesController(IExpenseLineService lines) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ExpenseLineDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseLineDto>> Add(Guid reportId, [FromBody] SaveExpenseLineRequest request, CancellationToken ct)
    {
        var line = await lines.AddAsync(reportId, request, ct);
        return Created($"/api/reports/{reportId}/lines/{line.Id}", line);
    }

    /// <summary>Replaces a line (including its meal/transport items). Requires <c>rowVersion</c>; stale → 409.</summary>
    [HttpPut("{lineId:long}")]
    [ProducesResponseType<ExpenseLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseLineDto>> Update(Guid reportId, long lineId, [FromBody] SaveExpenseLineRequest request, CancellationToken ct) =>
        Ok(await lines.UpdateAsync(reportId, lineId, request, ct));

    /// <summary>Sets the line order the R47 prints. Must list every active line once; a stale list → 409.</summary>
    [HttpPut("order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reorder(Guid reportId, [FromBody] ReorderExpenseLinesRequest request, CancellationToken ct)
    {
        await lines.ReorderAsync(reportId, request, ct);
        return NoContent();
    }

    [HttpDelete("{lineId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(Guid reportId, long lineId, CancellationToken ct)
    {
        await lines.RemoveAsync(reportId, lineId, ct);
        return NoContent();
    }
}
