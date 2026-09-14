using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>Employee authoring &amp; filing of expense reports (ERF header, cash advance, workflow actions).</summary>
[ApiController]
[Route("api/reports")]
[Authorize]
[Produces("application/json")]
public sealed class ExpenseReportsController(
    IExpenseReportService reports,
    IReportFilingService filing,
    IReportAccessPolicy access) : ControllerBase
{
    /// <summary>Lists the signed-in employee's own reports (paged, filterable by status/search).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ExpenseReportListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ExpenseReportListItemDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        [FromQuery] string? sort = null,
        [FromQuery] SortDirection direction = SortDirection.Desc,
        [FromQuery] ReportStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new ExpenseReportListQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort,
            Direction = direction,
            Status = status,
            Search = search,
        };
        return Ok(await reports.ListMineAsync(query, ct));
    }

    /// <summary>Creates a new report (status New) with its cash-advance block.</summary>
    [HttpPost]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseReportDetailDto>> Create([FromBody] CreateExpenseReportRequest request, CancellationToken ct)
    {
        var created = await reports.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { reportId = created.Id }, created);
    }

    /// <summary>Full editor model for one report. Owner, assigned approvers, Finance and Admin may view.</summary>
    [HttpGet("{reportId:guid}")]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseReportDetailDto>> Get(Guid reportId, CancellationToken ct)
    {
        if (!await access.CanViewAsync(reportId, ct))
        {
            throw new ForbiddenException("You are not allowed to view this report.");
        }

        return Ok(await reports.GetAsync(reportId, ct));
    }

    /// <summary>Updates the header and cash advance. Requires the current <c>rowVersion</c>; stale → 409.</summary>
    [HttpPut("{reportId:guid}")]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseReportDetailDto>> Update(Guid reportId, [FromBody] UpdateExpenseReportRequest request, CancellationToken ct) =>
        Ok(await reports.UpdateAsync(reportId, request, ct));

    /// <summary>Deletes a never-filed report.</summary>
    [HttpDelete("{reportId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid reportId, CancellationToken ct)
    {
        await reports.DeleteAsync(reportId, ct);
        return NoContent();
    }

    /// <summary>Files (or re-files) the report: snapshots the approval route and moves it to For Approval.</summary>
    [HttpPost("{reportId:guid}/file")]
    [ProducesResponseType<FileReportResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FileReportResultDto>> File(Guid reportId, CancellationToken ct) =>
        Ok(await filing.FileAsync(reportId, ct));

    /// <summary>Pulls a For Approval / Approved report back to New for correction (approval restarts on re-file).</summary>
    [HttpPost("{reportId:guid}/reopen")]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseReportDetailDto>> Reopen(Guid reportId, CancellationToken ct) =>
        Ok(await filing.ReopenAsync(reportId, ct));
}
