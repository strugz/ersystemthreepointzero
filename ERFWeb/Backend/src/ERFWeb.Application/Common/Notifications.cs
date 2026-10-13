namespace ERFWeb.Application.Common;

/// <summary>A filed report awaiting its first approver.</summary>
public sealed record ReportFiledNotification(
    Guid ReportId,
    string? ReferenceNo,
    string EmployeeName,
    long ApproverUserId,
    string ApproverName,
    string? ApproverEmail,
    string? ApproverMobileNo,
    int Cycle,
    bool IsRefile);

/// <summary>
/// Sends workflow notifications (email/SMS). Implementations must never throw into the
/// workflow transaction: a failed notification is logged, not a failed filing.
/// </summary>
public interface IApprovalNotifier
{
    Task NotifyReportFiledAsync(ReportFiledNotification notification, CancellationToken ct = default);

    /// <summary>Approved, returned, rejected, overridden, handed to the next approver, or a Finance step or follow-up.</summary>
    Task NotifyWorkflowEventAsync(WorkflowEventNotification notification, CancellationToken ct = default);
}

/// <summary>A workflow event for the people who need to hear about it (email and SMS when delivery lands).</summary>
public sealed record WorkflowEventNotification(
    string Kind,
    Guid ReportId,
    string? ReferenceNo,
    string EmployeeName,
    IReadOnlyList<long> RecipientUserIds,
    string Summary);
