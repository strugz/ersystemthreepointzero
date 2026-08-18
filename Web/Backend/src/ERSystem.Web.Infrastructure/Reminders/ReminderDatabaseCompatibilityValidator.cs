using ERSystem.Web.Application.Features.ApprovalReminders;
using ERSystem.Web.Infrastructure.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class ReminderDatabaseCompatibilityValidator(
    SqlConnectionStringBuilder connectionString,
    ApprovalReminderSettings settings,
    IOptions<SmtpReminderOptions> smtpOptions,
    IOptions<LegacyAuthenticationOptions> legacyAuthenticationOptions,
    ILogger<ReminderDatabaseCompatibilityValidator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ValidateConfiguration(settings, smtpOptions.Value, legacyAuthenticationOptions.Value);

        const string sql = """
            SELECT CONVERT(int, compatibility_level),
                   CASE WHEN OBJECT_ID(N'dbo.tbReportApprovalTransaction', N'U') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN OBJECT_ID(N'dbo.tbReportApprovalReminderDelivery', N'U') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN COL_LENGTH(N'dbo.tbUserRegistration', N'EmailAdd') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN COL_LENGTH(N'dbo.tbUserRegistration', N'EmailPass') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN COL_LENGTH(N'dbo.tbUserRegistration', N'EmailTo') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN OBJECT_ID(N'dbo.sp_Notify', N'P') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN EXISTS
                   (
                       SELECT 1 FROM sys.parameters
                       WHERE object_id = OBJECT_ID(N'dbo.sp_Notify') AND name = N'@ReminderApproverUsername'
                   ) THEN 1 ELSE 0 END,
                   CASE WHEN EXISTS
                   (
                       SELECT 1 FROM sys.parameters
                       WHERE object_id = OBJECT_ID(N'dbo.sp_Notify') AND name = N'@ReminderMessage'
                   ) THEN 1 ELSE 0 END,
                   CASE WHEN EXISTS
                   (
                       SELECT 1
                       FROM sys.check_constraints
                       WHERE parent_object_id = OBJECT_ID(N'dbo.tbReportApprovalReminderDelivery')
                         AND name = N'CK_tbReportApprovalReminderDelivery_ReminderNumber'
                         AND is_disabled = 0
                         AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                                 UPPER(definition), N' ', N''), N'(', N''), N')', N''), N'[', N''), N']', N'')
                             LIKE N'%REMINDERNUMBER>=0%'
                   ) THEN 1 ELSE 0 END
            FROM sys.databases
            WHERE name = DB_NAME();
            """;

        await using var connection = new SqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Unable to read reminder database compatibility information.");
        }

        if (reader.GetInt32(0) != 100)
        {
            throw new InvalidOperationException("The reminder service requires database compatibility level 100.");
        }

        for (var ordinal = 1; ordinal <= 9; ordinal++)
        {
            if (reader.GetInt32(ordinal) == 0)
            {
                throw new InvalidOperationException(
                    "Approval reminder database objects are missing or outdated. Apply all dated reminder support scripts before starting the service.");
            }
        }

        logger.LogInformation("Validated the approval reminder database contract at compatibility level 100");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void ValidateConfiguration(
        ApprovalReminderSettings reminderSettings,
        SmtpReminderOptions smtp,
        LegacyAuthenticationOptions legacyAuthentication)
    {
        if (!reminderSettings.EmailEnabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(smtp.Host) || smtp.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException("Valid SMTP host and port settings are required when email reminders are enabled.");
        }

        if (string.IsNullOrWhiteSpace(legacyAuthentication.EncryptionKey))
        {
            throw new InvalidOperationException(
                "LegacyAuthentication:EncryptionKey is required when email reminders are enabled.");
        }

        if (smtp.TlsMode.Trim().ToUpperInvariant() is not ("NONE" or "SSLONCONNECT" or "STARTTLS"))
        {
            throw new InvalidOperationException("Smtp:TlsMode must be None, SslOnConnect, or StartTls.");
        }
    }
}
