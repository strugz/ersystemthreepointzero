namespace ERSystem.Web.Application.Features.ApprovalReminders;

public sealed record ApprovalReminderSettings(
    bool EmailEnabled,
    bool SmsEnabled,
    int ActivationPollIntervalSeconds,
    string TimeZoneId,
    TimeOnly RunAtLocalTime,
    int InitialDelayDays,
    DayOfWeek ReminderDayOfWeek,
    string ManagerPortalBaseUrl);

public sealed record ApprovalReminderCandidate(
    long ApprovalTransactionId,
    string ReportId,
    int ApprovalCycle,
    int EmployeeUserId,
    string EmployeeUsername,
    string EmployeeFullName,
    string? EmployeeNotificationEmail,
    int ManagerUserId,
    string ManagerUsername,
    string ManagerFullName,
    string? ManagerNotificationEmail,
    string? ErfReferenceNumber,
    DateTime ActiveAtUtc);

public sealed record ReminderEmail(string Subject, string Body);

public sealed record ApprovalReminderMessages(
    string SmsMessage,
    ReminderEmail ManagerEmail,
    ReminderEmail EmployeeEmail);

public sealed record ApprovalActivationMessages(
    ReminderEmail ManagerEmail,
    ReminderEmail EmployeeEmail);

public enum ReminderChannel
{
    Email,
    SmsGateway
}

public enum ReminderAudience
{
    Manager,
    Employee,
    ManagerAndEmployee
}

public enum ReminderDeliveryStatus
{
    Attempting,
    Queued,
    Sent,
    Failed,
    Skipped
}

public sealed record ReminderSendResult(bool Succeeded, string? FailureCode)
{
    public static ReminderSendResult Success { get; } = new(true, null);

    public static ReminderSendResult Failed(string failureCode) => new(false, failureCode);
}

public sealed record ApprovalReminderRunSummary(
    int CandidatesFound,
    int DueCandidates,
    int Sent,
    int Queued,
    int Failed,
    int Skipped,
    int AlreadyClaimed);

public interface IApprovalReminderRepository
{
    Task<IReadOnlyList<ApprovalReminderCandidate>> GetActionableApprovalsAsync(CancellationToken cancellationToken);

    Task<long?> TryClaimAsync(
        ApprovalReminderCandidate candidate,
        int reminderNumber,
        ReminderChannel channel,
        ReminderAudience audience,
        int? recipientUserId,
        Guid correlationId,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        long deliveryId,
        ReminderDeliveryStatus status,
        string? failureCode,
        CancellationToken cancellationToken);
}

public interface IEmailReminderSender
{
    Task<ReminderSendResult> SendAsync(
        string recipientAddress,
        ReminderEmail message,
        CancellationToken cancellationToken);
}

public interface ISmsReminderSender
{
    Task<ReminderSendResult> QueueAsync(
        ApprovalReminderCandidate candidate,
        string message,
        CancellationToken cancellationToken);
}

public interface IApprovalReminderService
{
    Task<ApprovalReminderRunSummary> RunActivationNotificationsAsync(CancellationToken cancellationToken);

    Task<ApprovalReminderRunSummary> RunScheduledRemindersAsync(CancellationToken cancellationToken);
}
