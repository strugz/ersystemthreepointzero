using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Application.Features.Verification;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Adds email to the SMS notifier, so the employee hears at every stop (spec 7). Filing, re-filing,
/// each approval, full approval, a return and a rejection email the employee and the report's Level 1
/// and Level 2 signatories, with the R47 PDF as it stands after the event attached. Other workflow
/// events (override, line adjustments, Finance steps) are only logged and texted as before. Events
/// addressed to their recipients (verification steps) email those recipients only, R47 attached.
/// <para>
/// Runs after the workflow has committed and never throws: an email problem is logged, never a failed
/// action. Keys follow the SMS keys (report, approval cycle, step), so each event emails once.
/// </para>
/// </summary>
public sealed class EmailApprovalNotifier(
    SmsApprovalNotifier inner,
    IReportEmailService email,
    ILogger<EmailApprovalNotifier> logger) : IApprovalNotifier
{
    public const string FiledKind = "ReportFiled";

    public async Task NotifyReportFiledAsync(ReportFiledNotification notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await inner.NotifyReportFiledAsync(notification, ct);
        await SendAsync(notification.ReportId, ComposeFiled(notification), $"filed:{notification.ReportId:N}:{notification.Cycle}");
    }

    public async Task NotifyWorkflowEventAsync(WorkflowEventNotification notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await inner.NotifyWorkflowEventAsync(notification, ct);

        if (ComposeWorkflowEmail(notification) is { } composed)
        {
            await SendAsync(notification.ReportId, composed.Content, composed.Key);
        }
        else if (ComposeAddressedEmail(notification) is { } addressed)
        {
            await SendAsync(notification.ReportId, addressed, notification.DeliveryKey!, notification.RecipientUserIds);
        }
    }

    /// <summary>
    /// The email for an event addressed to its recipients (a verification step), or null for other
    /// events. One email to all of them. Public for tests.
    /// </summary>
    public static ReportEmailContent? ComposeAddressedEmail(WorkflowEventNotification n)
    {
        ArgumentNullException.ThrowIfNull(n);
        if (string.IsNullOrWhiteSpace(n.DeliveryKey) || n.RecipientUserIds.Count == 0)
        {
            return null;
        }

        var reference = n.ReferenceNo ?? "An expense report";
        var subject = n.Kind switch
        {
            VerificationAuditEvents.Requested => $"{reference} - a line was sent for verification",
            VerificationAuditEvents.Found => $"{reference} - verification finding recorded",
            VerificationAuditEvents.QueryRaised => $"{reference} - your decision is needed on a verification query",
            VerificationAuditEvents.QueryDecided => $"{reference} - verification query decided",
            _ => $"{reference} - update",
        };
        return new ReportEmailContent(n.Kind, subject, Body($"{reference} of {n.EmployeeName}: {n.Summary.Trim()}"));
    }

    /// <summary>The email for a filing or re-filing. Public for tests.</summary>
    public static ReportEmailContent ComposeFiled(ReportFiledNotification n)
    {
        ArgumentNullException.ThrowIfNull(n);
        var reference = n.ReferenceNo ?? "An expense report";
        var action = n.IsRefile ? "re-filed" : "filed";
        return new ReportEmailContent(
            FiledKind,
            $"{reference} {action} by {n.EmployeeName} - for approval",
            Body($"{reference} was {action} by {n.EmployeeName} and is waiting for {n.ApproverName}'s approval."));
    }

    /// <summary>The email for an approval decision, or null for events that do not email. Public for tests.</summary>
    public static (ReportEmailContent Content, string Key)? ComposeWorkflowEmail(WorkflowEventNotification n)
    {
        ArgumentNullException.ThrowIfNull(n);
        if (n.Cycle is not { } cycle || n.StepOrder is not { } step)
        {
            return null;
        }

        var reference = n.ReferenceNo ?? "An expense report";
        var actor = string.IsNullOrWhiteSpace(n.ActorName) ? "the approver" : n.ActorName;
        var note = string.IsNullOrWhiteSpace(n.Note) ? null : $"\n\nRemarks: {n.Note.Trim()}";
        (string Subject, string Text)? mail = n.Kind switch
        {
            ApprovalAuditEvents.Approved => ($"{reference} approved by {actor}", string.IsNullOrWhiteSpace(n.NextApproverName)
                ? $"{reference} of {n.EmployeeName} was approved by {actor} and is waiting for the next approval."
                : $"{reference} of {n.EmployeeName} was approved by {actor} and is now waiting for {n.NextApproverName}'s approval."),
            ApprovalAuditEvents.FullyApproved => ($"{reference} fully approved",
                $"{reference} of {n.EmployeeName} was fully approved by {actor}. Please submit the original receipts to Finance."),
            ApprovalAuditEvents.Returned => ($"{reference} returned by {actor}",
                $"{reference} of {n.EmployeeName} was returned by {actor}. Please update and re-file it.{note}"),
            ApprovalAuditEvents.Rejected => ($"{reference} rejected by {actor}",
                $"{reference} of {n.EmployeeName} was rejected by {actor}. A rejected report cannot be re-filed.{note}"),
            _ => null,
        };

        return mail is { } m
            ? (new ReportEmailContent(n.Kind, m.Subject, Body(m.Text)), $"{n.Kind}:{n.ReportId:N}:{cycle}:{step}")
            : null;
    }

    private static string Body(string text) =>
        $"Good day,\n\n{text}\n\nThe Expense Report (R47) PDF is attached. Sign in to ERFWeb to view the report.\n\nThis is an automated message; please do not reply.";

    /// <param name="userIds">The people to email; null for the report's claimant and signatories.</param>
    private async Task SendAsync(Guid reportId, ReportEmailContent content, string key, IReadOnlyList<long>? userIds = null)
    {
        try
        {
            var result = userIds is null
                ? await email.SendAboutReportAsync(reportId, content, key, CancellationToken.None)
                : await email.SendToUsersAboutReportAsync(reportId, userIds, content, key, CancellationToken.None);
            if (result.Outcome == EmailSendOutcome.Sent)
            {
                logger.LogInformation("{Kind} email for report {ReportId} sent to {RecipientCount} address(es).", content.Kind, reportId, result.RecipientCount);
            }
            else
            {
                logger.LogWarning("{Kind} email for report {ReportId} not sent: {Outcome}.", content.Kind, reportId, result.Outcome);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Kind} email for report {ReportId} failed.", content.Kind, reportId);
        }
    }
}
