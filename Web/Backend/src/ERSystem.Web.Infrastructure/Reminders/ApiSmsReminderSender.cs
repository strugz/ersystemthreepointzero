using System.Net;
using System.Text;
using ERSystem.Web.Application.Features.ApprovalReminders;
using Microsoft.Extensions.Logging;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class ApiSmsReminderSender(
    HttpClient httpClient,
    ILogger<ApiSmsReminderSender> logger)
    : ISmsReminderSender
{
    public async Task<ReminderSendResult> SendAsync(
        string receiverUsername,
        string senderUsername,
        string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(receiverUsername))
        {
            return Failed("SMS_API_RECEIVER_MISSING");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Failed("SMS_API_MESSAGE_MISSING");
        }

        if (string.IsNullOrWhiteSpace(senderUsername))
        {
            return Failed("SMS_API_SENDER_MISSING");
        }

        // The legacy endpoint substitutes GETPOST values without URL-decoding them.
        // Send its expected raw form body so spaces and punctuation reach the SMS unchanged.
        var requestBody =
            $"RECEIVER={NormalizeFormValue(receiverUsername)}&" +
            $"SENDER={NormalizeFormValue(senderUsername)}&" +
            $"MESSAGE={NormalizeFormValue(message)}";
        using var payload = new StringContent(
            requestBody,
            Encoding.UTF8,
            "application/x-www-form-urlencoded");

        try
        {
            using var response = await httpClient.PostAsync(string.Empty, payload, cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return ReminderSendResult.Success;
            }

            logger.LogWarning(
                "Approval reminder SMS API returned non-success status {StatusCode}",
                (int)response.StatusCode);
            return ReminderSendResult.Failed("SMS_API_HTTP_STATUS");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failed("SMS_API_TIMEOUT");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return Failed("SMS_API_CONNECTION_FAILED");
        }
        catch (Exception)
        {
            return Failed("SMS_API_SEND_FAILED");
        }
    }

    private ReminderSendResult Failed(string failureCode)
    {
        logger.LogWarning("Approval reminder SMS delivery failed with code {FailureCode}", failureCode);
        return ReminderSendResult.Failed(failureCode);
    }

    private static string NormalizeFormValue(string value) =>
        value.Trim()
            .Replace('&', ' ')
            .Replace('=', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ');
}
