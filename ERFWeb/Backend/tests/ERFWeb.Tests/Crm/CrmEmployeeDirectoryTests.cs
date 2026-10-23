using System.Net;
using System.Text;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using ERFWeb.Infrastructure.Crm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ERFWeb.Tests.Crm;

public class CrmEmployeeDirectoryTests
{
    private static readonly CrmOptions Configured = new()
    {
        BaseUrl = "https://crm.example.test/api2/erf/v1/",
        ApiKey = "test-key",
    };

    private const string Employee = """
        {"employeeId":"CNT-001","firstName":"Juan","middleName":null,"lastName":"Dela Cruz","nickname":"JDC",
         "fullName":"Juan Dela Cruz","department":"PMDLUZON","section":"PS","workflows":["PS"],"isActive":true}
        """;

    [Fact]
    public async Task SearchAsync_SendsTheQueryWithTheKeyInAHeaderAndMapsThePage()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, $$"""{"page":1,"pageSize":20,"totalCount":1,"items":[{{Employee}}]}""");

        var result = await Create(handler, Configured).SearchAsync(new CrmEmployeeSearchQuery { Search = " juan cruz ", PageSize = 20 });

        Assert.Equal("https://crm.example.test/api2/erf/v1/employees?search=juan%20cruz&includeInactive=false&page=1&pageSize=20", handler.Uri?.AbsoluteUri);
        Assert.Equal("test-key", handler.ApiKey);
        Assert.Equal(1, result.Total);
        var e = Assert.Single(result.Items);
        Assert.Equal("CNT-001", e.EmployeeId);
        Assert.Equal("Juan Dela Cruz", e.FullName);
        Assert.Equal(["PS"], e.Workflows);
        Assert.True(e.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("j")]
    [InlineData("  a ")]
    public async Task SearchAsync_RefusesAShortSearchWithoutCallingTheCrm(string? search)
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}");

        await Assert.ThrowsAsync<ValidationException>(() => Create(handler, Configured).SearchAsync(new CrmEmployeeSearchQuery { Search = search }));
        Assert.Null(handler.Uri);
    }

    [Fact]
    public async Task GetAsync_ReturnsTheEmployee()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, Employee);

        var employee = await Create(handler, Configured).GetAsync("CNT-001");

        Assert.Equal("https://crm.example.test/api2/erf/v1/employees/CNT-001", handler.Uri?.ToString());
        Assert.Equal("JDC", employee?.Nickname);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenTheCrmHasNoSuchId() =>
        Assert.Null(await Create(new RecordingHandler(HttpStatusCode.NotFound, """{"status":404}"""), Configured).GetAsync("CNT-999"));

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetAsync_ReportsACrmFailureAsUnavailable(HttpStatusCode status)
    {
        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => Create(new RecordingHandler(status, "{}"), Configured).GetAsync("CNT-001"));
        Assert.Equal("crm_unavailable", ex.Code);
    }

    [Fact]
    public async Task GetAsync_ReportsNoAnswerAsUnavailable()
    {
        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            Create(new RecordingHandler(throws: new HttpRequestException("connection refused")), Configured).GetAsync("CNT-001"));
        Assert.Equal("crm_unavailable", ex.Code);
    }

    [Fact]
    public async Task GetAsync_ReportsAnUnreadableBodyAsUnavailable() =>
        await Assert.ThrowsAsync<ServiceUnavailableException>(() => Create(new RecordingHandler(HttpStatusCode.OK, "<html>"), Configured).GetAsync("CNT-001"));

    [Fact]
    public async Task Calls_AreRefusedWhenNotConfigured()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, Employee);

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => Create(handler, new CrmOptions()).GetAsync("CNT-001"));
        Assert.Equal("crm_not_configured", ex.Code);
        Assert.Null(handler.Uri);
    }

    [Theory]
    [InlineData(null, "key")]
    [InlineData("not a url", "key")]
    [InlineData("ftp://crm.example.test/api", "key")]
    [InlineData("https://crm.example.test/api2/erf/v1", " ")]
    public void IsConfigured_NeedsAnHttpUrlAndAKey(string? url, string? apiKey) =>
        Assert.False(Create(new RecordingHandler(HttpStatusCode.OK, "{}"), new CrmOptions { BaseUrl = url, ApiKey = apiKey }).IsConfigured);

    private static CrmEmployeeDirectory Create(HttpMessageHandler handler, CrmOptions options) =>
        new(new HttpClient(handler), Options.Create(options), NullLogger<CrmEmployeeDirectory>.Instance);

    private sealed class RecordingHandler(HttpStatusCode status = HttpStatusCode.OK, string body = "", Exception? throws = null) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public string? ApiKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            ApiKey = request.Headers.TryGetValues("X-Api-Key", out var values) ? values.Single() : null;
            if (throws is not null)
            {
                throw throws;
            }

            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
