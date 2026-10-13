using ERFWeb.Application.Common;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Phase 2 notifier: records the "please approve" event in the application log. The
/// SMTP/SMS senders that deliver the same message arrive with Phase 4 (email) and reuse
/// this seam, so the filing workflow does not change when delivery is switched on.
/// </summary>
public sealed class LoggingApprovalNotifier(ILogger<LoggingApprovalNotifier> logger) : IApprovalNotifier
{
    public Task NotifyReportFiledAsync(ReportFiledNotification n, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(n);
        logger.LogInformation(
            "Report {ReportId} ({ReferenceNo}) {Action} by {Employee}; awaiting approver {Approver} (user {ApproverId}, cycle {Cycle}). Email on file: {HasEmail}, mobile on file: {HasMobile}.",
            n.ReportId, n.ReferenceNo ?? "no ref", n.IsRefile ? "re-filed" : "filed", n.EmployeeName, n.ApproverName, n.ApproverUserId,
            n.Cycle, !string.IsNullOrWhiteSpace(n.ApproverEmail), !string.IsNullOrWhiteSpace(n.ApproverMobileNo));
        return Task.CompletedTask;
    }

    public Task NotifyWorkflowEventAsync(WorkflowEventNotification n, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(n);
        logger.LogInformation(
            "Report {ReportId} ({ReferenceNo}) of {Employee}: {Kind}. {Summary}. {RecipientCount} recipient(s) to notify.",
            n.ReportId, n.ReferenceNo ?? "no ref", n.EmployeeName, n.Kind, n.Summary, n.RecipientUserIds.Count);
        return Task.CompletedTask;
    }
}
