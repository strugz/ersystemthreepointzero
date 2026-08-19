namespace ERSystem.Web.Application.Features.ApprovalReminders;

public sealed record ApprovalReminderSettings(
    bool EmailEnabled,
    bool SmsEnabled,
    int ActivationPollIntervalSeconds,
    string TimeZoneId,
    TimeOnly RunAtLocalTime,
    int InitialDelayDays,
    DayOfWeek ReminderDayOfWeek);

public sealed record ApprovalReminderCandidate(
    long ApprovalTransactionId,
    string ReportId,
    int ApprovalCycle,
    int EmployeeUserId,
    string EmployeeUsername,
    string EmployeeFullName,
    int ManagerUserId,
    string ManagerUsername,
    string ManagerFullName,
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
    SmsApi
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

public enum ReminderSendOutcome
{
    Sent,
    Skipped,
    Failed
}

public sealed record ReminderSendResult(ReminderSendOutcome Outcome, string? FailureCode)
{
    public bool Succeeded => Outcome == ReminderSendOutcome.Sent;

    public static ReminderSendResult Success { get; } = new(ReminderSendOutcome.Sent, null);

    public static ReminderSendResult Skipped(string failureCode) =>
        new(ReminderSendOutcome.Skipped, failureCode);

    public static ReminderSendResult Failed(string failureCode) =>
        new(ReminderSendOutcome.Failed, failureCode);
}

public sealed record ApprovalReminderRunSummary(
    int CandidatesFound,
    int DueCandidates,
    int EmailSent,
    int SmsSent,
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
        int employeeUserId,
        ReminderAudience audience,
        ReminderEmail message,
        CancellationToken cancellationToken);
}

public interface ISmsReminderSender
{
    Task<ReminderSendResult> SendAsync(
        string receiverUsername,
        string senderUsername,
        string message,
        CancellationToken cancellationToken);
}

public interface IApprovalReminderService
{
    Task<ApprovalReminderRunSummary> RunActivationNotificationsAsync(CancellationToken cancellationToken);

    Task<ApprovalReminderRunSummary> RunScheduledRemindersAsync(CancellationToken cancellationToken);
}
