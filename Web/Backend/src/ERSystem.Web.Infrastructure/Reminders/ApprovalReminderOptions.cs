using ERSystem.Web.Application.Features.ApprovalReminders;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class ApprovalReminderOptions
{
    public const string SectionName = "ApprovalReminders";

    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public int ActivationPollIntervalSeconds { get; set; } = 60;
    public string RunAtLocalTime { get; set; } = "08:00";
    public string TimeZoneId { get; set; } = "Asia/Manila";
    public int InitialDelayDays { get; set; } = 3;
    public string ReminderDayOfWeek { get; set; } = nameof(DayOfWeek.Wednesday);
    public string ManagerPortalBaseUrl { get; set; } = string.Empty;

    public ApprovalReminderSettings ToSettings()
    {
        if (!TimeOnly.TryParseExact(RunAtLocalTime, "HH:mm", out var runAt))
        {
            throw new InvalidOperationException("ApprovalReminders:RunAtLocalTime must use HH:mm format.");
        }

        if (!Enum.TryParse<DayOfWeek>(ReminderDayOfWeek, true, out var reminderDayOfWeek) ||
            !Enum.IsDefined(reminderDayOfWeek))
        {
            throw new InvalidOperationException(
                "ApprovalReminders:ReminderDayOfWeek must be a valid day of the week.");
        }

        return new ApprovalReminderSettings(
            EmailEnabled,
            SmsEnabled,
            ActivationPollIntervalSeconds,
            TimeZoneId,
            runAt,
            InitialDelayDays,
            reminderDayOfWeek,
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
