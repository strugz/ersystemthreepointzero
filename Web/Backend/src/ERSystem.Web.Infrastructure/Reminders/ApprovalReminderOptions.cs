using ERSystem.Web.Application.Features.ApprovalReminders;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class ApprovalReminderOptions
{
    public const string SectionName = "ApprovalReminders";

    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public string RunAtLocalTime { get; set; } = "08:00";
    public string TimeZoneId { get; set; } = "Asia/Manila";
    public int InitialDelayDays { get; set; } = 3;
    public int RepeatIntervalDays { get; set; } = 3;
    public string ManagerPortalBaseUrl { get; set; } = string.Empty;

    public ApprovalReminderSettings ToSettings()
    {
        if (!TimeOnly.TryParseExact(RunAtLocalTime, "HH:mm", out var runAt))
        {
            throw new InvalidOperationException("ApprovalReminders:RunAtLocalTime must use HH:mm format.");
        }

        return new ApprovalReminderSettings(
            EmailEnabled,
            SmsEnabled,
            TimeZoneId,
            runAt,
            InitialDelayDays,
            RepeatIntervalDays,
            ManagerPortalBaseUrl);
    }
}

public sealed class SmtpReminderOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string TlsMode { get; set; } = "StartTls";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = "ER System";
}
