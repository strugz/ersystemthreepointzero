using System.Net.Sockets;
using ERFWeb.Application.Features.Notifications;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Sends one message per call over SMTP with MailKit. Logs only the outcome, SMTP status code and
/// recipient count: never the addresses, subject, body, attachment, or the server's reply text.
/// </summary>
public sealed class MailKitEmailGateway(IOptions<SmtpOptions> options, ILogger<MailKitEmailGateway> logger) : IEmailGateway
{
    private readonly SmtpOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<EmailGatewayResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsConfigured)
        {
            throw new InvalidOperationException("SMTP is not configured.");
        }

        using var mime = Compose(message);
        using var client = new SmtpClient { Timeout = Math.Max(1, _options.TimeoutSeconds) * 1000 };
        var security = Enum.Parse<SecureSocketOptions>(_options.Security, ignoreCase: true);

        // Up to "connected and logged in" nothing has been handed over, so any failure there is a
        // definite "not sent". Once SendAsync starts, a dropped line leaves the outcome unknown.
        try
        {
            await client.ConnectAsync(_options.Host!, _options.Port, security, ct);
            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, ct);
            }
        }
        catch (Exception ex) when (ex is SocketException or IOException or SslHandshakeException or AuthenticationException
                                       or SmtpCommandException or SmtpProtocolException or TimeoutException or OperationCanceledException)
        {
            logger.LogWarning("SMTP connect or login failed ({Error}); the email was not sent.", ex.GetType().Name);
            return new EmailGatewayResult(EmailGatewayStatus.Rejected, (ex as SmtpCommandException)?.StatusCode is { } code ? (int)code : null);
        }

        try
        {
            await client.SendAsync(mime, ct);
            logger.LogInformation("SMTP accepted an email to {RecipientCount} recipient(s).", message.To.Count);
            await DisconnectQuietlyAsync(client);
            return new EmailGatewayResult(EmailGatewayStatus.Accepted);
        }
        catch (SmtpCommandException ex)
        {
            // The server answered with a refusal (bad recipient, message too large, relay denied).
            logger.LogWarning("SMTP refused an email to {RecipientCount} recipient(s) (SMTP {Status}).", message.To.Count, (int)ex.StatusCode);
            await DisconnectQuietlyAsync(client);
            return new EmailGatewayResult(EmailGatewayStatus.Rejected, (int)ex.StatusCode);
        }
        catch (Exception ex) when (ex is SocketException or IOException or SmtpProtocolException or ServiceNotConnectedException
                                       or TimeoutException or OperationCanceledException)
        {
            logger.LogWarning("SMTP did not confirm an email ({Error}); delivery is unconfirmed.", ex.GetType().Name);
            return new EmailGatewayResult(EmailGatewayStatus.Unreachable);
        }
    }

    private MimeMessage Compose(EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress!));
        foreach (var address in message.To)
        {
            mime.To.Add(MailboxAddress.Parse(address));
        }

        mime.Subject = message.Subject;
        var body = new BodyBuilder { TextBody = message.Body };
        foreach (var attachment in message.Attachments)
        {
            body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }

        mime.Body = body.ToMessageBody();
        return mime;
    }

    private async Task DisconnectQuietlyAsync(SmtpClient client)
    {
        try
        {
            await client.DisconnectAsync(quit: true, CancellationToken.None);
        }
        catch (Exception ex) when (ex is IOException or SocketException or SmtpProtocolException or SmtpCommandException or TimeoutException)
        {
            // The message is already accepted (or refused); a failed QUIT changes nothing.
            logger.LogDebug("SMTP disconnect failed ({Error}).", ex.GetType().Name);
        }
    }
}
