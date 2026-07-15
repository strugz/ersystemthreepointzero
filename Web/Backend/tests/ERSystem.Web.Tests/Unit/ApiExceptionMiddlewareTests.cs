using System.Text.Json;
using ERSystem.Web.Api.Middleware;
using ERSystem.Web.Application.Common;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ERSystem.Web.Tests.Unit;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task Antiforgery_failure_has_a_stable_machine_readable_code()
    {
        using var document = await InvokeAsync(new AntiforgeryValidationException("The antiforgery token is invalid."));

        Assert.Equal(StatusCodes.Status400BadRequest, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("antiforgery_validation_failed", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Ordinary_validation_failure_does_not_have_an_antiforgery_code()
    {
        using var document = await InvokeAsync(new ValidationException("The request is invalid."));

        Assert.Equal(StatusCodes.Status400BadRequest, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("code", out _));
    }

    private static async Task<JsonDocument> InvokeAsync(Exception exception)
    {
        var middleware = new ApiExceptionMiddleware(_ => throw exception, NullLogger<ApiExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
