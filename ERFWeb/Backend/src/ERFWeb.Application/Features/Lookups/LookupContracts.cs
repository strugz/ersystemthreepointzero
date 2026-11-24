using ERFWeb.Domain;

namespace ERFWeb.Application.Features.Lookups;

/// <summary>A colleague selectable in the "Work With" picker.</summary>
public sealed record WorkWithOptionDto(long UserId, string FullName, string? Position, string? Department);

/// <summary>
/// The signed-in employee's configured per-diem / transport rates. <see cref="LodgingRate"/>: per night, from the
/// rate profile or the Finance rules' flat amount (0 when none is set).
/// </summary>
public sealed record EmployeeRatesDto(
    decimal TransportRate,
    decimal BreakfastRate,
    decimal LunchRate,
    decimal DinnerRate,
    decimal OtMealRate,
    decimal LodgingRate = 0m);

/// <summary>
/// Whether the signed-in employee can file (signature on record, approvers configured, cooling period over).
/// <see cref="CrmActivitiesAvailable"/>: the account is linked to the CRM and the server can call it, so the
/// line editor offers CRM activities. <see cref="ProcessingDate"/>: the Friday a report filed now is processed in
/// (null when the weekly cutoff is off); <see cref="PastFilingCutoff"/>: it missed this week's cutoff
/// (<see cref="FilingCutoffText"/>, e.g. "Wednesday 12:00 PM"). A notice only; it never blocks filing.
/// <see cref="FleetCardGasReasons"/>: the employee has a fleet card and the rule is on, so a Gas line's Reason is one
/// of these (null otherwise); <see cref="FleetCardOtherReasonAllowed"/>: they may type another one.
/// </summary>
public sealed record MyFilingReadinessDto(
    bool HasSignature,
    int ApproverCount,
    IReadOnlyList<string> ApproverNames,
    bool CoolingPeriodElapsed,
    DateTimeOffset? CoolingPeriodEndsAt,
    string? RoutingProblem,
    bool CrmActivitiesAvailable = false,
    DateOnly? ProcessingDate = null,
    bool PastFilingCutoff = false,
    string? FilingCutoffText = null,
    IReadOnlyList<ExpenseCategory>? UnavailableCategories = null,
    IReadOnlyList<RequiredDocumentDto>? RequiredDocuments = null,
    string? WorkWithNotice = null,
    IReadOnlyList<string>? FleetCardGasReasons = null,
    bool FleetCardOtherReasonAllowed = false,
    IReadOnlyList<DepartmentOptionDto>? Departments = null);

/// <summary>A category whose lines need a document of <see cref="DocumentType"/> attached before filing (Finance rules).</summary>
public sealed record RequiredDocumentDto(ExpenseCategory Category, AttachmentDocumentType DocumentType, string Label);

/// <summary>An active department, for pickers.</summary>
public sealed record DepartmentOptionDto(long Id, string? Code, string Name);

/// <summary>An active fare mode for the fare-leg picker, in display order.</summary>
public sealed record FareModeOptionDto(long Id, string Code, string Name, bool RequiresDetail, bool ReceiptExpected);

public interface ILookupService
{
    Task<IReadOnlyList<FareModeOptionDto>> FareModesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DepartmentOptionDto>> DepartmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WorkWithOptionDto>> WorkWithAsync(string? search, CancellationToken ct = default);
    /// <summary><paramref name="departmentId"/> is the report's department; one the employee may not charge to is ignored.</summary>
    Task<EmployeeRatesDto> MyRatesAsync(DateOnly? date = null, long? departmentId = null, CancellationToken ct = default);

    /// <summary><paramref name="departmentId"/> is the report's department (null: the main one); the route is previewed for it.</summary>
    Task<MyFilingReadinessDto> MyFilingReadinessAsync(long? departmentId = null, CancellationToken ct = default);
    Task<MyVehicleDto> MyVehicleAsync(CancellationToken ct = default);
    Task<MyVehicleDto> UpdateMyVehicleAsync(UpdateMyVehicleRequest request, CancellationToken ct = default);
}

/// <summary>The signed-in employee's issued vehicle, filled into new Gas and Vehicle emergency purchase lines; nulls when none is saved.</summary>
public sealed record MyVehicleDto(string? Model, string? PlateNo);

/// <summary>Saves (or, with both empty, clears) the signed-in employee's vehicle.</summary>
public sealed record UpdateMyVehicleRequest(string? Model, string? PlateNo);

/// <summary>Audit events for changes employees make to their own account.</summary>
public static class AccountAuditEvents
{
    public const string VehicleChanged = "Account.VehicleChanged";
}
