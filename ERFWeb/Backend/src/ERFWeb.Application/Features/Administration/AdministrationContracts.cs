using ERFWeb.Application.Common;
using ERFWeb.Domain;

namespace ERFWeb.Application.Features.Administration;

// ---------- Users and role grants ----------

/// <summary>A row in the System Administrator's user list.</summary>
public sealed record UserListItemDto(
    long Id,
    string Username,
    string FullName,
    string? Position,
    string? JobGrade,
    string? Email,
    string? MobileNo,
    long? DepartmentId,
    string? DepartmentName,
    bool IsActive,
    bool HasSignature,
    bool WorkWithEnabled,
    DateTimeOffset CreatedAt,
    IReadOnlyList<UserRole> ActiveRoles,
    IReadOnlyList<UserRole> PendingRoles);

/// <summary>One role grant on an account, with its dual-approval trail.</summary>
public sealed record RoleGrantDto(
    UserRole Role,
    RoleGrantStatus Status,
    long? GrantedByUserId,
    string? GrantedByName,
    DateTimeOffset GrantedAt,
    long? CountersignedByUserId,
    string? CountersignedByName,
    DateTimeOffset? CountersignedAt,
    long? RevokedByUserId,
    string? RevokedByName,
    DateTimeOffset? RevokedAt,
    bool IsBootstrap,
    string? Note,
    uint RowVersion);

/// <summary>Full account view for the administration console.</summary>
public sealed record UserDetailDto(
    long Id,
    string Username,
    string FullName,
    string? Position,
    string? JobGrade,
    string? Email,
    string? MobileNo,
    long? DepartmentId,
    string? DepartmentName,
    long? CostCenterId,
    bool IsActive,
    bool HasSignature,
    bool WorkWithEnabled,
    DateTimeOffset CreatedAt,
    bool CoolingPeriodElapsed,
    IReadOnlyList<RoleGrantDto> RoleGrants,
    uint RowVersion);

public sealed record UserListQuery : PagedRequest
{
    public string? Search { get; init; }
    public long? DepartmentId { get; init; }
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}

public sealed record CreateUserRequest(
    string Username,
    string Password,
    string FullName,
    string? Position,
    string? JobGrade,
    string? Email,
    string? MobileNo,
    long? DepartmentId,
    long? CostCenterId,
    bool WorkWithEnabled);

public sealed record UpdateUserRequest(
    string FullName,
    string? Position,
    string? JobGrade,
    string? Email,
    string? MobileNo,
    long? DepartmentId,
    long? CostCenterId,
    bool WorkWithEnabled,
    uint RowVersion);

public sealed record SetUserActiveRequest(bool IsActive, uint RowVersion);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed record SetSignatureRequest(string FileName, string ContentType, Stream Content);

public sealed record GrantRoleRequest(UserRole Role, string? Note);

public sealed record RoleGrantActionRequest(string? Note, uint RowVersion);

/// <summary>System Administrator account management (spec section 10, flows 1 and 2).</summary>
public interface IUserAdminService
{
    Task<PagedResult<UserListItemDto>> ListAsync(UserListQuery query, CancellationToken ct = default);
    Task<UserDetailDto> GetAsync(long userId, CancellationToken ct = default);
    Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDetailDto> UpdateAsync(long userId, UpdateUserRequest request, CancellationToken ct = default);
    Task<UserDetailDto> SetActiveAsync(long userId, SetUserActiveRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(long userId, ResetPasswordRequest request, CancellationToken ct = default);
    Task<UserDetailDto> SetSignatureAsync(long userId, SetSignatureRequest request, CancellationToken ct = default);
}

/// <summary>Role grants with dual approval: grant, countersign, revoke (spec 9.7, 9.8, stacking rules).</summary>
public interface IRoleGrantService
{
    Task<UserDetailDto> GrantAsync(long userId, GrantRoleRequest request, CancellationToken ct = default);
    Task<UserDetailDto> CountersignAsync(long userId, UserRole role, RoleGrantActionRequest request, CancellationToken ct = default);
    Task<UserDetailDto> RevokeAsync(long userId, UserRole role, RoleGrantActionRequest request, CancellationToken ct = default);
}

// ---------- Master data ----------

public sealed record DepartmentDto(long Id, string? Code, string Name, bool IsActive, uint RowVersion);

public sealed record SaveDepartmentRequest(string? Code, string Name, bool IsActive, uint? RowVersion);

public sealed record CostCenterDto(long Id, string Code, string Name, long? DepartmentId, string? DepartmentName, bool IsActive, uint RowVersion);

public sealed record SaveCostCenterRequest(string Code, string Name, long? DepartmentId, bool IsActive, uint? RowVersion);

public sealed record CurrencyDto(string Code, string Name, bool IsDefault, bool IsActive, uint RowVersion);

public sealed record SaveCurrencyRequest(string Code, string Name, bool IsDefault, bool IsActive, uint? RowVersion);

public sealed record SiteDto(long Id, string Code, string Name, bool IsActive, uint RowVersion);

public sealed record SaveSiteRequest(string Code, string Name, bool IsActive, uint? RowVersion);

/// <summary>The fixed expense categories (R47 columns) exposed read-only for the console.</summary>
public sealed record ExpenseCategoryDto(ExpenseCategory Category, string Label, string R47Column);

/// <summary>Master data maintained by the System Administrator (flow 1, step 2).</summary>
public interface IMasterDataService
{
    Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(bool includeInactive, CancellationToken ct = default);
    Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken ct = default);
    Task<DepartmentDto> UpdateDepartmentAsync(long id, SaveDepartmentRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CostCenterDto>> ListCostCentersAsync(bool includeInactive, CancellationToken ct = default);
    Task<CostCenterDto> CreateCostCenterAsync(SaveCostCenterRequest request, CancellationToken ct = default);
    Task<CostCenterDto> UpdateCostCenterAsync(long id, SaveCostCenterRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CurrencyDto>> ListCurrenciesAsync(bool includeInactive, CancellationToken ct = default);
    Task<CurrencyDto> CreateCurrencyAsync(SaveCurrencyRequest request, CancellationToken ct = default);
    Task<CurrencyDto> UpdateCurrencyAsync(string code, SaveCurrencyRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<SiteDto>> ListSitesAsync(bool includeInactive, CancellationToken ct = default);
    Task<SiteDto> CreateSiteAsync(SaveSiteRequest request, CancellationToken ct = default);
    Task<SiteDto> UpdateSiteAsync(long id, SaveSiteRequest request, CancellationToken ct = default);

    IReadOnlyList<ExpenseCategoryDto> ListExpenseCategories();
}

// ---------- Workflow, business, and notification rules ----------

/// <summary>
/// System-wide rules kept in the database so the System Administrator can change them without
/// a deployment. Every value has a default; see <c>SystemSettingKeys</c>.
/// </summary>
public sealed record SystemSettingsDto(
    decimal Level2ThresholdAmount,
    bool Level2AlwaysRequired,
    int SlaDaysLevel1,
    int SlaDaysLevel2,
    int EscalationAfterDays,
    int FilingCutoffDays,
    decimal ReceiptRequiredAboveAmount,
    int CoolingPeriodDays,
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled,
    string? NotificationFromAddress,
    DateTimeOffset? UpdatedAt,
    string? UpdatedByName);

public sealed record UpdateSystemSettingsRequest(
    decimal Level2ThresholdAmount,
    bool Level2AlwaysRequired,
    int SlaDaysLevel1,
    int SlaDaysLevel2,
    int EscalationAfterDays,
    int FilingCutoffDays,
    decimal ReceiptRequiredAboveAmount,
    int CoolingPeriodDays,
    bool EmailNotificationsEnabled,
    bool SmsNotificationsEnabled,
    string? NotificationFromAddress);

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetAsync(CancellationToken ct = default);
    Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken ct = default);

    /// <summary>Cheap read of the one rule the filing path needs.</summary>
    Task<int> GetCoolingPeriodDaysAsync(CancellationToken ct = default);
}

// ---------- Audit trail (read-only, spec 9.10) ----------

public sealed record AdminAuditEntryDto(
    long Id,
    string Event,
    long? ActorUserId,
    string? ActorName,
    Guid? ReportId,
    DateTimeOffset OccurredAt,
    string? Detail,
    Guid? CorrelationId);

public sealed record AdminAuditQuery : PagedRequest
{
    public string? Event { get; init; }
    public long? ActorUserId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }

    /// <summary>When true, only administration events (accounts, roles, master data, rules) are returned.</summary>
    public bool AdministrationOnly { get; init; } = true;
}

public interface IAdminAuditService
{
    Task<PagedResult<AdminAuditEntryDto>> ListAsync(AdminAuditQuery query, CancellationToken ct = default);
}

/// <summary>Audit event names written by the administration services. Prefix identifies the area.</summary>
public static class AdminAuditEvents
{
    public const string Prefix = "Admin.";

    public const string UserCreated = "Admin.UserCreated";
    public const string UserUpdated = "Admin.UserUpdated";
    public const string UserActivated = "Admin.UserActivated";
    public const string UserDeactivated = "Admin.UserDeactivated";
    public const string PasswordReset = "Admin.PasswordReset";
    public const string SignatureSet = "Admin.SignatureSet";
    public const string RoleGranted = "Admin.RoleGranted";
    public const string RoleCountersigned = "Admin.RoleCountersigned";
    public const string RoleRevoked = "Admin.RoleRevoked";
    public const string MasterDataChanged = "Admin.MasterDataChanged";
    public const string SettingsChanged = "Admin.SettingsChanged";
}
