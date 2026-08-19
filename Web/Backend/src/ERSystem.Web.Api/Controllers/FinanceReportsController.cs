using ERSystem.Web.Api.Configuration;
using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.FinanceReceipts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERSystem.Web.Api.Controllers;

[ApiController]
[Authorize(Policy = "Finance")]
[Route("api/finance/reports")]
public sealed class FinanceReportsController(IFinanceReceiptService service, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<FinanceReceiptListItemDto>> GetReports([FromQuery] FinanceReceiptQuery query, CancellationToken cancellationToken) =>
        service.GetReportsAsync(query, cancellationToken);

    [HttpGet("{reportId}")]
    public Task<FinanceReceiptDetailDto> GetReport(string reportId, CancellationToken cancellationToken) =>
        service.GetReportAsync(reportId, cancellationToken);

    [HttpPost("{reportId}/receive")]
    public async Task<ActionResult<ReceivePhysicalReceiptsResult>> Receive(
        string reportId, ReceivePhysicalReceiptsRequest request, CancellationToken cancellationToken) =>
        Ok(await service.ReceiveAsync(currentUser.UserId, reportId, request, CorrelationIdMiddleware.Get(HttpContext), cancellationToken));
}
