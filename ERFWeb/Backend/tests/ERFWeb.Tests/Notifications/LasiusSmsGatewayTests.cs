using System.Net;
using System.Text.Json;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ERFWeb.Tests.Notifications;

public class LasiusSmsGatewayTests
{
    private static readonly LasiusOptions Configured = new()
    {
        Url = "https://lasius.example.test/api/sms",
        ApiKey = "test-key",
        ApiKeyHeaderName = "X-API-Key",
        Application = "ERMS",
    };

    private static readonly SmsGatewayMessage Message =
        new("Juan Dela Cruz", ["09171234567", "+639181234567"], "Please submit your receipts.");

    [Fact]
    public async Task SendAsync_PostsTheLasiusPayloadWithTheKeyInAHeader()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var gateway = Create(handler, Configured);

        var result = await gateway.SendAsync(Message);

        Assert.Equal(SmsGatewayStatus.Accepted, result.Status);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(Configured.Url, handler.Uri?.ToString());
        Assert.Equal("test-key", handler.ApiKey);

        using var body = JsonDocument.Parse(handler.Body!);
        var root = body.RootElement;
        Assert.Equal("Juan Dela Cruz", root.GetProperty("sender").GetString());
        Assert.Equal(["09171234567", "+639181234567"], root.GetProperty("recipients").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal("Please submit your receipts.", root.GetProperty("message").GetString());
        Assert.Equal("ERMS", root.GetProperty("application").GetString());
        Assert.DoesNotContain("test-key", handler.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendAsync_ReportsAnErrorAnswerAsRejected(HttpStatusCode status)
    {
        var result = await Create(new RecordingHandler(status), Configured).SendAsync(Message);

        Assert.Equal(SmsGatewayStatus.Rejected, result.Status);
        Assert.Equal((int)status, result.HttpStatus);
    }

    [Fact]
    public async Task SendAsync_ReportsNoAnswerAsUnreachable()
    {
        var result = await Create(new RecordingHandler(throws: new HttpRequestException("connection refused")), Configured).SendAsync(Message);

        Assert.Equal(SmsGatewayStatus.Unreachable, result.Status);
        Assert.Null(result.HttpStatus);
    }

    [Theory]
    [InlineData(null, "key")]
    [InlineData("", "key")]
    [InlineData("not a url", "key")]
    [InlineData("ftp://lasius.example.test/sms", "key")]
    [InlineData("https://lasius.example.test/api/sms", null)]
    [InlineData("https://lasius.example.test/api/sms", " ")]
    public void IsConfigured_NeedsAnHttpUrlAndAKey(string? url, string? apiKey) =>
        Assert.False(Create(new RecordingHandler(HttpStatusCode.OK), new LasiusOptions { Url = url, ApiKey = apiKey }).IsConfigured);

    [Fact]
    public void IsConfigured_IsTrueWithUrlAndKey() =>
        Assert.True(Create(new RecordingHandler(HttpStatusCode.OK), Configured).IsConfigured);

    private static LasiusSmsGateway Create(HttpMessageHandler handler, LasiusOptions options) =>
        new(new HttpClient(handler), Options.Create(options), NullLogger<LasiusSmsGateway>.Instance);

    private sealed class RecordingHandler(HttpStatusCode status = HttpStatusCode.OK, Exception? throws = null) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? ApiKey { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            ApiKey = request.Headers.TryGetValues("X-API-Key", out var values) ? values.Single() : null;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (throws is not null)
            {
                throw throws;
            }

            return new HttpResponseMessage(status);
        }
    }
}
