using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;

namespace ERFWeb.Application.Features.Approvals;

// ---------- Approver inbox and actions ----------

/// <summary>Which reports the inbox shows.</summary>
public enum ApprovalInboxView
{
    /// <summary>Reports with a pending step of mine in the current cycle (my turn, or waiting for an earlier step).</summary>
    Pending,

    /// <summary>Reports I have already acted on.</summary>
    Acted,
}

public sealed record ApprovalInboxItemDto(
    Guid ReportId,
    string? ReferenceNo,
    long EmployeeUserId,
    string EmployeeName,
    string? DepartmentName,
    ReportType ReportType,
    string? Purpose,
    ReportStatus Status,
    decimal TotalAmount,
    DateTimeOffset? FiledAt,
    int Cycle,
    int MyStepOrder,
    int? MyLevel,
    ApprovalStatus MyStepStatus,
    DateTimeOffset? MyActedAt,
    int? CurrentStepOrder,
    string? CurrentApproverName,
    bool IsMyTurn,
    bool CanOverride,
    int DaysWaiting,
    string? OnBehalfOfName = null);

public sealed record ApprovalInboxQuery : PagedRequest
{
    public ApprovalInboxView View { get; init; } = ApprovalInboxView.Pending;
    public string? Search { get; init; }
}

/// <summary>Approve (note optional) or return, reject, override (reason required). RowVersion is the report's.</summary>
public sealed record ApprovalActionRequest(string? Note, uint RowVersion);

/// <summary>Lowers one line amount before the first approval. RowVersion is the line's.</summary>
public sealed record AdjustLineRequest(decimal Amount, string? Reason, uint RowVersion);

public sealed record ApprovalActionResultDto(Guid ReportId, ReportStatus Status, string? NextApproverName, ExpenseReportDetailDto Report);

public interface IApprovalService
{
    Task<PagedResult<ApprovalInboxItemDto>> InboxAsync(ApprovalInboxQuery query, CancellationToken ct = default);
    Task<ApprovalActionResultDto> ApproveAsync(Guid reportId, ApprovalActionRequest request, CancellationToken ct = default);
    Task<ApprovalActionResultDto> ReturnAsync(Guid reportId, ApprovalActionRequest request, CancellationToken ct = default);
    Task<ApprovalActionResultDto> RejectAsync(Guid reportId, ApprovalActionRequest request, CancellationToken ct = default);
    Task<ApprovalActionResultDto> OverrideAsync(Guid reportId, ApprovalActionRequest request, CancellationToken ct = default);
    Task<ExpenseReportDetailDto> AdjustLineAsync(Guid reportId, long lineId, AdjustLineRequest request, CancellationToken ct = default);
}

// ---------- Finance receipt queue ----------

public sealed record FinanceQueueItemDto(
    Guid ReportId,
    string? ReferenceNo,
    long EmployeeUserId,
    string EmployeeName,
    string? DepartmentName,
    ReportType ReportType,
    string? Purpose,
    decimal TotalAmount,
    DateTimeOffset? ApprovedAt,
    FinanceStatus Status,
    string? Remarks,
    string? ReceivedByName,
    DateTimeOffset? ReceivedAt,
    string? ClearedByName,
    DateTimeOffset? ClearedAt,
    int FollowUpCount,
    DateTimeOffset? LastFollowUpAt,
    int AttachmentCount,
    int DaysSinceApproval,
    bool IsOwnReport,
    uint RowVersion,
    int OpenVerifications = 0,
    bool NeedsQuery = false);

public sealed record FinanceQueueQuery : PagedRequest
{
    public FinanceStatus? Status { get; init; }
    public string? Search { get; init; }

    /// <summary>When true (the default), cleared reports are left out.</summary>
    public bool OpenOnly { get; init; } = true;
}

/// <summary>Not received (Pending), incomplete (remarks required), or received (one-time). RowVersion is the tracking row's.</summary>
public sealed record RecordReceiptsRequest(FinanceStatus Status, string? Remarks, uint RowVersion);

public sealed record ClearForPaymentRequest(string? Remarks, uint RowVersion);

public sealed record FinanceFollowUpRequest(string? Message);

public interface IFinanceService
{
    Task<PagedResult<FinanceQueueItemDto>> QueueAsync(FinanceQueueQuery query, CancellationToken ct = default);
    Task<FinanceQueueItemDto> RecordReceiptsAsync(Guid reportId, RecordReceiptsRequest request, CancellationToken ct = default);
    Task<FinanceQueueItemDto> ClearAsync(Guid reportId, ClearForPaymentRequest request, CancellationToken ct = default);
    Task<FinanceQueueItemDto> FollowUpAsync(Guid reportId, FinanceFollowUpRequest request, CancellationToken ct = default);
}

/// <summary>Audit event names for approval and Finance actions (written with the report id).</summary>
public static class ApprovalAuditEvents
{
    public const string Approved = "ReportStepApproved";
    public const string FullyApproved = "ReportApproved";
    public const string Returned = "ReportReturned";
    public const string Rejected = "ReportRejected";
    public const string Overridden = "ReportStepOverridden";
    public const string LineAdjusted = "ReportLineAdjusted";
    public const string ReceiptsRecorded = "FinanceReceiptsRecorded";
    public const string Cleared = "FinanceClearedForPayment";
    public const string FollowUp = "FinanceFollowUpSent";
}

/// <summary>
/// The signatories the current user stands in for today, each mapped to the delegation that allows
/// it. Used by the inbox, the actions, the review permissions, and report view access.
/// </summary>
public interface IDelegationLookup
{
    Task<IReadOnlyDictionary<long, long>> ActingForAsync(CancellationToken ct = default);
}
