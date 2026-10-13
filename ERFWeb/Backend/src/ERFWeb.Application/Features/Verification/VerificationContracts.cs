using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;

namespace ERFWeb.Application.Features.Verification;

// ---------- On the report ----------

/// <summary>
/// One verification request as shown on the report. The claimant sees that a line is under
/// verification and the outcome; Finance's reason, the verifier's notes, and the query are left
/// out for them (open decision 9c defaults to not showing findings to the claimant).
/// </summary>
public sealed record VerificationDto(
    long Id,
    long LineId,
    string? LineParticulars,
    decimal LineTotal,
    VerificationStatus Status,
    string? Reason,
    string RequestedByName,
    DateTimeOffset RequestedAt,
    string? VerifierName,
    DateTimeOffset? PickedUpAt,
    VerificationFinding? Finding,
    string? FindingNote,
    DateTimeOffset? FoundAt,
    string? QueryToName,
    string? QueryNote,
    DateTimeOffset? QueriedAt,
    QueryDisposition? Disposition,
    string? DispositionNote,
    DateTimeOffset? DecidedAt,
    uint RowVersion,
    bool CanPickUp,
    bool CanRecordFinding,
    bool CanQuery,
    bool CanDecide);

/// <summary>The verification section of the report detail.</summary>
public sealed record VerificationPanelDto(bool CanRequest, bool UnderVerification, IReadOnlyList<VerificationDto> Requests);

// ---------- Queues ----------

/// <summary>Which requests the verifier queue shows.</summary>
public enum VerificationQueueView
{
    /// <summary>Open requests anyone in the pool may pick up (excluding ones the viewer may not verify, 9.16).</summary>
    Open,

    /// <summary>Requests I picked up and have not finished.</summary>
    Mine,

    /// <summary>Requests I recorded a finding on.</summary>
    Done,
}

/// <summary>Which queries the Level 2 list shows.</summary>
public enum QueryListView
{
    Pending,
    Decided,
}

public sealed record VerificationQueueItemDto(
    long Id,
    Guid ReportId,
    string? ReferenceNo,
    string EmployeeName,
    string? DepartmentName,
    long LineId,
    DateOnly LineDate,
    string? LineParticulars,
    decimal LineTotal,
    string Reason,
    string RequestedByName,
    DateTimeOffset RequestedAt,
    VerificationStatus Status,
    string? VerifierName,
    VerificationFinding? Finding,
    string? FindingNote,
    DateTimeOffset? FoundAt,
    string? QueryNote,
    DateTimeOffset? QueriedAt,
    QueryDisposition? Disposition,
    DateTimeOffset? DecidedAt,
    int DaysWaiting);

public sealed record VerificationQueueQuery : PagedRequest
{
    public VerificationQueueView View { get; init; } = VerificationQueueView.Open;
}

public sealed record QueryListQuery : PagedRequest
{
    public QueryListView View { get; init; } = QueryListView.Pending;
}

// ---------- Requests ----------

/// <summary>Finance flags one line of an approved report.</summary>
public sealed record RaiseVerificationRequest(Guid ReportId, long LineId, string? Reason);

/// <summary>RowVersion is the verification request's.</summary>
public sealed record PickUpVerificationRequest(uint RowVersion);

public sealed record RecordFindingRequest(VerificationFinding Finding, string? Note, uint RowVersion);

public sealed record RaiseQueryRequest(string? Note, uint RowVersion);

public sealed record DecideQueryRequest(QueryDisposition Disposition, string? Note, uint RowVersion);

/// <summary>Every action returns the report as the actor now sees it.</summary>
public interface IVerificationService
{
    Task<ExpenseReportDetailDto> RaiseAsync(RaiseVerificationRequest request, CancellationToken ct = default);
    Task<PagedResult<VerificationQueueItemDto>> QueueAsync(VerificationQueueQuery query, CancellationToken ct = default);
    Task<ExpenseReportDetailDto> PickUpAsync(long id, PickUpVerificationRequest request, CancellationToken ct = default);
    Task<ExpenseReportDetailDto> RecordFindingAsync(long id, RecordFindingRequest request, CancellationToken ct = default);
    Task<ExpenseReportDetailDto> QueryAsync(long id, RaiseQueryRequest request, CancellationToken ct = default);
    Task<PagedResult<VerificationQueueItemDto>> QueriesAsync(QueryListQuery query, CancellationToken ct = default);
    Task<ExpenseReportDetailDto> DecideAsync(long id, DecideQueryRequest request, CancellationToken ct = default);
}

/// <summary>Audit event names for verification and queries (written with the report id).</summary>
public static class VerificationAuditEvents
{
    public const string Requested = "VerificationRequested";
    public const string PickedUp = "VerificationPickedUp";
    public const string Found = "VerificationFound";
    public const string QueryRaised = "QueryRaised";
    public const string QueryDecided = "QueryDecided";
}
