using System.Net;
using System.Text;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using ERFWeb.Infrastructure.Crm;
using ERFWeb.Infrastructure.Expenses;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ERFWeb.Tests.Crm;

public class CrmActivityDirectoryTests
{
    private static readonly CrmOptions Configured = new()
    {
        BaseUrl = "https://crm.example.test/api2/erf/v1/",
        ApiKey = "test-key",
    };

    private const string ServiceReport = """
        {"activityId":"sr-2026-004512","activityType":"Preventive Maintenance","status":"Completed","workflow":"TSG",
         "activityDate":"2026-10-06","startAt":"2026-10-06T08:00:00+08:00","endAt":"2026-10-07T17:30:00+08:00",
         "subject":"Preventive Maintenance - XN-1000","serviceNumber":"SR-2026-004512",
         "customer":{"customerId":"C-1","name":" St. Luke's ","address":"BGC","branch":null,"city":null,"province":null},
         "instrument":{"name":"Sysmex XN-1000","serialNumber":"XN123"},
         "employee":{"employeeId":"CNT-001","name":"Juan Dela Cruz","role":"Lead"},
         "companions":[{"employeeId":"CNT-002","name":"Pedro Santos"}],
         "travel":{"origin":"MDMPI","destination":"St. Luke's","distanceKm":null,"isOutOfTown":null,"isOvernight":true},
         "lastModifiedAt":null}
        """;

    [Fact]
    public async Task ListAsync_SendsTheEmployeeAndPeriodAndMapsTheActivity()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, $$"""{"page":1,"pageSize":100,"totalCount":1,"items":[{{ServiceReport}}]}""");

        var result = await Create(handler, Configured).ListAsync(" CNT-001 ", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15), 1, 100);

        Assert.Equal("https://crm.example.test/api2/erf/v1/activities?employeeId=CNT-001&dateFrom=2026-10-01&dateTo=2026-10-15&status=Completed&page=1&pageSize=100", handler.Uri?.AbsoluteUri);
        Assert.Equal("test-key", handler.ApiKey);
        var a = Assert.Single(result!.Items);
        Assert.Equal("SR-2026-004512", a.ActivityId);
        Assert.Equal(new DateOnly(2026, 10, 6), a.ActivityDate);
        Assert.Equal("St. Luke's", a.CustomerName);
        Assert.Equal("Sysmex XN-1000", a.InstrumentName);
        Assert.Equal("XN123", a.SerialNumber);
        Assert.Equal("MDMPI", a.TravelOrigin);
        Assert.True(a.IsOvernight);
        Assert.True(a.IsCompleted);
        Assert.True(a.Involves("cnt-002"));
        Assert.False(a.Involves("CNT-003"));
    }

    [Fact]
    public async Task ListAsync_ReturnsNullWhenTheCrmHasNoSuchEmployee() =>
        Assert.Null(await Create(new RecordingHandler(HttpStatusCode.NotFound, """{"status":404}"""), Configured)
            .ListAsync("CNT-999", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), 1, 50));

    [Fact]
    public async Task ListAsync_TurnsARefusedRangeIntoAValidationError() =>
        await Assert.ThrowsAsync<ValidationException>(() => Create(new RecordingHandler(HttpStatusCode.BadRequest, """{"status":400}"""), Configured)
            .ListAsync("CNT-001", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), 1, 50));

    [Fact]
    public async Task ListAsync_SkipsItemsWithoutAnIdOrDate()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"page":1,"pageSize":50,"totalCount":2,"items":[{"activityId":null,"activityDate":"2026-10-06"},{"activityId":"ITI-9","activityDate":null}]}""");

        var result = await Create(handler, Configured).ListAsync("CNT-001", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15), 1, 50);

        Assert.Empty(result!.Items);
    }

    [Fact]
    public async Task GetAsync_ReturnsTheActivity()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, ServiceReport);

        var activity = await Create(handler, Configured).GetAsync("SR-2026-004512");

        Assert.Equal("https://crm.example.test/api2/erf/v1/activities/SR-2026-004512", handler.Uri?.ToString());
        Assert.Equal("CNT-001", activity?.Employee?.EmployeeId);
        Assert.Equal("Pedro Santos", Assert.Single(activity!.Companions).Name);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenTheCrmHasNoSuchId() =>
        Assert.Null(await Create(new RecordingHandler(HttpStatusCode.NotFound, """{"status":404}"""), Configured).GetAsync("SR-0"));

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetAsync_ReportsACrmFailureAsUnavailable(HttpStatusCode status)
    {
        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => Create(new RecordingHandler(status, "{}"), Configured).GetAsync("SR-1"));
        Assert.Equal("crm_unavailable", ex.Code);
    }

    [Fact]
    public async Task Calls_AreRefusedWhenNotConfigured()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, ServiceReport);

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => Create(handler, new CrmOptions()).GetAsync("SR-1"));
        Assert.Equal("crm_not_configured", ex.Code);
        Assert.Null(handler.Uri);
    }

    [Theory]
    [InlineData(" sr-12 ", "SR-12")]
    [InlineData("ITI-7", "ITI-7")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void Normalize_StoresOneSpelling(string? input, string? expected) =>
        Assert.Equal(expected, CrmActivityClaimCheck.Normalize(input));

    private static CrmActivityDirectory Create(HttpMessageHandler handler, CrmOptions options) =>
        new(new HttpClient(handler), Options.Create(options), NullLogger<CrmActivityDirectory>.Instance);

    private sealed class RecordingHandler(HttpStatusCode status = HttpStatusCode.OK, string body = "") : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public string? ApiKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            ApiKey = request.Headers.TryGetValues("X-Api-Key", out var values) ? values.Single() : null;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
