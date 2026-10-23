using ERFWeb.Application.Common;

namespace ERFWeb.Application.Features.Crm;

/// <summary>
/// One CRM user as the CRM employee lookup returns it. <see cref="EmployeeId"/> is the stable link
/// stored on the ERFWeb account; every other field is for display and can change in the CRM.
/// <see cref="Workflows"/> (Sales, PS, TSG) says which CRM activities the person can claim later.
/// <see cref="MobileNo"/> only prefills the account form; it is null until the CRM sends one.
/// </summary>
public sealed record CrmEmployeeDto(
    string EmployeeId,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? Nickname,
    string? FullName,
    string? Department,
    string? Section,
    IReadOnlyList<string> Workflows,
    bool IsActive,
    string? MobileNo = null);

public sealed record CrmEmployeeSearchQuery : PagedRequest
{
    public string? Search { get; init; }
    public bool IncludeInactive { get; init; }
}

/// <summary>
/// Read-only CRM employee lookup (the CRM team's "CRM Employee Lookup API for ERFWeb"). Throws
/// <see cref="ServiceUnavailableException"/> when the CRM is not configured or does not answer, and
/// <see cref="ValidationException"/> when the CRM refuses the query.
/// </summary>
public interface ICrmEmployeeDirectory
{
    bool IsConfigured { get; }

    Task<PagedResult<CrmEmployeeDto>> SearchAsync(CrmEmployeeSearchQuery query, CancellationToken ct = default);

    /// <summary>The employee with this CRM ID (leavers included), or null when the CRM has no such ID.</summary>
    Task<CrmEmployeeDto?> GetAsync(string employeeId, CancellationToken ct = default);
}

/// <summary>A person on a CRM activity. <see cref="Role"/> is Lead (the owner) or Companion (work-with).</summary>
public sealed record CrmActivityPersonDto(string EmployeeId, string? Name, string? Role);

/// <summary>
/// One CRM activity as the CRM activity API returns it: a Sales itinerary visit (<c>ITI-</c>) or a PS/TSG
/// service report (<c>SR-</c>). The CRM supplies what, where, when and with whom; ERFWeb keeps every amount.
/// </summary>
public sealed record CrmActivityDto(
    string ActivityId,
    string? ActivityType,
    string? Status,
    DateOnly ActivityDate,
    DateTimeOffset? EndAt,
    string? Subject,
    string? ServiceNumber,
    string? CustomerName,
    string? InstrumentName,
    string? SerialNumber,
    CrmActivityPersonDto? Employee,
    IReadOnlyList<CrmActivityPersonDto> Companions,
    string? TravelOrigin,
    string? TravelDestination,
    bool IsOvernight)
{
    public const string CompletedStatus = "Completed";

    public bool IsCompleted => string.Equals(Status, CompletedStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the employee did this activity, as its owner or as a work-with companion.</summary>
    public bool Involves(string employeeId) =>
        string.Equals(Employee?.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase)
        || Companions.Any(c => string.Equals(c.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Read-only CRM activity API (<c>GET /activities</c>, <c>GET /activities/{activityId}</c>). Throws
/// <see cref="ServiceUnavailableException"/> when the CRM is not configured or does not answer, and
/// <see cref="ValidationException"/> when the CRM refuses the query.
/// </summary>
public interface ICrmActivityDirectory
{
    bool IsConfigured { get; }

    /// <summary>The employee's Completed activities from <paramref name="from"/> to <paramref name="to"/> (at most 62 days); null when the CRM has no such employee.</summary>
    Task<PagedResult<CrmActivityDto>?> ListAsync(string employeeId, DateOnly from, DateOnly to, int page, int pageSize, CancellationToken ct = default);

    /// <summary>The activity as its owner sees it, whatever its status, or null when the CRM has no such ID.</summary>
    Task<CrmActivityDto?> GetAsync(string activityId, CancellationToken ct = default);
}
