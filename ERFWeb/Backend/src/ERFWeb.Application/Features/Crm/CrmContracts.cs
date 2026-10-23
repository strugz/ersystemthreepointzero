using ERFWeb.Application.Common;

namespace ERFWeb.Application.Features.Crm;

/// <summary>
/// One CRM user as the CRM employee lookup returns it. <see cref="EmployeeId"/> is the stable link
/// stored on the ERFWeb account; every other field is for display and can change in the CRM.
/// <see cref="Workflows"/> (Sales, PS, TSG) says which CRM activities the person can claim later.
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
    bool IsActive);

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
