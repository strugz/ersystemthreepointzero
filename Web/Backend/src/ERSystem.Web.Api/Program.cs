using System.Threading.RateLimiting;
using ERSystem.Web.Api.Configuration;
using ERSystem.Web.Api.Middleware;
using ERSystem.Web.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "ER-XSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "ER-Web-Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Manager", policy => policy.RequireRole("Manager"))
    .AddPolicy("Finance", policy => policy.RequireRole("Finance"));
builder.Services.AddRateLimiter(options => options.AddPolicy("login", context =>
    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ =>
        new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
builder.Services.AddWebInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AntiforgeryMiddleware>();
app.UseAuthorization();
app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ER System Web API v1");
        options.UseRequestInterceptor(
            "function (request) {" +
            "request.credentials = 'include';" +
            "const method = (request.method || 'GET').toUpperCase();" +
            "const safeMethods = ['GET', 'HEAD', 'OPTIONS', 'TRACE'];" +
            "if (!request.url.includes('/api/') || safeMethods.includes(method)) return request;" +
            "return fetch('/api/auth/antiforgery', { credentials: 'include' })" +
            ".then(function (response) {" +
            "if (!response.ok) throw new Error('Unable to initialize the antiforgery token.');" +
            "return response.json();" +
            "})" +
            ".then(function (body) {" +
            "request.headers = request.headers || {};" +
            "request.headers['X-CSRF-TOKEN'] = body.token;" +
            "return request;" +
            "});" +
            "}");
    });
}
app.MapHealthChecks("/health");
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;
