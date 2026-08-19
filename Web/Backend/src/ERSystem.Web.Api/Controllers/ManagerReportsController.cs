using ERSystem.Web.Api.Configuration;
using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.ManagerApprovals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERSystem.Web.Api.Controllers;

[ApiController]
[Authorize(Policy = "Manager")]
[Route("api/manager")]
public sealed class ManagerReportsController(IManagerApprovalService service, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("reports")]
    public Task<PagedResult<ManagerReportListItemDto>> GetReports([FromQuery] ManagerReportQuery query, CancellationToken cancellationToken) =>
        service.GetReportsAsync(currentUser.UserId, query, cancellationToken);

    [HttpGet("reports/{reportId}")]
    public Task<ManagerReportDetailDto> GetReport(string reportId, CancellationToken cancellationToken) =>
        service.GetReportAsync(currentUser.UserId, reportId, cancellationToken);

    [HttpGet("attachments/{attachmentId:int}")]
    public async Task<IActionResult> GetAttachment(int attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await service.GetAttachmentAsync(currentUser.UserId, attachmentId, cancellationToken);
        return File(attachment.Content, attachment.ContentType, attachment.FileName, enableRangeProcessing: true);
    }

    [HttpPost("reports/{reportId}/approve")]
    public async Task<ActionResult<WorkflowActionResult>> Approve(string reportId, ApproveReportRequest request, CancellationToken cancellationToken) =>
        Ok(await service.ApproveAsync(currentUser.UserId, reportId, request, CorrelationIdMiddleware.Get(HttpContext), cancellationToken));

    [HttpPost("reports/{reportId}/return")]
    public async Task<ActionResult<WorkflowActionResult>> Return(string reportId, ReturnReportRequest request, CancellationToken cancellationToken) =>
        Ok(await service.ReturnAsync(currentUser.UserId, reportId, request, CorrelationIdMiddleware.Get(HttpContext), cancellationToken));
}
