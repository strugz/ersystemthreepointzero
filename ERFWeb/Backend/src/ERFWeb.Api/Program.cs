using System.Text.Json.Serialization;
using ERFWeb.Api.Middleware;
using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Infrastructure;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Notifications;
using ERFWeb.Infrastructure.Reporting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------
// Configuration policy: appsettings.json is the single source of truth.
// Only appsettings.json and appsettings.{Environment}.json are read. Environment
// variables, user secrets, and hard-coded fallbacks are deliberately NOT used, so
// every runtime setting is visible in those two files. Missing keys fail fast.
// ---------------------------------------------------------------------------------
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var config = builder.Configuration;

static string Required(IConfiguration configuration, string key) =>
    configuration[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Configuration key '{key}' is missing. Set it in appsettings.json.");

var connectionString = Required(config, "ConnectionStrings:Postgres");
var sessionCookieName = Required(config, "Authentication:SessionCookieName");
var sessionHours = int.Parse(Required(config, "Authentication:SessionHours"));
var slidingExpiration = bool.Parse(Required(config, "Authentication:SlidingExpiration"));
var antiforgeryCookieName = Required(config, "Authentication:AntiforgeryCookieName");
var antiforgeryHeaderName = Required(config, "Authentication:AntiforgeryHeaderName");
var swaggerEnabled = bool.Parse(Required(config, "Swagger:Enabled"));
var corsOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (corsOrigins.Length == 0)
{
    throw new InvalidOperationException("Configuration key 'Cors:AllowedOrigins' must list at least one origin in appsettings.json.");
}

// Attachment limits are required keys too: they decide what the server accepts on upload.
var attachmentMaxMb = int.Parse(Required(config, "Attachments:MaxFileSizeMb"));
_ = int.Parse(Required(config, "Attachments:MaxFilesPerReport"));
if ((config.GetSection("Attachments:AllowedContentTypes").Get<string[]>() ?? []).Length == 0)
{
    throw new InvalidOperationException("Configuration key 'Attachments:AllowedContentTypes' must list at least one MIME type in appsettings.json.");
}

builder.Services.AddErfWebInfrastructure(connectionString);
builder.Services.Configure<R47ReportOptions>(config.GetSection(R47ReportOptions.SectionName));
builder.Services.Configure<AttachmentOptions>(config.GetSection(AttachmentOptions.SectionName));

// ERF reference number series (ERF-2026-000142): prefix and padding are business settings, so they are required keys.
_ = Required(config, "ReferenceNumber:Prefix");
_ = int.Parse(Required(config, "ReferenceNumber:Digits"));
builder.Services.Configure<ReferenceNumberOptions>(config.GetSection(ReferenceNumberOptions.SectionName));

// Lasius SMS gateway: deliberately optional. Without Lasius:Url and Lasius:ApiKey the API still starts and
// the SMS endpoint answers "SMS is not configured" (503) rather than failing the whole application.
builder.Services.Configure<LasiusOptions>(config.GetSection(LasiusOptions.SectionName));

// SMTP for report email: optional in the same way. Without Smtp:Host and Smtp:FromAddress the API starts,
// "Email R47" answers "Email is not configured" (503) and workflow emails are skipped (logged).
builder.Services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.SectionName));

// Keep Kestrel/form limits in step with the configured attachment size (plus headroom for the multipart envelope).
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = (attachmentMaxMb + 1) * 1024L * 1024L);
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = (attachmentMaxMb + 1) * 1024L * 1024L);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = sessionCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = slidingExpiration;
        options.ExpireTimeSpan = TimeSpan.FromHours(sessionHours);
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

builder.Services.AddAuthorization(Policies.Register);

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = antiforgeryHeaderName;
    options.Cookie.Name = antiforgeryCookieName;
});

const string SpaCorsPolicy = "spa";
builder.Services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy =>
    policy.WithOrigins(corsOrigins)
        .AllowCredentials()
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Convenience only: "open in browser" from the IDE lands on Swagger instead of a blank root.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseCors(SpaCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AntiforgeryMiddleware>();

app.MapControllers();

app.Run();

/// <summary>Exposed so the test host (WebApplicationFactory) and EF design-time tooling can bootstrap the API.</summary>
public partial class Program;
