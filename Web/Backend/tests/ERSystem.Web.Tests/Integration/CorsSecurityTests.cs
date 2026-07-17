using System.Net;
using ERSystem.Web.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ERSystem.Web.Tests.Integration;

public sealed class CorsSecurityTests : IClassFixture<CorsWebApplicationFactory>
{
    private const string AllowedOrigin = "https://er-system-web-client.onrender.com";
    private readonly CorsWebApplicationFactory factory;

    public CorsSecurityTests(CorsWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Allowed_origin_receives_antiforgery_cookie_and_credential_headers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/antiforgery");
        request.Headers.Add("Origin", AllowedOrigin);

        using var response = await CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Allowed_origin_login_preflight_accepts_required_method_and_headers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-csrf-token");

        using var response = await CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
        Assert.Contains("POST", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Methods")), StringComparison.OrdinalIgnoreCase);
        var allowedHeaders = Assert.Single(response.Headers.GetValues("Access-Control-Allow-Headers"));
        Assert.Contains("content-type", allowedHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("x-csrf-token", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Untrusted_origin_receives_no_cors_permission_headers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/antiforgery");
        request.Headers.Add("Origin", "https://untrusted.example");

        using var response = await CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public void Authentication_cookie_remains_secure_and_httponly_with_samesite_none()
    {
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.None, options.Cookie.SameSite);
    }

    [Fact]
    public void Antiforgery_cookie_remains_secure_and_httponly_with_samesite_none()
    {
        var options = factory.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.None, options.Cookie.SameSite);
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });
}

public sealed class CorsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ErDatabase",
            "Server=localhost;Database=ERSystemCorsTests;Integrated Security=true;TrustServerCertificate=true;");
        builder.UseSetting("LegacyAuthentication:EncryptionKey", "integration-test-key");
        builder.UseSetting("Cors:AllowedOrigins:0", "https://er-system-web-client.onrender.com");
        builder.ConfigureServices(services =>
        {
            var validator = services.SingleOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(DatabaseCompatibilityValidator));
            if (validator is not null) services.Remove(validator);
        });
    }
}
