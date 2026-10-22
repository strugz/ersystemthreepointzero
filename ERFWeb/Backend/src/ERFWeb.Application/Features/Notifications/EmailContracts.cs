namespace ERFWeb.Application.Features.Notifications;

/// <summary>
/// Email the R47 PDF of a filed report. The server picks the recipients: the claimant, the report's
/// Level 1 and Level 2 signatories (current approval cycle), and the person sending, whoever has an
/// email address on file. Repeat a request with the same <see cref="IdempotencyKey"/> to retry safely.
/// </summary>
public sealed record SendReportEmailRequest(string? IdempotencyKey);

public enum EmailSendOutcome
{
    Sent,
    InvalidRequest,
    ReportNotFound,

    /// <summary>A draft has not been filed, so it has no R47 to send yet.</summary>
    ReportNotFiled,

    NoRecipients,
    NotConfigured,
    GatewayFailed,

    /// <summary>The SMTP server did not answer, so the email may or may not have gone out. It is not resent under the same key.</summary>
    GatewayUnconfirmed,

    /// <summary>A request with the same key is still being sent.</summary>
    InProgress,

    /// <summary>The key was already used for a different kind of email about another report.</summary>
    KeyReused,
}

/// <param name="Replayed">True when a retry found the email already sent and nothing new went out.</param>
public sealed record EmailSendResult(
    EmailSendOutcome Outcome,
    int RecipientCount = 0,
    bool Replayed = false,
    IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>What the API returns when the email was sent (or a retry found it already sent).</summary>
public sealed record EmailSentDto(string Status, int RecipientCount, bool Replayed);

/// <summary>One email about a report, newest first in <see cref="IReportEmailService.HistoryAsync"/>. Never the addresses or body.</summary>
/// <param name="Kind">"R47" for a manual send, otherwise the workflow event (e.g. ReportApproved).</param>
/// <param name="Status">Sending | Sent | Failed | Unconfirmed.</param>
public sealed record ReportEmailDeliveryDto(
    long Id,
    string Kind,
    string Status,
    int RecipientCount,
    string SentByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>A server-composed email about a report; the R47 PDF is attached by the service.</summary>
/// <param name="Kind">Stored with the delivery: "R47" or the workflow event name.</param>
public sealed record ReportEmailContent(string Kind, string Subject, string Body);

public interface IReportEmailService
{
    /// <summary>
    /// Manual "Email R47". Never throws for an expected failure; every case comes back as an outcome.
    /// Once the SMTP call starts it runs to completion even if the caller's request is aborted.
    /// </summary>
    Task<EmailSendResult> SendR47Async(Guid reportId, SendReportEmailRequest request, CancellationToken ct = default);

    /// <summary>
    /// Server-initiated email about a workflow event: sent to the claimant and the report's Level 1 and
    /// Level 2 signatories with the R47 attached, as the signed-in user, at most once per key.
    /// </summary>
    Task<EmailSendResult> SendAboutReportAsync(Guid reportId, ReportEmailContent content, string idempotencyKey, CancellationToken ct = default);

    /// <summary>The latest emails about the report (the "sent status"), newest first.</summary>
    Task<IReadOnlyList<ReportEmailDeliveryDto>> HistoryAsync(Guid reportId, CancellationToken ct = default);
}

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>Who an email is from: the signed-in user's name and address on file.</summary>
public sealed record EmailSender(string Name, string Address);

/// <summary>One message for the SMTP server. Never log <see cref="To"/>, <see cref="Body"/> or the attachment.</summary>
/// <param name="From">The signed-in user, sent on behalf of by the configured mailbox; null sends from the mailbox itself.</param>
public sealed record EmailMessage(
    IReadOnlyList<string> To,
    string Subject,
    string Body,
    IReadOnlyList<EmailAttachment> Attachments,
    EmailSender? From = null);

public enum EmailGatewayStatus
{
    /// <summary>The SMTP server accepted the message for delivery.</summary>
    Accepted,

    /// <summary>The server refused the message or the login; nothing was sent.</summary>
    Rejected,

    /// <summary>No answer or the connection dropped mid-send; the message may or may not have been sent.</summary>
    Unreachable,
}

public sealed record EmailGatewayResult(EmailGatewayStatus Status, int? SmtpStatusCode = null);

/// <summary>The outbound mail server (SMTP through MailKit).</summary>
public interface IEmailGateway
{
    /// <summary>False when the SMTP host or sender address is not set up on the server.</summary>
    bool IsConfigured { get; }

    Task<EmailGatewayResult> SendAsync(EmailMessage message, CancellationToken ct = default);
}
