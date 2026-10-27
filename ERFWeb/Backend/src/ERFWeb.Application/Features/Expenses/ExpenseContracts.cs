using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Verification;
using ERFWeb.Domain;

namespace ERFWeb.Application.Features.Expenses;

// ---------------------------------------------------------------------------------
// Read models
// ---------------------------------------------------------------------------------

/// <summary>One row of the employee's "My Reports" list.</summary>
public sealed record ExpenseReportListItemDto(
    Guid Id,
    string? ReferenceNo,
    ReportType ReportType,
    string? Purpose,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    ReportStatus Status,
    decimal TotalAmount,
    int LineCount,
    DateTimeOffset? FiledAt,
    DateTimeOffset UpdatedAt);

/// <summary>One row of the CASH ADVANCE RECEIVED block (Liquidation of Cash Advance only). Who the balance is due to is derived from the sign of total − expenses, never stored.</summary>
public sealed record CashAdvanceLineDto(
    long Id,
    DateOnly? AdvanceDate,
    string? ReferenceDocument,
    string? ReferenceNo,
    decimal Amount,
    uint RowVersion);

public sealed record ExpenseMealItemDto(string MealType, string? PaidFor, long? PaidEmployeeId);

/// <summary>
/// One fare leg. On save only <see cref="FareModeId"/> and <see cref="FareModeDetail"/> are read; the
/// code and name are filled in on the way out. A null mode is a leg saved before fare modes existed.
/// </summary>
public sealed record ExpenseTransportItemDto(
    string? FareFrom,
    string? FareTo,
    decimal FareAmount,
    long? FareModeId = null,
    string? FareModeDetail = null,
    string? FareModeCode = null,
    string? FareModeName = null);

public sealed record ExpenseLineDto(
    long Id,
    DateOnly TransactionDate,
    string? Particulars,
    string? WorkWith,
    string? InvoiceOrNo,
    ExpenseType ExpenseType,
    ExpenseCategory Category,
    decimal Amount,
    decimal VatAmount,
    decimal TotalAmount,
    string? Location,
    string? ServiceNumber,
    string? Instrument,
    string? SerialNumber,
    bool IsPerdiem,
    int Multiplier,
    int? MinusDays,
    int? TotalDays,
    string? Computation,
    string? Remarks,
    int SortOrder,
    uint RowVersion,
    IReadOnlyList<ExpenseMealItemDto> Meals,
    IReadOnlyList<ExpenseTransportItemDto> Transports,
    LineAdjustmentDto? Adjustment = null,
    IReadOnlyList<string>? CrmActivityIds = null);

/// <summary>An approver lowered this line before the first approval; the original amount is kept.</summary>
public sealed record LineAdjustmentDto(decimal OriginalAmount, decimal OriginalTotal, string? AdjustedByName, DateTimeOffset AdjustedAt, string? Reason);

public sealed record ApprovalStepDto(
    int StepOrder,
    long ApproverUserId,
    string ApproverName,
    ApprovalStatus Status,
    DateTimeOffset? ActedAt,
    string? Note,
    int? Level = null,
    string? ActedByName = null);

public sealed record AttachmentDto(
    long Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string UploadedBy,
    DateTimeOffset UploadedAt);

/// <summary>The full editor model for one report. <see cref="CanEdit"/> is computed server-side.</summary>
public sealed record ExpenseReportDetailDto(
    Guid Id,
    string? ReferenceNo,
    long EmployeeUserId,
    string EmployeeName,
    ReportType ReportType,
    string? Purpose,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    ReportStatus Status,
    decimal RevolvingFundAmount,
    int CurrentCycle,
    DateTimeOffset? FiledAt,
    DateTimeOffset? ApprovedAt,
    string? CancelNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    uint RowVersion,
    bool CanEdit,
    bool CanFile,
    bool CanReopen,
    bool CanDelete,
    decimal TotalAmount,
    decimal CashAdvanceTotal,
    IReadOnlyList<CashAdvanceLineDto> CashAdvances,
    IReadOnlyList<ExpenseLineDto> Lines,
    IReadOnlyList<ApprovalStepDto> ApprovalSteps,
    IReadOnlyList<AttachmentDto> Attachments,
    ReviewActionsDto? Review = null,
    FinanceSummaryDto? Finance = null,
    VerificationPanelDto? Verification = null);

/// <summary>What the viewer may do on the report as an approver. Computed by the same rules the actions enforce.</summary>
public sealed record ReviewActionsDto(bool CanApprove, bool CanReturn, bool CanReject, bool CanOverride, bool CanAdjustLines);

/// <summary>Finance receipt tracking for an approved report.</summary>
public sealed record FinanceSummaryDto(FinanceStatus Status, string? Remarks, DateTimeOffset? ReceivedAt, DateTimeOffset? ClearedAt, bool UnderVerification = false);

/// <summary>Downloaded attachment bytes.</summary>
public sealed record AttachmentContentDto(string FileName, string ContentType, byte[] Content);

public sealed record FileReportResultDto(Guid ReportId, ReportStatus Status, int Cycle, string? NextApproverName);

// ---------------------------------------------------------------------------------
// Requests
// ---------------------------------------------------------------------------------

/// <summary>List filter for the employee's own reports.</summary>
public sealed record ExpenseReportListQuery : PagedRequest
{
    public ReportStatus? Status { get; init; }
    public string? Search { get; init; }
}

/// <summary>Report header. The ERF reference number is issued by the system on first filing; cash advances are separate rows.</summary>
public sealed record CreateExpenseReportRequest(
    ReportType ReportType,
    string? Purpose,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    decimal RevolvingFundAmount);

public sealed record UpdateExpenseReportRequest(
    ReportType ReportType,
    string? Purpose,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    decimal RevolvingFundAmount,
    uint RowVersion);

/// <summary>Create or replace one cash advance row. <see cref="RowVersion"/> is required on update.</summary>
public sealed record SaveCashAdvanceLineRequest(
    DateOnly? AdvanceDate,
    string? ReferenceDocument,
    string? ReferenceNo,
    decimal Amount,
    uint? RowVersion);

/// <summary>
/// Create or replace one expense line. <see cref="RowVersion"/> is required on update. <see cref="CrmActivityIds"/>
/// links the line to the CRM activities it was pre-filled from (several instruments of one visit); empty or null
/// for a line typed by hand.
/// </summary>
public sealed record SaveExpenseLineRequest(
    DateOnly TransactionDate,
    string? Particulars,
    string? WorkWith,
    string? InvoiceOrNo,
    ExpenseType ExpenseType,
    ExpenseCategory Category,
    decimal Amount,
    decimal VatAmount,
    string? Location,
    string? ServiceNumber,
    string? Instrument,
    string? SerialNumber,
    bool IsPerdiem,
    int Multiplier,
    int? MinusDays,
    int? TotalDays,
    string? Remarks,
    IReadOnlyList<ExpenseMealItemDto>? Meals,
    IReadOnlyList<ExpenseTransportItemDto>? Transports,
    uint? RowVersion,
    IReadOnlyList<string>? CrmActivityIds = null);

/// <summary>Every active line of the report, in the order the R47 should print them.</summary>
public sealed record ReorderExpenseLinesRequest(IReadOnlyList<long> LineIds);

public sealed record UploadAttachmentRequest(string FileName, string ContentType, byte[] Content);

// ---------------------------------------------------------------------------------
// Use-case interfaces (implemented in Infrastructure with EF Core + LINQ)
// ---------------------------------------------------------------------------------

public interface IExpenseReportService
{
    Task<PagedResult<ExpenseReportListItemDto>> ListMineAsync(ExpenseReportListQuery query, CancellationToken ct = default);
    Task<ExpenseReportDetailDto> GetAsync(Guid reportId, CancellationToken ct = default);
    Task<ExpenseReportDetailDto> CreateAsync(CreateExpenseReportRequest request, CancellationToken ct = default);
    Task<ExpenseReportDetailDto> UpdateAsync(Guid reportId, UpdateExpenseReportRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid reportId, CancellationToken ct = default);
}

public interface IExpenseLineService
{
    Task<ExpenseLineDto> AddAsync(Guid reportId, SaveExpenseLineRequest request, CancellationToken ct = default);
    Task<ExpenseLineDto> UpdateAsync(Guid reportId, long lineId, SaveExpenseLineRequest request, CancellationToken ct = default);
    Task RemoveAsync(Guid reportId, long lineId, CancellationToken ct = default);
    Task ReorderAsync(Guid reportId, ReorderExpenseLinesRequest request, CancellationToken ct = default);
}

/// <summary>
/// One CRM activity of the employee's day, offered to pre-fill an expense line. Only a
/// <see cref="Claimable"/> (Completed) one can be picked; Open and Cancelled ones are listed so the day is
/// complete. <see cref="WorkWith"/> names the other people on the activity, using their ERFWeb names where
/// linked. <see cref="ClaimedInReferenceNo"/> is set when the activity is already on another of the
/// employee's reports (null reference: not filed yet), and <see cref="LinesInThisReport"/> counts the lines
/// of this report that already use it.
/// </summary>
public sealed record CrmActivityOptionDto(
    string ActivityId,
    string? ActivityType,
    string? Status,
    bool Claimable,
    DateOnly ActivityDate,
    string? Subject,
    string? ServiceNumber,
    string? CustomerName,
    string? InstrumentName,
    string? SerialNumber,
    string? Role,
    string? WorkWith,
    string? TravelOrigin,
    string? TravelDestination,
    bool IsOvernight,
    bool ClaimedElsewhere,
    string? ClaimedInReferenceNo,
    int LinesInThisReport);

/// <summary>The employee's CRM activities on <see cref="Date"/>; <see cref="Linked"/> is false when the account has no CRM link.</summary>
public sealed record CrmActivityPickListDto(
    bool Linked,
    DateOnly Date,
    IReadOnlyList<CrmActivityOptionDto> Items);

public interface ICrmActivityPickService
{
    /// <summary>The current employee's CRM activities on one day (the line's date), in every status, Completed first.</summary>
    Task<CrmActivityPickListDto> ListForDateAsync(Guid reportId, DateOnly date, CancellationToken ct = default);
}

public interface ICashAdvanceService
{
    Task<CashAdvanceLineDto> AddAsync(Guid reportId, SaveCashAdvanceLineRequest request, CancellationToken ct = default);
    Task<CashAdvanceLineDto> UpdateAsync(Guid reportId, long lineId, SaveCashAdvanceLineRequest request, CancellationToken ct = default);
    Task RemoveAsync(Guid reportId, long lineId, CancellationToken ct = default);
}

public interface IReceiptAttachmentService
{
    Task<IReadOnlyList<AttachmentDto>> ListAsync(Guid reportId, CancellationToken ct = default);
    Task<AttachmentDto> UploadAsync(Guid reportId, UploadAttachmentRequest request, CancellationToken ct = default);
    Task<AttachmentContentDto> DownloadAsync(Guid reportId, long attachmentId, CancellationToken ct = default);
    Task DeleteAsync(Guid reportId, long attachmentId, CancellationToken ct = default);
}

public interface IReportFilingService
{
    /// <summary>Files (or re-files) a New/Returned report: snapshots the approval route and notifies the first approver.</summary>
    Task<FileReportResultDto> FileAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>Pulls a ForApproval/Approved report back to New for correction, superseding the current cycle.</summary>
    Task<ExpenseReportDetailDto> ReopenAsync(Guid reportId, CancellationToken ct = default);
}
