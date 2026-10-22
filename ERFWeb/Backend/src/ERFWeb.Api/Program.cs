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
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using Serilog.Extensions.Logging;

// On the server the API runs as a Windows Service, whose working directory is System32: the content root
// is the install folder, so appsettings.json and logs/ are found next to ERFWeb.Api.dll.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null,
});
builder.Host.UseWindowsService(options => options.ServiceName = "ERFWeb.Api");

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

// Daily rolling log files (erfweb-yyyyMMdd.log, 30 kept, 50 MB each at most) in Logging:File:Directory,
// default logs/ under the content root. Levels follow Logging:LogLevel, exactly as for the console.
var logDirectory = config["Logging:File:Directory"] is { Length: > 0 } configuredLogDirectory ? configuredLogDirectory : "logs";
if (!Path.IsPathRooted(logDirectory))
{
    logDirectory = Path.Combine(builder.Environment.ContentRootPath, logDirectory);
}

// Registered as a plain provider (not AddSerilog, which lets every level through) so the Logging:LogLevel
// filters apply to the file too.
builder.Logging.AddProvider(new SerilogLoggerProvider(
    new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.File(
            Path.Combine(logDirectory, "erfweb-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 50L * 1024 * 1024,
            rollOnFileSizeLimit: true,
            shared: true,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {CorrelationId}{NewLine}  {Message:lj}{NewLine}{Exception}")
        .CreateLogger(),
    dispose: true));

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
// Optional, default false. True when the SPA is served from another site than the API (e.g. Render's
// onrender.com calling sr.mdmpi.com.ph): the browser only sends the session and antiforgery cookies on
// those cross-site calls when they are SameSite=None and Secure. Safari blocks such cookies regardless.
var crossSiteCookies = config.GetValue("Authentication:CrossSiteCookies", false);
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
        options.Cookie.SameSite = crossSiteCookies ? SameSiteMode.None : SameSiteMode.Lax;
        options.Cookie.SecurePolicy = crossSiteCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
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
    if (crossSiteCookies)
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
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

// TLS ends at nginx (https://sr.mdmpi.com.ph/erf), which reaches Kestrel over plain HTTP. Secure cookies
// require an HTTPS request (antiforgery throws otherwise), so with cross-site cookies the request is
// treated as the HTTPS request the browser actually made.
if (crossSiteCookies)
{
    app.Use((context, next) =>
    {
        context.Request.Scheme = Uri.UriSchemeHttps;
        return next(context);
    });
}

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
