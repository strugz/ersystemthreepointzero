using System.Net;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ERFWeb.Api.Background;
using ERFWeb.Api.Middleware;
using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Infrastructure;
using ERFWeb.Infrastructure.Crm;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Notifications;
using ERFWeb.Infrastructure.Reporting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
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

// CRM employee lookup: optional in the same way. Without Crm:BaseUrl and Crm:ApiKey the API starts and the
// Add User CRM search answers "CRM lookup is not configured" (503); accounts can still be saved unlinked.
builder.Services.Configure<CrmOptions>(config.GetSection(CrmOptions.SectionName));

// Daily SLA reminders: off unless Reminders:Enabled is true, so only the server sends them.
builder.Services.Configure<ReminderOptions>(config.GetSection(ReminderOptions.SectionName));
builder.Services.AddHostedService<ApprovalReminderJob>();

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

// Behind nginx the client's address and scheme arrive in X-Forwarded-For / X-Forwarded-Proto. They are
// trusted only from the proxies in ReverseProxy:KnownProxies (nginx's address as Kestrel sees it); with
// none listed they are ignored, every request appears to come from nginx, and the per-address rate
// limits below then apply to all users together (their defaults allow for that).
var knownProxies = config.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var proxy in knownProxies)
    {
        options.KnownProxies.Add(IPAddress.TryParse(proxy, out var address)
            ? address
            : throw new InvalidOperationException($"ReverseProxy:KnownProxies has '{proxy}', which is not an IP address."));
    }
});

// Rate limits per client address (fixed one-minute windows): sign-in and password change are tight, to
// slow password guessing across usernames (the lockout covers guessing one username); everything else
// is generous and only stops runaway clients. 429 with Retry-After when exceeded.
var signInPerMinute = config.GetValue("RateLimiting:SignInPerMinute", 30);
var apiPerMinute = config.GetValue("RateLimiting:ApiPerMinute", 1200);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(ClientAddress(context), _ => PerMinute(apiPerMinute)));
    options.AddPolicy(RateLimitPolicies.SignIn, context =>
        RateLimitPartition.GetFixedWindowLimiter(ClientAddress(context), _ => PerMinute(signInPerMinute)));
    options.OnRejected = async (rejected, token) =>
    {
        var http = rejected.HttpContext;
        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests. Please wait a minute and try again.",
            Instance = http.Request.Path,
        };
        problem.Extensions["code"] = "rate_limited";
        if (http.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        await http.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", token);
    };
});

static string ClientAddress(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static FixedWindowRateLimiterOptions PerMinute(int permits) =>
    new() { PermitLimit = Math.Max(1, permits), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 };

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseForwardedHeaders();

// TLS ends at nginx (https://sr.mdmpi.com.ph/erf), which reaches Kestrel over plain HTTP. Secure cookies
// require an HTTPS request (antiforgery throws otherwise), so with cross-site cookies a request that
// X-Forwarded-Proto did not already mark as HTTPS is treated as the HTTPS request the browser made.
if (crossSiteCookies)
{
    app.Use((context, next) =>
    {
        if (!context.Request.IsHttps)
        {
            context.Request.Scheme = Uri.UriSchemeHttps;
        }

        return next(context);
    });
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>((IReadOnlyList<string>)corsOrigins);
app.UseMiddleware<ApiExceptionMiddleware>();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Convenience only: "open in browser" from the IDE lands on Swagger instead of a blank root.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseCors(SpaCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseAuthorization();
app.UseMiddleware<AntiforgeryMiddleware>();

app.MapControllers();

app.Run();

/// <summary>Exposed so the test host (WebApplicationFactory) and EF design-time tooling can bootstrap the API.</summary>
public partial class Program;
