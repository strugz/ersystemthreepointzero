namespace ERFWeb.Infrastructure.Crm;

/// <summary>
/// CRM API settings, bound from the <c>Crm</c> section of appsettings.json (gitignored, so the API key
/// never reaches a tracked file). The section is optional: without a base URL and key the CRM lookup
/// answers "CRM lookup is not configured" instead of failing at startup.
/// </summary>
public sealed class CrmOptions
{
    public const string SectionName = "Crm";

    /// <summary>Absolute http(s) URL up to the version, e.g. https://host/api2/erf/v1 (no trailing path).</summary>
    public string? BaseUrl { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>Request header that carries <see cref="ApiKey"/>.</summary>
    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

    /// <summary>The CRM team asks for an answer within 5 seconds; this is the hard stop.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    public bool IsConfigured =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiKeyHeaderName);
}
