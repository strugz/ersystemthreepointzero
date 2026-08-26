using System.Net;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using ERSystem.Web.Application.Features.ApprovalReminders;
using Microsoft.Extensions.Logging;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class ApiSmsReminderSender(
    HttpClient httpClient,
    ILogger<ApiSmsReminderSender> logger)
    : ISmsReminderSender
{
    // Mirror the proven desktop client (ERSystem.AppServices SmsNotificationService): the
    // endpoint expects a JSON body with upper-case keys and a bare application/json content
    // type. Relaxed escaping keeps ordinary punctuation literal, as the desktop client sends it.
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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

        var requestBody = JsonSerializer.Serialize(
            new Dictionary<string, string>
            {
                ["RECEIVER"] = receiverUsername.Trim(),
                ["SENDER"] = senderUsername.Trim(),
                ["MESSAGE"] = message.Trim()
            },
            PayloadSerializerOptions);
        using var payload = new StringContent(requestBody);
        payload.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, (Uri?)null)
            {
                Content = payload
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            using var response = await httpClient.SendAsync(request, cancellationToken);
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
}
