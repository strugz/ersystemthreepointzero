using ERSystem.Web.Application.Features.ApprovalReminders;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Utils;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class SmtpReminderSender(
    IOptions<SmtpReminderOptions> options,
    ILogger<SmtpReminderSender> logger) : IEmailReminderSender
{
    public async Task<ReminderSendResult> SendAsync(
        string recipientAddress,
        ReminderEmail message,
        CancellationToken cancellationToken)
    {
        try
        {
            var smtp = options.Value;
            var mail = new MimeMessage
            {
                MessageId = MimeUtils.GenerateMessageId(),
                Subject = message.Subject,
                Body = new TextPart("plain") { Text = message.Body }
            };
            mail.From.Add(new MailboxAddress(smtp.SenderDisplayName, smtp.SenderAddress));
            mail.To.Add(MailboxAddress.Parse(recipientAddress));

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port, GetSocketOptions(smtp.TlsMode), cancellationToken);
            if (!string.IsNullOrWhiteSpace(smtp.Username))
            {
                await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
            }

            await client.SendAsync(mail, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return ReminderSendResult.Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ParseException)
        {
            return Failed("EMAIL_ADDRESS_INVALID");
        }
        catch (AuthenticationException)
        {
            return Failed("SMTP_AUTHENTICATION_FAILED");
        }
        catch (SmtpCommandException)
        {
            return Failed("SMTP_COMMAND_FAILED");
        }
        catch (IOException)
        {
            return Failed("SMTP_CONNECTION_FAILED");
        }
        catch (Exception)
        {
            return Failed("SMTP_SEND_FAILED");
        }
    }

    private ReminderSendResult Failed(string failureCode)
    {
        logger.LogWarning("Approval reminder email delivery failed with code {FailureCode}", failureCode);
        return ReminderSendResult.Failed(failureCode);
    }

    private static SecureSocketOptions GetSocketOptions(string tlsMode) => tlsMode.Trim().ToUpperInvariant() switch
    {
        "NONE" => SecureSocketOptions.None,
        "SSLONCONNECT" => SecureSocketOptions.SslOnConnect,
        "STARTTLS" => SecureSocketOptions.StartTls,
        _ => throw new InvalidOperationException("Smtp:TlsMode must be None, SslOnConnect, or StartTls.")
    };
}
