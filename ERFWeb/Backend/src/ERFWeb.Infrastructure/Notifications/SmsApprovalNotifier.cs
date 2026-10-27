using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Adds SMS to the logging notifier. Filing, re-filing, each approval and a return send one SMS to the
/// employee and the report's Level 1 and Level 2 signatories, whoever has a mobile number. Other
/// workflow events (reject, override, line adjustments, Finance steps) are only logged for now, except
/// events addressed to their recipients (verification steps), which text each recipient alone.
/// <para>
/// Runs after the workflow has committed and never throws: an SMS problem is logged, never a failed
/// action. Each event has a fixed key per report, approval cycle and step, so it texts once; a new
/// cycle after a return texts again. The send ignores the request's cancellation, so it still completes
/// if the user leaves the page.
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
        await SendAsync(notification.ReportId, message, $"filed:{notification.ReportId:N}:{notification.Cycle}", "Filing");
    }

    public async Task NotifyWorkflowEventAsync(WorkflowEventNotification notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await inner.NotifyWorkflowEventAsync(notification, ct);

        if (ComposeWorkflowSms(notification) is { } sms)
        {
            await SendAsync(notification.ReportId, sms.Message, sms.Key, notification.Kind);
        }
        else if (ComposeAddressedSms(notification) is { } addressed)
        {
            foreach (var userId in notification.RecipientUserIds.Distinct())
            {
                await SendToUserAsync(notification.ReportId, userId, addressed, $"{notification.DeliveryKey}:{userId}", notification.Kind);
            }
        }
    }

    /// <summary>
    /// The SMS for an event addressed to its recipients (a verification step), or null for other events.
    /// One message for all of them, shortened to fit one notification. Public for tests.
    /// </summary>
    public static string? ComposeAddressedSms(WorkflowEventNotification n)
    {
        ArgumentNullException.ThrowIfNull(n);
        if (string.IsNullOrWhiteSpace(n.DeliveryKey) || n.RecipientUserIds.Count == 0)
        {
            return null;
        }

        var text = $"{n.ReferenceNo ?? "An expense report"} of {n.EmployeeName}: {n.Summary.Trim()}";
        const string Tail = " See ERFWeb.";
        var room = SmsRules.MaxMessageLength - Tail.Length;
        if (text.Length > room)
        {
            // Plain dots, as in a return note: "…" would shrink every segment to 70 characters.
            text = text[..(room - 3)].TrimEnd() + "...";
        }

        return text + Tail;
    }

    /// <summary>
    /// The SMS for an approval decision, or null for events that do not text. Public for tests.
    /// A return quotes the approver's note, shortened so the whole message fits one notification.
    /// </summary>
    public static (string Message, string Key)? ComposeWorkflowSms(WorkflowEventNotification n)
    {
        ArgumentNullException.ThrowIfNull(n);
        if (n.Cycle is not { } cycle || n.StepOrder is not { } step)
        {
            return null;
        }

        var reference = n.ReferenceNo ?? "An expense report";
        var actor = string.IsNullOrWhiteSpace(n.ActorName) ? "the approver" : n.ActorName;
        var message = n.Kind switch
        {
            ApprovalAuditEvents.Approved => string.IsNullOrWhiteSpace(n.NextApproverName)
                ? $"{reference} was approved by {actor} and is waiting for the next approval in ERFWeb."
                : $"{reference} was approved by {actor} and is now waiting for {n.NextApproverName}'s approval in ERFWeb.",
            ApprovalAuditEvents.FullyApproved => $"{reference} was fully approved by {actor}. Please submit the original receipts to Finance.",
            ApprovalAuditEvents.Returned => ReturnMessage(reference, actor, n.Note),
            _ => null,
        };

        return message is null ? null : (message, $"{n.Kind}:{n.ReportId:N}:{cycle}:{step}");
    }

    private static string ReturnMessage(string reference, string actor, string? note)
    {
        var head = $"{reference} was returned by {actor}";
        const string Tail = ". Please update and re-file it in ERFWeb.";
        var text = note?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return head + Tail;
        }

        // Quote as much of the note as fits: head + ': "' + note + '"' + tail.
        var room = SmsRules.MaxMessageLength - head.Length - Tail.Length - 4;
        if (room < 10)
        {
            return head + Tail;
        }

        if (text.Length > room)
        {
            // Plain dots: "…" is not in the GSM-7 SMS alphabet and would shrink every segment to 70 characters.
            text = text[..(room - 3)].TrimEnd() + "...";
        }

        return $"{head}: \"{text}\"{Tail}";
    }

    private async Task SendAsync(Guid reportId, string message, string key, string label)
    {
        try
        {
            var result = await sms.SendAboutReportAsync(reportId, message, key, CancellationToken.None);
            if (result.Outcome == SmsSendOutcome.Sent)
            {
                logger.LogInformation("{Label} SMS for report {ReportId} sent to {RecipientCount} number(s).", label, reportId, result.RecipientCount);
            }
            else
            {
                logger.LogWarning("{Label} SMS for report {ReportId} not sent: {Outcome}.", label, reportId, result.Outcome);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Label} SMS for report {ReportId} failed.", label, reportId);
        }
    }

    private async Task SendToUserAsync(Guid reportId, long userId, string message, string key, string label)
    {
        try
        {
            var result = await sms.SendToUserAboutReportAsync(reportId, userId, message, key, CancellationToken.None);
            if (result.Outcome == SmsSendOutcome.Sent)
            {
                logger.LogInformation("{Label} SMS for report {ReportId} sent to {RecipientCount} number(s) of one user.", label, reportId, result.RecipientCount);
            }
            else
            {
                logger.LogWarning("{Label} SMS for report {ReportId} not sent to one user: {Outcome}.", label, reportId, result.Outcome);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Label} SMS for report {ReportId} failed for one user.", label, reportId);
        }
    }
}
