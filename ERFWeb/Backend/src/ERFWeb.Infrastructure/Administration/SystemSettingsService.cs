using System.Globalization;
using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain.Administration;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>Keys of the rows in <c>app_settings</c>, with their defaults.</summary>
public static class SystemSettingKeys
{
    public const string Level2ThresholdAmount = "Workflow.Level2ThresholdAmount";
    public const string Level2AlwaysRequired = "Workflow.Level2AlwaysRequired";
    public const string SlaDaysLevel1 = "Workflow.SlaDaysLevel1";
    public const string SlaDaysLevel2 = "Workflow.SlaDaysLevel2";
    public const string EscalationAfterDays = "Workflow.EscalationAfterDays";
    public const string FilingCutoffDays = "Business.FilingCutoffDays";
    public const string ReceiptRequiredAboveAmount = "Business.ReceiptRequiredAboveAmount";
    public const string CoolingPeriodDays = "Business.CoolingPeriodDays";
    public const string EmailNotificationsEnabled = "Notifications.EmailEnabled";
    public const string SmsNotificationsEnabled = "Notifications.SmsEnabled";
    public const string NotificationFromAddress = "Notifications.FromAddress";
    public const string EmailDomains = "Accounts.EmailDomains";
    public const string R47ReviewedBy = "Reports.R47ReviewedBy";

    /// <summary>Longest REVIEWED BY text that still fits the R47 signatory line.</summary>
    public const int R47ReviewedByMaxLength = 60;

    public static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>
    {
        [Level2ThresholdAmount] = "0",
        [Level2AlwaysRequired] = "true",
        [SlaDaysLevel1] = "3",
        [SlaDaysLevel2] = "3",
        [EscalationAfterDays] = "3",
        [FilingCutoffDays] = "0",
        [ReceiptRequiredAboveAmount] = "0",
        [CoolingPeriodDays] = AccountRules.DefaultCoolingPeriodDays.ToString(CultureInfo.InvariantCulture),
        [EmailNotificationsEnabled] = "false",
        [SmsNotificationsEnabled] = "false",
        [NotificationFromAddress] = "",
        [EmailDomains] = EmailDomainRules.DefaultDomain,
        [R47ReviewedBy] = "FINANCE",
    };
}

/// <summary>
/// Workflow, business, and notification rules stored as key/value rows so the System
/// Administrator can change them without a deployment. Missing rows fall back to defaults,
/// so the application starts on a database that predates the table contents.
/// </summary>
public sealed class SystemSettingsService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit) : ISystemSettingsService
{
    public async Task<SystemSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var rows = await db.AppSettings.AsNoTracking().ToListAsync(ct);
        var values = rows.ToDictionary(r => r.Key, r => r.Value, StringComparer.Ordinal);

        var latest = rows.OrderByDescending(r => r.UpdatedAt).FirstOrDefault();
        string? updatedByName = null;
        if (latest?.UpdatedByUserId is { } updatedBy)
        {
            updatedByName = await db.Users.AsNoTracking().Where(u => u.Id == updatedBy).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        }

        return new SystemSettingsDto(
            Decimal(values, SystemSettingKeys.Level2ThresholdAmount),
            Bool(values, SystemSettingKeys.Level2AlwaysRequired),
            Int(values, SystemSettingKeys.SlaDaysLevel1),
            Int(values, SystemSettingKeys.SlaDaysLevel2),
            Int(values, SystemSettingKeys.EscalationAfterDays),
            Int(values, SystemSettingKeys.FilingCutoffDays),
            Decimal(values, SystemSettingKeys.ReceiptRequiredAboveAmount),
            Int(values, SystemSettingKeys.CoolingPeriodDays),
            Bool(values, SystemSettingKeys.EmailNotificationsEnabled),
            Bool(values, SystemSettingKeys.SmsNotificationsEnabled),
            Text(values, SystemSettingKeys.NotificationFromAddress),
            latest?.UpdatedAt,
            updatedByName,
            EmailDomainRules.Parse(Raw(values, SystemSettingKeys.EmailDomains)),
            Text(values, SystemSettingKeys.R47ReviewedBy) ?? SystemSettingKeys.Defaults[SystemSettingKeys.R47ReviewedBy]);
    }

    public async Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var incoming = new Dictionary<string, string>
        {
            [SystemSettingKeys.Level2ThresholdAmount] = request.Level2ThresholdAmount.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.Level2AlwaysRequired] = request.Level2AlwaysRequired ? "true" : "false",
            [SystemSettingKeys.SlaDaysLevel1] = request.SlaDaysLevel1.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.SlaDaysLevel2] = request.SlaDaysLevel2.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.EscalationAfterDays] = request.EscalationAfterDays.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.FilingCutoffDays] = request.FilingCutoffDays.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.ReceiptRequiredAboveAmount] = request.ReceiptRequiredAboveAmount.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.CoolingPeriodDays] = request.CoolingPeriodDays.ToString(CultureInfo.InvariantCulture),
            [SystemSettingKeys.EmailNotificationsEnabled] = request.EmailNotificationsEnabled ? "true" : "false",
            [SystemSettingKeys.SmsNotificationsEnabled] = request.SmsNotificationsEnabled ? "true" : "false",
            [SystemSettingKeys.NotificationFromAddress] = request.NotificationFromAddress?.Trim() ?? string.Empty,
        };

        // A client that predates the domain list sends none; leave the stored list as it is.
        if (request.EmailDomains is not null)
        {
            incoming[SystemSettingKeys.EmailDomains] = EmailDomainRules.Format(EmailDomainRules.Normalize(request.EmailDomains).Domains);
        }

        // Likewise a client that predates the R47 REVIEWED BY rule sends none.
        if (request.R47ReviewedBy is not null)
        {
            incoming[SystemSettingKeys.R47ReviewedBy] = request.R47ReviewedBy.Trim();
        }

        await tx.ExecuteAsync(async token =>
        {
            var existing = await db.AppSettings.ToDictionaryAsync(r => r.Key, token);
            var changes = new Dictionary<string, object>();
            var now = clock.UtcNow;

            foreach (var (key, value) in incoming)
            {
                if (existing.TryGetValue(key, out var row))
                {
                    if (row.Value == value)
                    {
                        continue;
                    }

                    changes[key] = new { from = row.Value, to = value };
                    row.Value = value;
                    row.UpdatedByUserId = user.UserId;
                    row.UpdatedAt = now;
                }
                else
                {
                    var previous = SystemSettingKeys.Defaults.TryGetValue(key, out var d) ? d : null;
                    if (previous == value)
                    {
                        // Unchanged from the default: still persist it so the row exists, but do not report a change.
                        db.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedByUserId = user.UserId, UpdatedAt = now });
                        continue;
                    }

                    changes[key] = new { from = previous, to = value };
                    db.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedByUserId = user.UserId, UpdatedAt = now });
                }
            }

            await db.SaveChangesAsync(token);
            if (changes.Count > 0)
            {
                await audit.WriteAsync(new WorkflowAuditEntry(
                    AdminAuditEvents.SettingsChanged, null, user.UserId, JsonSerializer.Serialize(changes)), token);
            }
        }, ct);

        return await GetAsync(ct);
    }

    public async Task<int> GetCoolingPeriodDaysAsync(CancellationToken ct = default)
    {
        var value = await db.AppSettings.AsNoTracking()
            .Where(s => s.Key == SystemSettingKeys.CoolingPeriodDays)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
        return value is not null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) && days >= 0
            ? days
            : AccountRules.DefaultCoolingPeriodDays;
    }

    private static void Validate(UpdateSystemSettingsRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        if (r.Level2ThresholdAmount < 0) errors["level2ThresholdAmount"] = ["The threshold cannot be negative."];
        if (r.SlaDaysLevel1 < 0 || r.SlaDaysLevel1 > 365) errors["slaDaysLevel1"] = ["Enter 0 to 365 days."];
        if (r.SlaDaysLevel2 < 0 || r.SlaDaysLevel2 > 365) errors["slaDaysLevel2"] = ["Enter 0 to 365 days."];
        if (r.EscalationAfterDays < 0 || r.EscalationAfterDays > 365) errors["escalationAfterDays"] = ["Enter 0 to 365 days."];
        if (r.FilingCutoffDays < 0 || r.FilingCutoffDays > 365) errors["filingCutoffDays"] = ["Enter 0 to 365 days (0 disables the cut-off)."];
        if (r.ReceiptRequiredAboveAmount < 0) errors["receiptRequiredAboveAmount"] = ["The amount cannot be negative."];
        if (r.CoolingPeriodDays < 0 || r.CoolingPeriodDays > 90) errors["coolingPeriodDays"] = ["Enter 0 to 90 days (0 disables the cooling period)."];
        if (!string.IsNullOrWhiteSpace(r.NotificationFromAddress) && (r.NotificationFromAddress.Length > 200 || !r.NotificationFromAddress.Contains('@')))
        {
            errors["notificationFromAddress"] = ["Enter a valid sender email address."];
        }

        if (r.EmailDomains is not null && EmailDomainRules.Normalize(r.EmailDomains).Error is { } domainError)
        {
            errors["emailDomains"] = [domainError];
        }

        if (r.R47ReviewedBy is not null && (string.IsNullOrWhiteSpace(r.R47ReviewedBy) || r.R47ReviewedBy.Trim().Length > SystemSettingKeys.R47ReviewedByMaxLength))
        {
            errors["r47ReviewedBy"] = [$"Enter who reviews the report, up to {SystemSettingKeys.R47ReviewedByMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static string Raw(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var v) ? v : SystemSettingKeys.Defaults[key];

    private static decimal Decimal(IReadOnlyDictionary<string, string> values, string key) =>
        decimal.TryParse(Raw(values, key), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static int Int(IReadOnlyDictionary<string, string> values, string key) =>
        int.TryParse(Raw(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;

    private static bool Bool(IReadOnlyDictionary<string, string> values, string key) =>
        string.Equals(Raw(values, key), "true", StringComparison.OrdinalIgnoreCase);

    private static string? Text(IReadOnlyDictionary<string, string> values, string key)
    {
        var v = Raw(values, key);
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }
}
