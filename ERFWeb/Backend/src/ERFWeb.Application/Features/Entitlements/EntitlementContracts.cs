using ERFWeb.Application.Features.Lookups;
using ERFWeb.Domain;

namespace ERFWeb.Application.Features.Entitlements;

// ---------- Scope assignments (who signs for whom) ----------

public sealed record ScopeMemberDto(long UserId, string FullName);

public sealed record ScopeAssignmentDto(
    long Id,
    long HolderUserId,
    string HolderName,
    int Level,
    ScopeAssignmentType Type,
    long? DepartmentId,
    string? DepartmentName,
    IReadOnlyList<ScopeMemberDto> Members,
    decimal? ApprovalLimit,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    bool IsBootstrap,
    string? CreatedByName,
    DateTimeOffset CreatedAt,
    string? Note,
    uint RowVersion);

public sealed record ScopeAssignmentQuery(long? HolderUserId, long? DepartmentId, int? Level, bool IncludeInactive);

public sealed record CreateScopeAssignmentRequest(
    long HolderUserId,
    int Level,
    ScopeAssignmentType Type,
    long? DepartmentId,
    IReadOnlyList<long>? MemberUserIds,
    decimal? ApprovalLimit,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Note);

/// <summary>
/// Every field can change. A new effective-from date cannot be in the past; an unchanged one may be. Reports
/// already filed keep the routing they were filed with.
/// </summary>
public sealed record UpdateScopeAssignmentRequest(
    long HolderUserId,
    int Level,
    ScopeAssignmentType Type,
    long? DepartmentId,
    DateOnly? EffectiveFrom,
    IReadOnlyList<long>? MemberUserIds,
    decimal? ApprovalLimit,
    DateOnly? EffectiveTo,
    bool IsActive,
    string? Note,
    uint RowVersion);

/// <summary>One active employee and who signs for them today.</summary>
public sealed record ScopeCoverageRowDto(
    long UserId,
    string FullName,
    string? DepartmentName,
    long? Level1UserId,
    string? Level1Name,
    string? Level1Problem,
    long? Level2UserId,
    string? Level2Name,
    string? Level2Problem);

public interface IScopeService
{
    Task<IReadOnlyList<ScopeAssignmentDto>> ListAsync(ScopeAssignmentQuery query, CancellationToken ct = default);
    Task<ScopeAssignmentDto> GetAsync(long id, CancellationToken ct = default);
    Task<ScopeAssignmentDto> CreateAsync(CreateScopeAssignmentRequest request, CancellationToken ct = default);
    Task<ScopeAssignmentDto> UpdateAsync(long id, UpdateScopeAssignmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ScopeCoverageRowDto>> CoverageAsync(DateOnly? date, CancellationToken ct = default);
}

// ---------- Approval routing at filing ----------

public sealed record RoutedApproverDto(int StepOrder, int Level, long UserId, string FullName, string? Email, string? MobileNo);

public sealed record RoutingResultDto(IReadOnlyList<RoutedApproverDto> Approvers, string? Problem)
{
    public bool IsRouted => Problem is null && Approvers.Count > 0;
}

/// <summary>Resolves the approval steps for a claimant from scope and the workflow rules.</summary>
public interface IApprovalRouter
{
    /// <summary>
    /// <paramref name="chargedDepartmentId"/> is the other department a report is charged to (null: the main one);
    /// that department's signatories then sign, and by-name assignments only as the Rules page says.
    /// </summary>
    Task<RoutingResultDto> RouteAsync(long claimantUserId, decimal reportTotal, DateOnly date, long? chargedDepartmentId = null, CancellationToken ct = default);
}

// ---------- Rate profiles ----------

public sealed record RateLineDto(
    long Id,
    long ProfileId,
    ExpenseCategory Category,
    string? SubCategory,
    RateType RateType,
    decimal Amount,
    string Currency,
    int? FrequencyCap,
    bool RequiresReceipt,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ChangedByName,
    string? ChangeReason,
    bool BackdateOverride,
    DateTimeOffset CreatedAt,
    uint RowVersion);

public sealed record RateAssignmentDto(
    long Id,
    long ProfileId,
    string ProfileName,
    RateTargetType TargetType,
    long? UserId,
    string? UserName,
    string? JobGrade,
    long? DepartmentId,
    string? DepartmentName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? AssignedByName,
    DateTimeOffset AssignedAt,
    bool IsBootstrap,
    string? ChangeReason,
    uint RowVersion);

public sealed record RateProfileListItemDto(long Id, string Name, bool IsActive, int CurrentLineCount, int ActiveAssignmentCount, uint RowVersion);

public sealed record RateProfileDetailDto(
    long Id,
    string Name,
    bool IsActive,
    IReadOnlyList<RateLineDto> Lines,
    IReadOnlyList<RateAssignmentDto> Assignments,
    uint RowVersion);

public sealed record CreateRateProfileRequest(string Name);

public sealed record UpdateRateProfileRequest(string Name, bool IsActive, uint RowVersion);

public sealed record AddRateLineRequest(
    ExpenseCategory Category,
    string? SubCategory,
    RateType RateType,
    decimal Amount,
    string? Currency,
    int? FrequencyCap,
    bool RequiresReceipt,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ChangeReason,
    bool BackdateOverride);

/// <summary>Corrects one version in place; its category and meal stay. A reason is required.</summary>
public sealed record UpdateRateLineRequest(
    RateType RateType,
    decimal Amount,
    string? Currency,
    int? FrequencyCap,
    bool RequiresReceipt,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ChangeReason,
    bool BackdateOverride,
    uint RowVersion);

public sealed record CreateRateAssignmentRequest(
    long ProfileId,
    RateTargetType TargetType,
    long? UserId,
    string? JobGrade,
    long? DepartmentId,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ChangeReason,
    bool BackdateOverride);

/// <summary>Corrects one assignment in place; its profile stays. A reason is required.</summary>
public sealed record UpdateRateAssignmentRequest(
    RateTargetType TargetType,
    long? UserId,
    string? JobGrade,
    long? DepartmentId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? ChangeReason,
    bool BackdateOverride,
    uint RowVersion);

public sealed record EndRateAssignmentRequest(DateOnly EffectiveTo, string? ChangeReason, uint RowVersion);

public sealed record RateAssignmentQuery(long? ProfileId, RateTargetType? TargetType, bool IncludeEnded);

/// <summary>
/// The lines that apply to one person on one date. Each category comes from the highest-ranked profile that has it
/// (a line's <c>ProfileId</c> names its profile); <c>Sources</c> lists those profiles in rank order. ProfileId,
/// ProfileName and Via are the highest-ranked assignment.
/// </summary>
public sealed record ResolvedRatesDto(
    long UserId,
    DateOnly Date,
    long? ProfileId,
    string? ProfileName,
    RateTargetType? Via,
    IReadOnlyList<RateLineDto> Lines,
    IReadOnlyList<ResolvedRateSourceDto> Sources);

public sealed record ResolvedRateSourceDto(long ProfileId, string ProfileName, RateTargetType Via);

public interface IRateService
{
    Task<IReadOnlyList<RateProfileListItemDto>> ListProfilesAsync(bool includeInactive, CancellationToken ct = default);
    Task<RateProfileDetailDto> GetProfileAsync(long profileId, CancellationToken ct = default);
    Task<RateProfileDetailDto> CreateProfileAsync(CreateRateProfileRequest request, CancellationToken ct = default);
    Task<RateProfileDetailDto> UpdateProfileAsync(long profileId, UpdateRateProfileRequest request, CancellationToken ct = default);
    Task<RateProfileDetailDto> AddLineAsync(long profileId, AddRateLineRequest request, CancellationToken ct = default);
    Task<RateProfileDetailDto> UpdateLineAsync(long profileId, long lineId, UpdateRateLineRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<RateAssignmentDto>> ListAssignmentsAsync(RateAssignmentQuery query, CancellationToken ct = default);
    Task<RateAssignmentDto> CreateAssignmentAsync(CreateRateAssignmentRequest request, CancellationToken ct = default);
    Task<RateAssignmentDto> UpdateAssignmentAsync(long assignmentId, UpdateRateAssignmentRequest request, CancellationToken ct = default);
    Task<RateAssignmentDto> EndAssignmentAsync(long assignmentId, EndRateAssignmentRequest request, CancellationToken ct = default);

    /// <summary>Today in the business calendar when no date is given.</summary>
    Task<ResolvedRatesDto> ResolveAsync(long userId, DateOnly? date, CancellationToken ct = default);

    /// <summary>
    /// The meal, transport and lodging amounts the line editor offers, as resolved for the date. A report charged to
    /// another department (<paramref name="chargedDepartmentId"/>) uses that department's rates when the Rules page says so.
    /// </summary>
    Task<EmployeeRatesDto> EditorRatesAsync(long userId, DateOnly date, long? chargedDepartmentId = null, CancellationToken ct = default);
}

// ---------- Change notifications (spec 9.14) ----------

public sealed record EntitlementChangeNotification(
    string Kind,
    string Summary,
    long ActorUserId,
    IReadOnlyList<long> AffectedUserIds,
    string? Before,
    string? After);

/// <summary>Tells affected employees (and their Level 1) about scope and rate changes. Must never throw.</summary>
public interface IEntitlementChangeNotifier
{
    Task NotifyAsync(EntitlementChangeNotification notification, CancellationToken ct = default);
}

/// <summary>Audit event names for scope and rates. Finance and System Administrators read them (spec 9.10).</summary>
public static class EntitlementAuditEvents
{
    public const string ScopePrefix = "Scope.";
    public const string RatesPrefix = "Rates.";

    public const string ScopeCreated = "Scope.AssignmentCreated";
    public const string ScopeUpdated = "Scope.AssignmentUpdated";
    public const string ProfileCreated = "Rates.ProfileCreated";
    public const string ProfileUpdated = "Rates.ProfileUpdated";
    public const string LineAdded = "Rates.LineAdded";
    public const string LineCorrected = "Rates.LineCorrected";
    public const string AssignmentCreated = "Rates.AssignmentCreated";
    public const string AssignmentCorrected = "Rates.AssignmentCorrected";
    public const string AssignmentEnded = "Rates.AssignmentEnded";
}
