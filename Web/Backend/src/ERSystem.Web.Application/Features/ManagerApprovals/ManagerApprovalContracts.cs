using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.ReportReview;

namespace ERSystem.Web.Application.Features.ManagerApprovals;

public sealed record ManagerReportQuery : PagedRequest
{
    public string Status { get; init; } = "pending";
    public string? Search { get; init; }
    public int? DepartmentId { get; init; }
    public string? ReportType { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}

public sealed record ManagerReportListItemDto(
    string ReportId, string ErfReferenceNumber, int EmployeeUserId, string EmployeeName, string Department,
    DateOnly? DateFrom, DateOnly? DateTo, string Description, string ReportType,
    int CurrentStep, int TotalSteps, string Status, string RowVersion);

public sealed record ManagerReportDetailDto(
    string ReportId, int EmployeeUserId, string EmployeeName, string Department,
    DateOnly? DateFrom, DateOnly? DateTo, string Description, string ReportType,
    string ErfReferenceNumber, IReadOnlyList<ExpenseLineDto> Expenses,
    CashAdvanceDto? CashAdvance, IReadOnlyList<ReceiptAttachmentDto> Attachments,
    IReadOnlyList<ApprovalTrailItemDto> ApprovalTrail, int CurrentStep, int TotalSteps,
    string Status, string RowVersion);

public sealed record ApproveReportRequest(string RowVersion);
public sealed record ReturnReportRequest(string Reason, string RowVersion);
public sealed record WorkflowActionResult(string ReportId, string Status, string RowVersion);

public interface IManagerApprovalService
{
    Task<PagedResult<ManagerReportListItemDto>> GetReportsAsync(int managerUserId, ManagerReportQuery query, CancellationToken cancellationToken);
    Task<ManagerReportDetailDto> GetReportAsync(int managerUserId, string reportId, CancellationToken cancellationToken);
    Task<AttachmentContentDto> GetAttachmentAsync(int managerUserId, int attachmentId, CancellationToken cancellationToken);
    Task<WorkflowActionResult> ApproveAsync(int managerUserId, string reportId, ApproveReportRequest request, Guid correlationId, CancellationToken cancellationToken);
    Task<WorkflowActionResult> ReturnAsync(int managerUserId, string reportId, ReturnReportRequest request, Guid correlationId, CancellationToken cancellationToken);
}
