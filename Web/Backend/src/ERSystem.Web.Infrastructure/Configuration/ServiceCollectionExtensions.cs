using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.Authentication;
using ERSystem.Web.Application.Features.ApprovalReminders;
using ERSystem.Web.Application.Features.FinanceReceipts;
using ERSystem.Web.Application.Features.ManagerApprovals;
using ERSystem.Web.Infrastructure.Authentication;
using ERSystem.Web.Infrastructure.Persistence;
using ERSystem.Web.Infrastructure.Reminders;
using ERSystem.Web.Infrastructure.Security;
using ERSystem.Web.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERSystem.Web.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ErDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:ErDatabase must be configured.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:ErDatabase must not be empty.");
        void ConfigureSql(DbContextOptionsBuilder options) => options.UseSqlServer(connectionString, sql =>
        {
            sql.UseCompatibilityLevel(100);
        });

        services.AddOptions<LegacyAuthenticationOptions>()
            .Bind(configuration.GetSection(LegacyAuthenticationOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.EncryptionKey),
                "LegacyAuthentication:EncryptionKey must be configured.")
            .ValidateOnStart();
        services.AddDbContextFactory<LegacyErDbContext>(ConfigureSql);
        services.AddDbContextFactory<WebWorkflowDbContext>(ConfigureSql);
        services.AddSingleton(new SqlConnectionStringBuilder(connectionString));
        services.AddHostedService<DatabaseCompatibilityValidator>();
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IRowVersionCodec, RowVersionCodec>();
        services.AddScoped<IWorkflowAuditWriter, WorkflowAuditWriter>();
        services.AddScoped<IReportAuthorizationService, ReportAuthorizationService>();
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        services.AddSingleton<LegacyPasswordCipher>();
        services.AddScoped<IAuthenticationService, LegacyAuthenticationService>();
        services.AddScoped<IManagerApprovalService, ManagerApprovalService>();
        services.AddScoped<IFinanceReceiptService, FinanceReceiptService>();
        return services;
    }

    public static IServiceCollection AddApprovalReminderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ErDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:ErDatabase must be configured.");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:ErDatabase must not be empty.");
        }

        services.AddOptions<ApprovalReminderOptions>()
            .Bind(configuration.GetSection(ApprovalReminderOptions.SectionName))
            .Validate(options => TimeOnly.TryParseExact(options.RunAtLocalTime, "HH:mm", out _),
                "ApprovalReminders:RunAtLocalTime must use HH:mm format.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.TimeZoneId),
                "ApprovalReminders:TimeZoneId must be configured.")
            .Validate(options => CanResolveTimeZone(options.TimeZoneId),
                "ApprovalReminders:TimeZoneId is not available on this server.")
            .Validate(options => options.InitialDelayDays > 0,
                "ApprovalReminders:InitialDelayDays must be greater than zero.")
            .Validate(options => options.ActivationPollIntervalSeconds is >= 10 and <= 3600,
                "ApprovalReminders:ActivationPollIntervalSeconds must be between 10 and 3600.")
            .Validate(options =>
                    Enum.TryParse<DayOfWeek>(options.ReminderDayOfWeek, true, out var reminderDay) &&
                    Enum.IsDefined(reminderDay),
                "ApprovalReminders:ReminderDayOfWeek must be a valid day of the week.")
            .Validate(options => !options.EmailEnabled || IsValidAbsoluteHttpUrl(options.ManagerPortalBaseUrl),
                "ApprovalReminders:ManagerPortalBaseUrl must be an absolute HTTP or HTTPS URL when email is enabled.")
            .ValidateOnStart();
        services.AddOptions<SmtpReminderOptions>()
            .Bind(configuration.GetSection(SmtpReminderOptions.SectionName));

        services.AddSingleton(new SqlConnectionStringBuilder(connectionString));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApprovalReminderOptions>>()
                .Value
                .ToSettings());
        services.AddSingleton<ApprovalReminderMessageFactory>();
        services.AddScoped<IApprovalReminderRepository, ApprovalReminderRepository>();
        services.AddScoped<IApprovalReminderService, ApprovalReminderService>();
        services.AddSingleton<IEmailReminderSender, SmtpReminderSender>();
        services.AddSingleton<ISmsReminderSender, StoredProcedureSmsReminderSender>();
        services.AddHostedService<ReminderDatabaseCompatibilityValidator>();
        return services;
    }

    private static bool CanResolveTimeZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool IsValidAbsoluteHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
