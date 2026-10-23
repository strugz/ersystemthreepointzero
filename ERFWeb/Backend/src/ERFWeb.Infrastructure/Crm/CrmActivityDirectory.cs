using System.Globalization;
using System.Net;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Crm;

/// <summary>
/// Calls the CRM activity API (<c>GET /activities</c> and <c>GET /activities/{activityId}</c>) with the
/// server-held API key. Field names follow the CRM team's "CRM Employee and Activity API for ERFWeb" doc.
/// Logs only the HTTP status: never the employee ID, the activity, or the key.
/// </summary>
public sealed class CrmActivityDirectory(HttpClient http, IOptions<CrmOptions> options, ILogger<CrmActivityDirectory> logger) : ICrmActivityDirectory
{
    public const int MaxActivityIdLength = 50;

    /// <summary>The CRM refuses a longer range with 400.</summary>
    public const int MaxRangeDays = 62;

    private readonly CrmApi _api = new(
        http,
        options.Value,
        logger,
        "an activity lookup",
        "CRM activities are not set up on the server yet. Type the line instead, or contact the system administrator.",
        "The CRM could not be reached. Try again later, or type the line instead.");

    public bool IsConfigured => _api.IsConfigured;

    public async Task<PagedResult<CrmActivityDto>?> ListAsync(string employeeId, DateOnly from, DateOnly to, string status, int page, int pageSize, CancellationToken ct = default)
    {
        var id = employeeId?.Trim() ?? string.Empty;
        if (id.Length == 0 || id.Length > CrmEmployeeDirectory.MaxEmployeeIdLength)
        {
            return null;
        }

        var path = $"activities?employeeId={Uri.EscapeDataString(id)}"
            + $"&dateFrom={from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
            + $"&dateTo={to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
            + $"&status={Uri.EscapeDataString(status)}&page={page}&pageSize={pageSize}";

        using var response = await _api.SendAsync(path, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ValidationException("dateFrom", $"The CRM did not accept that search. It lists at most {MaxRangeDays} days at a time.");
        }

        _api.EnsureSuccess(response);
        var body = await _api.ReadAsync<CrmPage>(response, ct);
        var items = (body.Items ?? []).Select(Map).OfType<CrmActivityDto>().ToList();
        return new PagedResult<CrmActivityDto>(items, body.TotalCount, body.Page < 1 ? page : body.Page, body.PageSize < 1 ? pageSize : body.PageSize);
    }

    public async Task<CrmActivityDto?> GetAsync(string activityId, CancellationToken ct = default)
    {
        var id = activityId?.Trim() ?? string.Empty;
        if (id.Length == 0 || id.Length > MaxActivityIdLength)
        {
            return null;
        }

        using var response = await _api.SendAsync($"activities/{Uri.EscapeDataString(id)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        _api.EnsureSuccess(response);
        return Map(await _api.ReadAsync<CrmActivity>(response, ct));
    }

    /// <summary>Null for an item ERFWeb cannot use (no ID or no date).</summary>
    private static CrmActivityDto? Map(CrmActivity a)
    {
        if (string.IsNullOrWhiteSpace(a.ActivityId) || a.ActivityDate is not { } date)
        {
            return null;
        }

        return new CrmActivityDto(
            // ITI- and SR- come back in any letter case; one spelling keeps the claimed-twice check exact.
            a.ActivityId.Trim().ToUpperInvariant(),
            Clean(a.ActivityType),
            Clean(a.Status),
            date,
            a.EndAt,
            Clean(a.Subject),
            Clean(a.ServiceNumber),
            Clean(a.Customer?.Name),
            Clean(a.Instrument?.Name),
            Clean(a.Instrument?.SerialNumber),
            Person(a.Employee),
            (a.Companions ?? []).Select(Person).OfType<CrmActivityPersonDto>().ToList(),
            Clean(a.Travel?.Origin),
            Clean(a.Travel?.Destination),
            a.Travel?.IsOvernight ?? false);
    }

    private static CrmActivityPersonDto? Person(CrmPerson? p) =>
        p is null || string.IsNullOrWhiteSpace(p.EmployeeId) ? null : new(p.EmployeeId.Trim(), Clean(p.Name), Clean(p.Role));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CrmPage(int Page, int PageSize, int TotalCount, IReadOnlyList<CrmActivity>? Items);

    private sealed record CrmActivity(
        string? ActivityId,
        string? ActivityType,
        string? Status,
        DateOnly? ActivityDate,
        DateTimeOffset? EndAt,
        string? Subject,
        string? ServiceNumber,
        CrmCustomer? Customer,
        CrmInstrument? Instrument,
        CrmPerson? Employee,
        IReadOnlyList<CrmPerson>? Companions,
        CrmTravel? Travel);

    private sealed record CrmCustomer(string? Name);

    private sealed record CrmInstrument(string? Name, string? SerialNumber);

    private sealed record CrmPerson(string? EmployeeId, string? Name, string? Role);

    private sealed record CrmTravel(string? Origin, string? Destination, bool? IsOvernight);
}
