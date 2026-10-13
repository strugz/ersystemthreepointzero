using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ERFWeb.Application.Features.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Posts <c>{ sender, recipients, message, application }</c> to Lasius with the server-held API key in
/// a header. Logs only the HTTP status and recipient count: never the message, the numbers, the key,
/// or the response body (which may echo the message).
/// </summary>
public sealed class LasiusSmsGateway(HttpClient http, IOptions<LasiusOptions> options, ILogger<LasiusSmsGateway> logger) : ISmsGateway
{
    private readonly LasiusOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<SmsGatewayResult> SendAsync(SmsGatewayMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsConfigured)
        {
            throw new InvalidOperationException("The Lasius SMS gateway is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url)
        {
            Content = JsonContent.Create(new LasiusPayload(message.Sender, message.Recipients, message.Message, _options.Application)),
        };
        request.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, _options.ApiKey);

        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            var status = (int)response.StatusCode;
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Lasius accepted an SMS to {RecipientCount} recipient(s) (HTTP {Status}).", message.Recipients.Count, status);
                return new SmsGatewayResult(SmsGatewayStatus.Accepted, status);
            }

            logger.LogWarning("Lasius rejected an SMS to {RecipientCount} recipient(s) (HTTP {Status}).", message.Recipients.Count, status);
            return new SmsGatewayResult(SmsGatewayStatus.Rejected, status);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // A timeout or dropped connection: Lasius may have sent the SMS before the line went quiet.
            // Only the exception type is logged; its message can carry request details.
            logger.LogWarning("Lasius did not answer an SMS request ({Error}); delivery is unconfirmed.", ex.GetType().Name);
            return new SmsGatewayResult(SmsGatewayStatus.Unreachable);
        }
    }

    private sealed record LasiusPayload(
        [property: JsonPropertyName("sender")] string Sender,
        [property: JsonPropertyName("recipients")] IReadOnlyList<string> Recipients,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("application")] string Application);
}
