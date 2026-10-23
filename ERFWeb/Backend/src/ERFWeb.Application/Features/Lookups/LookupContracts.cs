namespace ERFWeb.Application.Features.Lookups;

/// <summary>A colleague selectable in the "Work With" picker.</summary>
public sealed record WorkWithOptionDto(long UserId, string FullName, string? Position, string? Department);

/// <summary>The signed-in employee's configured per-diem / transport rates.</summary>
public sealed record EmployeeRatesDto(
    decimal TransportRate,
    decimal BreakfastRate,
    decimal LunchRate,
    decimal DinnerRate,
    decimal OtMealRate);

/// <summary>
/// Whether the signed-in employee can file (signature on record, approvers configured, cooling period over).
/// <see cref="CrmActivitiesAvailable"/>: the account is linked to the CRM and the server can call it, so the
/// line editor offers CRM activities.
/// </summary>
public sealed record MyFilingReadinessDto(
    bool HasSignature,
    int ApproverCount,
    IReadOnlyList<string> ApproverNames,
    bool CoolingPeriodElapsed,
    DateTimeOffset? CoolingPeriodEndsAt,
    string? RoutingProblem,
    bool CrmActivitiesAvailable = false);

/// <summary>An active department, for pickers.</summary>
public sealed record DepartmentOptionDto(long Id, string? Code, string Name);

/// <summary>An active fare mode for the fare-leg picker, in display order.</summary>
public sealed record FareModeOptionDto(long Id, string Code, string Name, bool RequiresDetail, bool ReceiptExpected);

public interface ILookupService
{
    Task<IReadOnlyList<FareModeOptionDto>> FareModesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DepartmentOptionDto>> DepartmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WorkWithOptionDto>> WorkWithAsync(string? search, CancellationToken ct = default);
    Task<EmployeeRatesDto> MyRatesAsync(DateOnly? date = null, CancellationToken ct = default);
    Task<MyFilingReadinessDto> MyFilingReadinessAsync(CancellationToken ct = default);
}
