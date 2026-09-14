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
}
