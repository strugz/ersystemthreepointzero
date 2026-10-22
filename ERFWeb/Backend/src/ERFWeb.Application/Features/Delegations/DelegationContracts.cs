using ERFWeb.Domain.Approval;

namespace ERFWeb.Application.Features.Delegations;

public sealed record DelegationDto(
    long Id,
    long SignatoryUserId,
    string SignatoryName,
    int? SignatoryLevel,
    long DelegateUserId,
    string DelegateName,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string? Reason,
    bool IsForced,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    string? CancelledByName,
    DateTimeOffset? CancelledAt,
    DelegationState State,
    bool CanCancel,
    uint RowVersion);

/// <summary>Delegations that concern the current user.</summary>
/// <param name="Given">I am the signatory (including ones my Level 2 assigned for me).</param>
/// <param name="Received">I am the delegate.</param>
/// <param name="Assigned">I assigned them, as Level 2, for a Level 1.</param>
public sealed record MyDelegationsDto(
    IReadOnlyList<DelegationDto> Given,
    IReadOnlyList<DelegationDto> Received,
    IReadOnlyList<DelegationDto> Assigned);

/// <summary>A signatory who could be chosen as a delegate.</summary>
public sealed record DelegateCandidateDto(long UserId, string FullName, string? DepartmentName, int Level);

/// <summary>A Level 1 the current user is the Level 2 over, for assigning a delegate in their place.</summary>
public sealed record CoveredSignatoryDto(long UserId, string FullName, string? DepartmentName, int PendingReports);

/// <summary>What the create form needs: my level, who can stand in, and the Level 1s I may assign for.</summary>
public sealed record DelegationOptionsDto(
    int? MyLevel,
    DateOnly Today,
    int MaxDays,
    IReadOnlyList<DelegateCandidateDto> Candidates,
    IReadOnlyList<CoveredSignatoryDto> CoveredLevel1s);

/// <summary>
/// Names a delegate. Leave <see cref="SignatoryUserId"/> empty to delegate your own approvals; set it
/// (Level 2 only) to assign a delegate for an absent Level 1.
/// </summary>
public sealed record CreateDelegationRequest(long? SignatoryUserId, long DelegateUserId, DateOnly StartsOn, DateOnly EndsOn, string? Reason);

public sealed record CancelDelegationRequest(uint RowVersion);

public interface IDelegationService
{
    Task<MyDelegationsDto> MineAsync(CancellationToken ct = default);
    Task<DelegationOptionsDto> OptionsAsync(CancellationToken ct = default);
    Task<DelegationDto> CreateAsync(CreateDelegationRequest request, CancellationToken ct = default);
    Task<DelegationDto> CancelAsync(long delegationId, CancelDelegationRequest request, CancellationToken ct = default);
}

/// <summary>Audit event names for delegation (no report id).</summary>
public static class DelegationAuditEvents
{
    public const string Created = "ApprovalDelegationCreated";
    public const string Cancelled = "ApprovalDelegationCancelled";
}
