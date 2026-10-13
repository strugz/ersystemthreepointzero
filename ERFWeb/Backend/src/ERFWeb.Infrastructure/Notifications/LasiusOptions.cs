namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Lasius SMS gateway settings, bound from the <c>Lasius</c> section of appsettings.json (gitignored,
/// so the API key never reaches a tracked file). The section is optional: without a URL and key the
/// SMS endpoint answers "SMS is not configured" instead of failing at startup.
/// </summary>
public sealed class LasiusOptions
{
    public const string SectionName = "Lasius";

    /// <summary>Absolute http(s) URL the payload is POSTed to.</summary>
    public string? Url { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>Request header that carries <see cref="ApiKey"/>.</summary>
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";

    /// <summary>The <c>application</c> value Lasius files the message under.</summary>
    public string Application { get; set; } = "ERMS";

    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured =>
        Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiKeyHeaderName);
}
