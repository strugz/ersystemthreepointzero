using System.Net;
using ERSystem.Web.Infrastructure.Reminders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ERSystem.Web.Tests.Unit;

public sealed class ApiSmsReminderSenderTests
{
    [Fact]
    public async Task Posts_finance_compatible_payload_and_accepts_http_200()
    {
        string? contentType = null;
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            contentType = request.Content!.Headers.ContentType?.MediaType;
            requestBody = await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sender = CreateSender(handler);

        var result = await sender.SendAsync("MCRUZ/JSMITH", "JSMITH", "Approval: reminder", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("application/x-www-form-urlencoded", contentType);
        Assert.Equal(
            "RECEIVER=MCRUZ/JSMITH&SENDER=JSMITH&MESSAGE=Approval: reminder",
            requestBody);
    }

    [Fact]
    public async Task Non_success_status_returns_stable_failure_code()
    {
        var sender = CreateSender(new StubHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest))));

        var result = await sender.SendAsync("MCRUZ", "JSMITH", "Approval reminder", CancellationToken.None);

        Assert.Equal("SMS_API_HTTP_STATUS", result.FailureCode);
    }

    [Fact]
    public async Task Timeout_returns_stable_failure_code()
    {
        var sender = CreateSender(new StubHttpMessageHandler(
            _ => throw new TaskCanceledException("Simulated timeout")));

        var result = await sender.SendAsync("MCRUZ", "JSMITH", "Approval reminder", CancellationToken.None);

        Assert.Equal("SMS_API_TIMEOUT", result.FailureCode);
    }

    [Fact]
    public async Task Connection_failure_returns_stable_failure_code()
    {
        var sender = CreateSender(new StubHttpMessageHandler(
            _ => throw new HttpRequestException("Simulated connection failure")));

        var result = await sender.SendAsync("MCRUZ", "JSMITH", "Approval reminder", CancellationToken.None);

        Assert.Equal("SMS_API_CONNECTION_FAILED", result.FailureCode);
    }

    private static ApiSmsReminderSender CreateSender(HttpMessageHandler handler)
    {
        var options = Options.Create(new ApprovalReminderOptions
        {
            SmsApiUrl = "https://mdmpi.com.ph/lasius/api_sendsms",
            SmsTimeoutSeconds = 30
        });
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.Value.SmsApiUrl),
            Timeout = TimeSpan.FromSeconds(options.Value.SmsTimeoutSeconds)
        };
        return new ApiSmsReminderSender(client, NullLogger<ApiSmsReminderSender>.Instance);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request);
    }
}
