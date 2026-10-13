using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// The approver inbox and actions. Report-level checks (it must be your step, your turn, not your
/// own report) live in the service; stale or out-of-order actions return 409.
/// </summary>
[ApiController]
[Route("api/approvals")]
[Authorize(Policy = Policies.Approver)]
[Produces("application/json")]
public sealed class ApprovalsController(IApprovalService approvals) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ApprovalInboxItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ApprovalInboxItemDto>>> Inbox(
        [FromQuery] ApprovalInboxView view = ApprovalInboxView.Pending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await approvals.InboxAsync(new ApprovalInboxQuery { View = view, Page = page, PageSize = pageSize, Search = search }, ct));

    [HttpPost("{reportId:guid}/approve")]
    [ProducesResponseType<ApprovalActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApprovalActionResultDto>> Approve(Guid reportId, [FromBody] ApprovalActionRequest request, CancellationToken ct) =>
        Ok(await approvals.ApproveAsync(reportId, request, ct));

    [HttpPost("{reportId:guid}/return")]
    [ProducesResponseType<ApprovalActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApprovalActionResultDto>> Return(Guid reportId, [FromBody] ApprovalActionRequest request, CancellationToken ct) =>
        Ok(await approvals.ReturnAsync(reportId, request, ct));

    [HttpPost("{reportId:guid}/reject")]
    [ProducesResponseType<ApprovalActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApprovalActionResultDto>> Reject(Guid reportId, [FromBody] ApprovalActionRequest request, CancellationToken ct) =>
        Ok(await approvals.RejectAsync(reportId, request, ct));

    /// <summary>A Level 2 approves past a pending Level 1, with a reason.</summary>
    [HttpPost("{reportId:guid}/override")]
    [ProducesResponseType<ApprovalActionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApprovalActionResultDto>> Override(Guid reportId, [FromBody] ApprovalActionRequest request, CancellationToken ct) =>
        Ok(await approvals.OverrideAsync(reportId, request, ct));

    /// <summary>The current Level 1 lowers a line amount, with a reason, before the first approval.</summary>
    [HttpPut("{reportId:guid}/lines/{lineId:long}")]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseReportDetailDto>> AdjustLine(Guid reportId, long lineId, [FromBody] AdjustLineRequest request, CancellationToken ct) =>
        Ok(await approvals.AdjustLineAsync(reportId, lineId, request, ct));
}

/// <summary>The Finance physical-receipt queue.</summary>
[ApiController]
[Route("api/finance")]
[Authorize(Policy = Policies.Finance)]
[Produces("application/json")]
public sealed class FinanceController(IFinanceService finance) : ControllerBase
{
    [HttpGet("queue")]
    [ProducesResponseType<PagedResult<FinanceQueueItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FinanceQueueItemDto>>> Queue(
        [FromQuery] FinanceStatus? status = null,
        [FromQuery] bool openOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await finance.QueueAsync(new FinanceQueueQuery { Status = status, OpenOnly = openOnly, Page = page, PageSize = pageSize, Search = search }, ct));

    [HttpPost("{reportId:guid}/receipts")]
    [ProducesResponseType<FinanceQueueItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FinanceQueueItemDto>> Receipts(Guid reportId, [FromBody] RecordReceiptsRequest request, CancellationToken ct) =>
        Ok(await finance.RecordReceiptsAsync(reportId, request, ct));

    [HttpPost("{reportId:guid}/clear")]
    [ProducesResponseType<FinanceQueueItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FinanceQueueItemDto>> Clear(Guid reportId, [FromBody] ClearForPaymentRequest request, CancellationToken ct) =>
        Ok(await finance.ClearAsync(reportId, request, ct));

    [HttpPost("{reportId:guid}/follow-up")]
    [ProducesResponseType<FinanceQueueItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FinanceQueueItemDto>> FollowUp(Guid reportId, [FromBody] FinanceFollowUpRequest request, CancellationToken ct) =>
        Ok(await finance.FollowUpAsync(reportId, request, ct));
}
