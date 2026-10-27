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
/// <param name="ActorName">Who acted (approval decisions); used to word the SMS.</param>
/// <param name="Note">The approver's remark, e.g. why a report was returned.</param>
/// <param name="NextApproverName">Who the report now waits for, after a Level 1 approval.</param>
/// <param name="Cycle">Approval cycle the decision belongs to; with <paramref name="StepOrder"/> it makes the SMS key.</param>
/// <param name="StepOrder">The step that acted.</param>
/// <param name="DeliveryKey">
/// Set for an event that is texted and emailed to <paramref name="RecipientUserIds"/> themselves (e.g. a
/// verification step) rather than to the report's claimant and signatories; unique per event, so each sends once.
/// </param>
public sealed record WorkflowEventNotification(
    string Kind,
    Guid ReportId,
    string? ReferenceNo,
    string EmployeeName,
    IReadOnlyList<long> RecipientUserIds,
    string Summary,
    string? ActorName = null,
    string? Note = null,
    string? NextApproverName = null,
    int? Cycle = null,
    int? StepOrder = null,
    string? DeliveryKey = null);
