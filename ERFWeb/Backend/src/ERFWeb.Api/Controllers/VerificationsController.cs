using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Application.Features.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Verification requests and queries (plan Phase 6). Finance raises and queries, the Expense Verifier
/// pool picks up and records findings, and the queried Level 2 decides. Each action is gated by the
/// role policy here and by the report-level rules in the service (9.16, 9.17, 9.18); stale or
/// out-of-order actions return 409. Every action returns the report as the actor now sees it.
/// </summary>
[ApiController]
[Route("api/verifications")]
[Produces("application/json")]
public sealed class VerificationsController(IVerificationService verifications) : ControllerBase
{
    /// <summary>Finance flags one line of an approved report for fact-checking; clearance pauses.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.Finance)]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseReportDetailDto>> Raise([FromBody] RaiseVerificationRequest request, CancellationToken ct) =>
        Ok(await verifications.RaiseAsync(request, ct));

    /// <summary>The verifier queue: the open pool, my requests in progress, or my findings.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ExpenseVerifier)]
    [ProducesResponseType<PagedResult<VerificationQueueItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<VerificationQueueItemDto>>> Queue(
        [FromQuery] VerificationQueueView view = VerificationQueueView.Open,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        CancellationToken ct = default) =>
        Ok(await verifications.QueueAsync(new VerificationQueueQuery { View = view, Page = page, PageSize = pageSize }, ct));

    [HttpPost("{id:long}/pick-up")]
    [Authorize(Policy = Policies.ExpenseVerifier)]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseReportDetailDto>> PickUp(long id, [FromBody] PickUpVerificationRequest request, CancellationToken ct) =>
        Ok(await verifications.PickUpAsync(id, request, ct));

    /// <summary>Verified, Verified with Note, or Not Verified. Recorded once (9.17).</summary>
    [HttpPost("{id:long}/finding")]
    [Authorize(Policy = Policies.ExpenseVerifier)]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseReportDetailDto>> Finding(long id, [FromBody] RecordFindingRequest request, CancellationToken ct) =>
        Ok(await verifications.RecordFindingAsync(id, request, ct));

    /// <summary>Finance refers a Not Verified finding to the Level 2 Signatory.</summary>
    [HttpPost("{id:long}/query")]
    [Authorize(Policy = Policies.Finance)]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseReportDetailDto>> Query(long id, [FromBody] RaiseQueryRequest request, CancellationToken ct) =>
        Ok(await verifications.QueryAsync(id, request, ct));

    /// <summary>Queries addressed to me (Level 2).</summary>
    [HttpGet("queries")]
    [Authorize(Policy = Policies.Approver)]
    [ProducesResponseType<PagedResult<VerificationQueueItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<VerificationQueueItemDto>>> Queries(
        [FromQuery] QueryListView view = QueryListView.Pending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedRequest.DefaultPageSize,
        CancellationToken ct = default) =>
        Ok(await verifications.QueriesAsync(new QueryListQuery { View = view, Page = page, PageSize = pageSize }, ct));

    /// <summary>Level 2 resumes, returns (reason), or rejects (reason) after a Not Verified finding.</summary>
    [HttpPost("{id:long}/decision")]
    [Authorize(Policy = Policies.Approver)]
    [ProducesResponseType<ExpenseReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseReportDetailDto>> Decide(long id, [FromBody] DecideQueryRequest request, CancellationToken ct) =>
        Ok(await verifications.DecideAsync(id, request, ct));
}
