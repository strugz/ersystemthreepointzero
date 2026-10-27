using System.Globalization;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// The daily SLA reminder run, bound from the optional <c>Reminders</c> section of appsettings.json.
/// Off unless <see cref="Enabled"/> is true, so a developer machine pointed at a shared database never
/// sends reminders. The SLA days themselves are System Administrator rules (Settings), not here.
/// </summary>
public sealed class ReminderOptions
{
    public const string SectionName = "Reminders";

    public bool Enabled { get; set; }

    /// <summary>Time of day (business time zone, <c>ReferenceNumber:UtcOffsetHours</c>) to run on working days, "HH:mm".</summary>
    public string RunAt { get; set; } = "08:00";

    /// <summary>False when <see cref="RunAt"/> is not a time such as 08:00; the job then does not run.</summary>
    public bool TryGetRunAt(out TimeOnly runAt) =>
        TimeOnly.TryParseExact(RunAt, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out runAt);
}
