using System.Net;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Crm;

/// <summary>
/// Calls the CRM employee lookup (<c>GET /employees</c> and <c>GET /employees/{employeeId}</c>) with the
/// server-held API key in a header. Logs only the HTTP status: never the search text (it is a person's
/// name), the key, or the response body.
/// </summary>
public sealed class CrmEmployeeDirectory(HttpClient http, IOptions<CrmOptions> options, ILogger<CrmEmployeeDirectory> logger) : ICrmEmployeeDirectory
{
    public const int MinSearchLength = 2;
    public const int MaxEmployeeIdLength = 50;

    private readonly CrmApi _api = new(
        http,
        options.Value,
        logger,
        "an employee lookup",
        "The CRM lookup is not set up on the server yet. Save the account without a CRM link, or contact the system administrator.",
        "The CRM could not be reached. Try again later, or save the account without a CRM link.");

    public bool IsConfigured => _api.IsConfigured;

    public async Task<PagedResult<CrmEmployeeDto>> SearchAsync(CrmEmployeeSearchQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var search = query.Search?.Trim() ?? string.Empty;
        if (search.Length < MinSearchLength)
        {
            throw new ValidationException("search", $"Type at least {MinSearchLength} characters to search the CRM.");
        }

        var path = $"employees?search={Uri.EscapeDataString(search)}"
            + $"&includeInactive={(query.IncludeInactive ? "true" : "false")}"
            + $"&page={query.NormalizedPage}&pageSize={query.NormalizedPageSize}";

        using var response = await _api.SendAsync(path, ct);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ValidationException("search", "The CRM did not accept that search. Try different words.");
        }

        _api.EnsureSuccess(response);
        var page = await _api.ReadAsync<CrmPage>(response, ct);
        var items = (page.Items ?? []).Where(e => !string.IsNullOrWhiteSpace(e.EmployeeId)).Select(Map).ToList();
        return new PagedResult<CrmEmployeeDto>(items, page.TotalCount, page.Page < 1 ? query.NormalizedPage : page.Page, page.PageSize < 1 ? query.NormalizedPageSize : page.PageSize);
    }

    public async Task<CrmEmployeeDto?> GetAsync(string employeeId, CancellationToken ct = default)
    {
        var id = employeeId?.Trim() ?? string.Empty;
        if (id.Length == 0 || id.Length > MaxEmployeeIdLength)
        {
            return null;
        }

        using var response = await _api.SendAsync($"employees/{Uri.EscapeDataString(id)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        _api.EnsureSuccess(response);
        var employee = await _api.ReadAsync<CrmEmployee>(response, ct);
        return string.IsNullOrWhiteSpace(employee.EmployeeId) ? null : Map(employee);
    }

    private static CrmEmployeeDto Map(CrmEmployee e) => new(
        e.EmployeeId!.Trim(),
        e.FirstName,
        e.MiddleName,
        e.LastName,
        e.Nickname,
        e.FullName,
        e.Department,
        e.Section,
        e.Workflows ?? [],
        e.IsActive,
        // mobileNumber is the CRM's CNTNUM column; null once the person has left.
        string.IsNullOrWhiteSpace(e.MobileNumber) ? null : e.MobileNumber.Trim());

    private sealed record CrmPage(int Page, int PageSize, int TotalCount, IReadOnlyList<CrmEmployee>? Items);

    private sealed record CrmEmployee(
        string? EmployeeId,
        string? FirstName,
        string? MiddleName,
        string? LastName,
        string? Nickname,
        string? FullName,
        string? Department,
        string? Section,
        IReadOnlyList<string>? Workflows,
        bool IsActive,
        string? MobileNumber);
}
