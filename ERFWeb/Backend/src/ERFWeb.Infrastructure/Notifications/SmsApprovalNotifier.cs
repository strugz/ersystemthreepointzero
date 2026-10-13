using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Notifications;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Adds the filing SMS to the logging notifier: when an employee files (or re-files) a report, one SMS
/// goes to the employee and the report's Level 1 and Level 2 signatories, whoever has a mobile number.
/// Other workflow events are only logged for now.
/// <para>
/// Runs after the filing has committed and never throws: an SMS problem is logged, never a failed
/// filing. The key is fixed per report and approval cycle, so one filing texts once, and a re-file after
/// a return (a new cycle) texts again. The send ignores the request's cancellation, so it still
/// completes if the employee leaves the page.
/// </para>
/// </summary>
public sealed class SmsApprovalNotifier(
    LoggingApprovalNotifier inner,
    ISmsService sms,
    ILogger<SmsApprovalNotifier> logger) : IApprovalNotifier
{
    public async Task NotifyReportFiledAsync(ReportFiledNotification notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await inner.NotifyReportFiledAsync(notification, ct);

        var reference = notification.ReferenceNo ?? "An expense report";
        var message = $"{reference} was {(notification.IsRefile ? "re-filed" : "filed")} by {notification.EmployeeName} and is waiting for approval in ERFWeb.";
        var key = $"filed:{notification.ReportId:N}:{notification.Cycle}";
        try
        {
            var result = await sms.SendAboutReportAsync(notification.ReportId, message, key, CancellationToken.None);
            if (result.Outcome == SmsSendOutcome.Sent)
            {
                logger.LogInformation("Filing SMS for report {ReportId} sent to {RecipientCount} number(s).", notification.ReportId, result.RecipientCount);
            }
            else
            {
                logger.LogWarning("Filing SMS for report {ReportId} not sent: {Outcome}.", notification.ReportId, result.Outcome);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Filing SMS for report {ReportId} failed.", notification.ReportId);
        }
    }

    public Task NotifyWorkflowEventAsync(WorkflowEventNotification notification, CancellationToken ct = default) =>
        inner.NotifyWorkflowEventAsync(notification, ct);
}
