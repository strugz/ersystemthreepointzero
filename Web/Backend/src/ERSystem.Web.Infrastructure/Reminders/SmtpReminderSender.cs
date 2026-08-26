using ERSystem.Web.Application.Features.ApprovalReminders;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Utils;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class SmtpReminderSender(
    IEmployeeSmtpAccountProvider accountProvider,
    IOptions<SmtpReminderOptions> options,
    ILogger<SmtpReminderSender> logger) : IEmailReminderSender
{
    public async Task<ReminderSendResult> SendAsync(
        int employeeUserId,
        ReminderAudience audience,
        ReminderEmail message,
        CancellationToken cancellationToken)
    {
        var resolution = await accountProvider.ResolveAsync(employeeUserId, cancellationToken);
        if (resolution.Failure is not null)
        {
            return LogFailure(resolution.Failure);
        }

        var account = resolution.Account
            ?? throw new InvalidOperationException("A successful SMTP account resolution must contain an account.");
        if (!MailboxAddress.TryParse(account.SenderAddress, out var sender))
        {
            return Failed("SMTP_SENDER_ADDRESS_INVALID");
        }

        MailboxAddress recipient;
        if (audience == ReminderAudience.Employee)
        {
            // The desktop "Reminder Email" field (tbUserRegistration.NotificationEmail) wins over
            // the employee's own mailbox when configured; an unparseable configured value is a
            // failure rather than a silent fallback so the misconfiguration surfaces in the ledger.
            if (!string.IsNullOrWhiteSpace(account.NotificationEmailAddress))
            {
                if (!MailboxAddress.TryParse(account.NotificationEmailAddress, out var notificationRecipient) ||
                    notificationRecipient is null)
                {
                    return Failed("NOTIFICATION_EMAIL_INVALID");
                }

                recipient = notificationRecipient;
            }
            else
            {
                recipient = sender;
            }
        }
        else if (audience == ReminderAudience.Manager)
        {
            if (string.IsNullOrWhiteSpace(account.ManagerEmailAddress))
            {
                return Skipped("MANAGER_EMAIL_ADDRESS_MISSING");
            }

            if (!MailboxAddress.TryParse(account.ManagerEmailAddress, out var managerRecipient) ||
                managerRecipient is null)
            {
                return Failed("MANAGER_EMAIL_ADDRESS_INVALID");
            }

            recipient = managerRecipient;
        }
        else
        {
            return Failed("EMAIL_AUDIENCE_INVALID");
        }

        try
        {
            var smtp = options.Value;
            var mail = new MimeMessage
            {
                MessageId = MimeUtils.GenerateMessageId(),
                Subject = message.Subject,
                Body = new TextPart("plain") { Text = message.Body }
            };
            mail.From.Add(new MailboxAddress(smtp.SenderDisplayName, sender.Address));
            mail.To.Add(recipient);

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port, GetSocketOptions(smtp.TlsMode), cancellationToken);
            await client.AuthenticateAsync(account.SenderAddress, account.Password, cancellationToken);

            await client.SendAsync(mail, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return ReminderSendResult.Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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

    private ReminderSendResult Skipped(string failureCode)
    {
        logger.LogInformation("Approval reminder email delivery skipped with code {FailureCode}", failureCode);
        return ReminderSendResult.Skipped(failureCode);
    }

    private ReminderSendResult LogFailure(ReminderSendResult result)
    {
        if (result.Outcome == ReminderSendOutcome.Skipped)
        {
            logger.LogInformation(
                "Approval reminder email delivery skipped with code {FailureCode}",
                result.FailureCode);
        }
        else
        {
            logger.LogWarning(
                "Approval reminder email delivery failed with code {FailureCode}",
                result.FailureCode);
        }

        return result;
    }

    private static SecureSocketOptions GetSocketOptions(string tlsMode) => tlsMode.Trim().ToUpperInvariant() switch
    {
        "NONE" => SecureSocketOptions.None,
        "SSLONCONNECT" => SecureSocketOptions.SslOnConnect,
        "STARTTLS" => SecureSocketOptions.StartTls,
        _ => throw new InvalidOperationException("Smtp:TlsMode must be None, SslOnConnect, or StartTls.")
    };
}
