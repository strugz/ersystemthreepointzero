namespace ERFWeb.Application.Features.Notifications;

/// <summary>What one reminder run did; counts only, for the log and the <see cref="ReminderAuditEvents.Run"/> row.</summary>
/// <param name="StepsWaiting">Approval steps whose turn it is, overdue or not.</param>
/// <param name="Sent">Emails and SMS the gateways accepted.</param>
/// <param name="Failed">Refused by a gateway, or no answer; a refused one is retried on the next run.</param>
/// <param name="Skipped">Due, but nobody it was for has an address or number on file.</param>
public sealed record ReminderRunResult(int StepsWaiting, int Sent, int Failed, int Skipped);

/// <summary>
/// Sends the SLA reminders that are due today: to the approver (and their delegate) whose step is
/// overdue, and once to the report's Level 2 when a Level 1 step stays overdue. Safe to run more than
/// once a day: each reminder is claimed once in <c>reminder_deliveries</c>.
/// </summary>
public interface IApprovalReminderService
{
    Task<ReminderRunResult> RunAsync(CancellationToken ct = default);
}

/// <summary>Event names of reminder rows in <c>workflow_audit</c>.</summary>
public static class ReminderAuditEvents
{
    public const string Sent = "ReminderSent";
    public const string Failed = "ReminderFailed";
    public const string Unconfirmed = "ReminderUnconfirmed";

    /// <summary>One daily run finished; the detail is its <see cref="ReminderRunResult"/>.</summary>
    public const string Run = "ReminderRun";

    /// <summary>One daily run stopped on an error; the detail names the exception type only.</summary>
    public const string RunFailed = "ReminderRunFailed";
}

/// <summary>
/// Whether the daily reminder job is on and what its last run did, for the System Administrator's
/// Rules page. Read from the server's <c>Reminders</c> settings and the last run row in <c>workflow_audit</c>.
/// </summary>
/// <param name="Enabled"><c>Reminders:Enabled</c> is true and <c>Reminders:RunAt</c> is a valid time.</param>
/// <param name="RunAt">Time of day of the run on working days, "HH:mm", business time zone.</param>
/// <param name="EmailConfigured">The <c>Smtp</c> section is set, so reminders go by email.</param>
/// <param name="SmsConfigured">The <c>Lasius</c> section is set, so reminders go by SMS.</param>
/// <param name="NextRunAt">The next scheduled run; null when the job is off.</param>
/// <param name="LastRun">The most recent run recorded; null when none has run yet.</param>
public sealed record ReminderStatusDto(
    bool Enabled,
    string RunAt,
    bool EmailConfigured,
    bool SmsConfigured,
    DateTimeOffset? NextRunAt,
    ReminderLastRunDto? LastRun);

/// <param name="Succeeded">False when the run stopped on an error (the server log has the details); the counts are then null.</param>
public sealed record ReminderLastRunDto(
    DateTimeOffset At,
    bool Succeeded,
    int? StepsWaiting,
    int? Sent,
    int? Failed,
    int? Skipped);

public interface IReminderStatusService
{
    Task<ReminderStatusDto> GetAsync(CancellationToken ct = default);
}
