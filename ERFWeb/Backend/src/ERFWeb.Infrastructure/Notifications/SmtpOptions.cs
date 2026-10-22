namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// SMTP settings for report email, bound from the <c>Smtp</c> section of appsettings.json (gitignored,
/// so the password never reaches a tracked file). The section is optional: without a host and sender
/// address the email endpoint answers "Email is not configured" and workflow emails are skipped.
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    /// <summary>Auto | None | StartTls | SslOnConnect (MailKit's SecureSocketOptions names).</summary>
    public string Security { get; set; } = "StartTls";

    /// <summary>Leave empty for a relay that does not need a login.</summary>
    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string FromName { get; set; } = "ERFWeb";

    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && Port is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(FromAddress)
        && Enum.TryParse<MailKit.Security.SecureSocketOptions>(Security, ignoreCase: true, out _);
}
