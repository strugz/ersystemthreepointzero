namespace ERFWeb.Application.Features.Notifications;

/// <summary>
/// Send a notification SMS to a user's text group. <see cref="ContactId"/> is the recipient's
/// <c>users.id</c>; the server looks up their numbers, so the client never names a phone number.
/// Repeat a request with the same <see cref="IdempotencyKey"/> to retry safely: a message already
/// sent under that key is never sent again.
/// <para>
/// With <see cref="ReportId"/> the SMS is about that expense report: <see cref="ContactId"/> must be its
/// claimant, and the report's Level 1 and Level 2 signatories (current approval cycle) are texted too.
/// Whoever has a mobile number on file receives it; the server picks the people, never the client.
/// </para>
/// </summary>
public sealed record SendSmsRequest(long ContactId, string? Message, string? IdempotencyKey, Guid? ReportId = null);

public enum SmsSendOutcome
{
    Sent,
    InvalidRequest,
    UserNotFound,
    ReportNotFound,
    NoTextGroup,
    NotConfigured,
    GatewayFailed,

    /// <summary>The gateway did not answer, so the SMS may or may not have gone out. It is not resent under the same key.</summary>
    GatewayUnconfirmed,

    /// <summary>A request with the same key is still being sent.</summary>
    InProgress,

    /// <summary>The key was already used for a different recipient or message.</summary>
    KeyReused,
}

/// <param name="Replayed">True when a retry found the SMS already sent and nothing new went out.</param>
public sealed record SmsSendResult(
    SmsSendOutcome Outcome,
    int RecipientCount = 0,
    bool Replayed = false,
    IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>What the API returns when the SMS was sent (or a retry found it already sent).</summary>
public sealed record SmsSentDto(string Status, int RecipientCount, bool Replayed);

public interface ISmsService
{
    /// <summary>
    /// Never throws for an expected failure; every case comes back as an outcome. Once the gateway
    /// call starts it runs to completion even if the caller's request is aborted.
    /// </summary>
    Task<SmsSendResult> SendAsync(SendSmsRequest request, CancellationToken ct = default);

    /// <summary>
    /// Server-initiated SMS about a report (e.g. on filing): texts its claimant and its Level 1 and Level 2
    /// signatories, sent as the signed-in user. Same outcomes and once-per-key guarantee as <see cref="SendAsync"/>.
    /// </summary>
    Task<SmsSendResult> SendAboutReportAsync(Guid reportId, string message, string idempotencyKey, CancellationToken ct = default);
}

/// <summary>One message for the SMS gateway. Never log <see cref="Message"/>.</summary>
public sealed record SmsGatewayMessage(string Sender, IReadOnlyList<string> Recipients, string Message);

public enum SmsGatewayStatus
{
    /// <summary>The gateway accepted the message (2xx).</summary>
    Accepted,

    /// <summary>The gateway answered with an error; nothing was sent.</summary>
    Rejected,

    /// <summary>No answer (timeout, network failure); the message may or may not have been sent.</summary>
    Unreachable,
}

public sealed record SmsGatewayResult(SmsGatewayStatus Status, int? HttpStatus = null);

/// <summary>The outbound SMS gateway (Lasius).</summary>
public interface ISmsGateway
{
    /// <summary>False when the gateway URL or API key is not set up on the server.</summary>
    bool IsConfigured { get; }

    Task<SmsGatewayResult> SendAsync(SmsGatewayMessage message, CancellationToken ct = default);
}
