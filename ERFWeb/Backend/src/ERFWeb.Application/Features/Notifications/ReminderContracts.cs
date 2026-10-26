namespace ERFWeb.Application.Features.Notifications;

/// <summary>What one reminder run did; counts only, for the log.</summary>
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
}
