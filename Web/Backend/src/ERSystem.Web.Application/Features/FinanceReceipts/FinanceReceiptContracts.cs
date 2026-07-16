using ERSystem.Web.Application.Common;

namespace ERSystem.Web.Application.Features.FinanceReceipts;

public sealed record FinanceReceiptQuery : PagedRequest
{
    public string? Search { get; init; }
    public string? FinanceStatus { get; init; }
    public bool? PhysicalReceiptsReceived { get; init; }
    public string? ReportType { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}

public sealed record FinanceReceiptListItemDto(
    string ReportId, int EmployeeUserId, string EmployeeName, DateOnly? DateFrom,
    DateOnly? DateTo, string Description, string ReportType, string ErfReferenceNumber,
    string CashReferenceNumber, string FinanceStatus, bool PhysicalReceiptsReceived,
    DateTime? ReceivedDateUtc, string RowVersion);

public sealed record FinanceReceiptDetailDto(
    string ReportId, int EmployeeUserId, string EmployeeName, DateOnly? DateFrom,
    DateOnly? DateTo, string Description, string ReportType, string ErfReferenceNumber,
    string FinanceStatus, bool PhysicalReceiptsReceived,
    int? ReceivedByUserId, string ReceivedByName,
    DateTime? ReceivedDateUtc, string Remarks, string RowVersion, string Department);

public sealed record ReceivePhysicalReceiptsRequest(string? Remarks, string RowVersion);
public sealed record ReceivePhysicalReceiptsResult(string ReportId, string FinanceStatus, string RowVersion);

public interface IFinanceReceiptService
{
    Task<PagedResult<FinanceReceiptListItemDto>> GetReportsAsync(FinanceReceiptQuery query, CancellationToken cancellationToken);
    Task<FinanceReceiptDetailDto> GetReportAsync(string reportId, CancellationToken cancellationToken);
    Task<ReceivePhysicalReceiptsResult> ReceiveAsync(int financeUserId, string reportId, ReceivePhysicalReceiptsRequest request, Guid correlationId, CancellationToken cancellationToken);
}
