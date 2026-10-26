using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Notifications;
using ERFWeb.Infrastructure.Approvals;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// The daily SLA reminder run (<see cref="ApprovalReminderRules"/>). Runs without a signed-in user, so
/// it talks to the SMS and SMTP gateways directly instead of through the report notifiers, which send
/// as the current user and always copy the claimant.
/// <para>
/// Each reminder is claimed once per (report, step, reminder number, channel, audience) by an insert
/// into <c>reminder_deliveries</c> that the unique index lets only one run win, so running twice a day
/// (a restart, two servers) never sends twice. Only a reminder a gateway definitely refused is retried
/// on a later run while it is still the current one; one with no answer is not, as it may have gone out.
/// A channel whose gateway is not configured is left unclaimed, so it starts once it is set up.
/// </para>
/// </summary>
public sealed class ApprovalReminderService(
    ErfDbContext db,
    IClock clock,
    IOptions<ReferenceNumberOptions> calendar,
    ISystemSettingsService settings,
    ISmsGateway smsGateway,
    IEmailGateway emailGateway,
    IExpenseReportDocumentLoader loader,
    IExpenseReportPdfRenderer renderer,
    IWorkflowAuditWriter audit,
    ILogger<ApprovalReminderService> logger) : IApprovalReminderService
{
    public const string ApproverAudience = "Approver";
    public const string Level2Audience = "Level2";
    public const string EmailChannel = "Email";
    public const string SmsChannel = "SMS";

    private const string Sending = "Sending";
    private const string Sent = "Sent";
    private const string Failed = "Failed";
    private const string Unconfirmed = "Unconfirmed";
    private const string Skipped = "Skipped";

    /// <summary>Shown as the SMS sender; there is no signed-in user to name.</summary>
    private const string SmsSender = "ERFWeb";

    public async Task<ReminderRunResult> RunAsync(CancellationToken ct = default)
    {
        var rules = await settings.GetAsync(ct);
        var offset = TimeSpan.FromHours(calendar.Value.UtcOffsetHours);
        var today = LocalDate(clock.UtcNow, offset);

        var steps = await (
            from s in db.ApprovalSteps.AsNoTracking()
            join r in db.ExpenseReports.AsNoTracking() on s.ReportId equals r.Id
            where r.Status == ReportStatus.ForApproval && s.Cycle == r.CurrentCycle
            select new StepRow(s.Id, s.ReportId, s.StepOrder, s.SignatoryLevel, s.ApproverUserId, s.Status, s.ActedAt,
                r.ReferenceNo, r.EmployeeUserId, r.FiledAt))
            .ToListAsync(ct);

        var plans = new List<ReminderPlan>();
        var waiting = 0;
        foreach (var report in steps.GroupBy(s => s.ReportId))
        {
            var states = report.Select(s => new ApprovalStepState(s.StepOrder, s.Status)).ToList();
            if (ApprovalSequence.CurrentStepOrder(states) is not { } order || !ApprovalSequence.CanApprove(states, order))
            {
                continue;
            }

            var current = report.First(s => s.StepOrder == order);

            // The clock starts when the step became this approver's turn: the previous step's approval, or the filing.
            var since = report.Where(s => s.StepOrder < order).Max(s => s.ActedAt) ?? current.FiledAt;
            if (since is null)
            {
                continue;
            }

            waiting++;
            var waited = ApprovalReminderRules.WorkingDaysBetween(LocalDate(since.Value, offset), today);
            var level = Level(current);
            if (ApprovalReminderRules.ReminderNumberDue(waited, level == 1 ? rules.SlaDaysLevel1 : rules.SlaDaysLevel2) is { } number)
            {
                plans.Add(new ReminderPlan(current, number, ApproverAudience, [current.ApproverUserId], waited));
            }

            if (level == 1 && ApprovalReminderRules.EscalationDue(waited, rules.SlaDaysLevel1, rules.EscalationAfterDays))
            {
                var level2 = report
                    .Where(s => s.StepOrder > order && s.Status == ApprovalStatus.Pending && Level(s) == 2)
                    .Select(s => s.ApproverUserId)
                    .Distinct()
                    .ToList();
                if (level2.Count > 0)
                {
                    plans.Add(new ReminderPlan(current, 1, Level2Audience, level2, waited));
                }
            }
        }

        if (plans.Count == 0)
        {
            return new ReminderRunResult(waiting, 0, 0, 0);
        }

        var delegates = await DelegatesTodayAsync(plans.Where(p => p.Audience == ApproverAudience).Select(p => p.Step.ApproverUserId), today, ct);
        var userIds = plans.SelectMany(p => p.RecipientUserIds)
            .Concat(plans.Select(p => p.Step.ApproverUserId))
            .Concat(plans.Select(p => p.Step.EmployeeUserId))
            .Concat(delegates.Values.SelectMany(d => d))
            .Distinct()
            .ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new Contact(u.Id, u.FullName, u.Email, u.MobileNo, u.IsActive))
            .ToDictionaryAsync(u => u.Id, ct);

        int sent = 0, failed = 0, skipped = 0;
        foreach (var plan in plans)
        {
            // The approver's delegate (if one is in force today) can act, so they hear about it too.
            var recipientIds = plan.Audience == ApproverAudience
                ? plan.RecipientUserIds.Concat(delegates.GetValueOrDefault(plan.Step.ApproverUserId) ?? []).Distinct()
                : plan.RecipientUserIds;
            var recipients = recipientIds.Select(id => users.GetValueOrDefault(id)).OfType<Contact>().Where(u => u.IsActive).ToList();
            var text = Compose(plan, Name(users, plan.Step.EmployeeUserId), Name(users, plan.Step.ApproverUserId));

            foreach (var channel in new[] { EmailChannel, SmsChannel })
            {
                var status = await DeliverAsync(plan, channel, recipients, text, ct);
                switch (status)
                {
                    case Sent: sent++; break;
                    case Failed or Unconfirmed: failed++; break;
                    case Skipped: skipped++; break;
                }
            }
        }

        return new ReminderRunResult(waiting, sent, failed, skipped);
    }

    /// <summary>The reminder texts. Public for tests.</summary>
    public static ReminderText Compose(ReminderPlan plan, string employeeName, string approverName)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var reference = plan.Step.ReferenceNo ?? "An expense report";
        var days = plan.WorkingDaysWaited == 1 ? "1 working day" : $"{plan.WorkingDaysWaited} working days";
        return plan.Audience == Level2Audience
            ? new ReminderText(
                $"{reference} of {employeeName} has waited {days} for {approverName}'s approval",
                $"{reference} of {employeeName} has waited {days} for {approverName}'s Level 1 approval in ERFWeb. You are its Level 2 signatory.",
                $"{reference} of {employeeName} has waited {days} for {approverName}'s Level 1 approval. You are its Level 2 signatory; you may follow up with them, approve past them with a reason, or assign a delegate.")
            : new ReminderText(
                $"Reminder: {reference} is waiting for your approval",
                $"Reminder: {reference} of {employeeName} has waited {days} for your approval in ERFWeb.",
                $"{reference} of {employeeName} has waited {days} for your approval.");
    }

    private async Task<string?> DeliverAsync(ReminderPlan plan, string channel, IReadOnlyList<Contact> recipients, ReminderText text, CancellationToken ct)
    {
        var isSms = channel == SmsChannel;
        if (isSms ? !smsGateway.IsConfigured : !emailGateway.IsConfigured)
        {
            return null;
        }

        var to = isSms
            ? recipients.SelectMany(u => SmsRules.RecipientNumbers(u.MobileNo)).Distinct().ToList()
            : EmailRules.DistinctAddresses(recipients.Select(u => u.Email));

        var deliveryId = await ClaimAsync(plan, channel, to.Count == 0, ct);
        if (deliveryId is null)
        {
            return null;
        }

        if (to.Count == 0)
        {
            logger.LogInformation("{Channel} reminder {Number} ({Audience}) for report {ReportId} skipped: no contact on file.",
                channel, plan.Number, plan.Audience, plan.Step.ReportId);
            return Skipped;
        }

        // From here on: CancellationToken.None. The claim is taken, so the send and its record must finish.
        string status;
        string? failureCode = null;
        try
        {
            if (isSms)
            {
                var result = await smsGateway.SendAsync(new SmsGatewayMessage(SmsSender, to, text.Sms), CancellationToken.None);
                status = Status(result.Status == SmsGatewayStatus.Accepted, result.Status == SmsGatewayStatus.Rejected);
                failureCode = result.HttpStatus?.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                var document = await loader.LoadAsync(plan.Step.ReportId, CancellationToken.None);
                var pdf = renderer.Render(document, clock.UtcNow);
                var fileName = $"ExpenseReport-{(plan.Step.ReferenceNo is { Length: > 0 } r ? r : plan.Step.ReportId.ToString("N"))}.pdf";
                var body = $"Good day,\n\n{text.EmailBody}\n\nThe Expense Report (R47) PDF is attached. Sign in to ERFWeb to act on the report.\n\nThis is an automated reminder; please do not reply.";
                var result = await emailGateway.SendAsync(
                    new EmailMessage(to, text.Subject, body, [new EmailAttachment(fileName, "application/pdf", pdf)]),
                    CancellationToken.None);
                status = Status(result.Status == EmailGatewayStatus.Accepted, result.Status == EmailGatewayStatus.Rejected);
                failureCode = result.SmtpStatusCode?.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        catch (Exception ex)
        {
            // Unknown failure while rendering or inside a gateway client: whether anything left is unknown.
            logger.LogError(ex, "{Channel} reminder delivery {DeliveryId} failed unexpectedly.", channel, deliveryId);
            status = Unconfirmed;
        }

        var now = clock.UtcNow;
        var code = status == Sent ? null : failureCode;
        await db.ReminderDeliveries
            .Where(x => x.Id == deliveryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.SentAt, now)
                .SetProperty(x => x.FailureCode, code), CancellationToken.None);

        // Spec 9.9: every message sent is in the append-only log. Which reminder and how many, never the text or contacts.
        var auditEvent = status switch
        {
            Sent => ReminderAuditEvents.Sent,
            Failed => ReminderAuditEvents.Failed,
            _ => ReminderAuditEvents.Unconfirmed,
        };
        await audit.WriteAsync(
            new WorkflowAuditEntry(
                auditEvent,
                plan.Step.ReportId,
                null,
                JsonSerializer.Serialize(new { deliveryId, channel, audience = plan.Audience, reminderNumber = plan.Number, stepId = plan.Step.StepId, recipientCount = to.Count })),
            CancellationToken.None);

        if (status == Sent)
        {
            logger.LogInformation("{Channel} reminder {Number} ({Audience}) for report {ReportId} sent to {RecipientCount} recipient(s).",
                channel, plan.Number, plan.Audience, plan.Step.ReportId, to.Count);
        }
        else
        {
            logger.LogWarning("{Channel} reminder {Number} ({Audience}) for report {ReportId} not sent: {Status}.",
                channel, plan.Number, plan.Audience, plan.Step.ReportId, status);
        }

        return status;
    }

    /// <summary>The row id when this run may send the reminder; null when it was already handled.</summary>
    private async Task<long?> ClaimAsync(ReminderPlan plan, string channel, bool noContact, CancellationToken ct)
    {
        var step = plan.Step;
        var existing = await db.ReminderDeliveries.AsNoTracking()
            .Where(x => x.ReportId == step.ReportId && x.StepId == step.StepId && x.ReminderNumber == plan.Number
                && x.Channel == channel && x.Audience == plan.Audience)
            .Select(x => new { x.Id, x.Status })
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            var row = new ReminderDelivery
            {
                ReportId = step.ReportId,
                StepId = step.StepId,
                ReminderNumber = plan.Number,
                Channel = channel,
                Audience = plan.Audience,
                Status = noContact ? Skipped : Sending,
                FailureCode = noContact ? "NoContact" : null,
                SentAt = clock.UtcNow,
            };
            db.ReminderDeliveries.Add(row);
            try
            {
                await db.SaveChangesAsync(ct);
                return row.Id;
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
            {
                // Another run claimed it first.
                db.Entry(row).State = EntityState.Detached;
                return null;
            }
        }

        if (existing.Status != Failed || noContact)
        {
            return null;
        }

        // The gateway refused the earlier attempt, so nothing went out: this run may send it. The
        // conditional update lets only one of two simultaneous runs through.
        var claimed = await db.ReminderDeliveries
            .Where(x => x.Id == existing.Id && x.Status == Failed)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, Sending), ct);
        return claimed == 1 ? existing.Id : null;
    }

    /// <summary>
    /// Delegates in force today for each signatory. As in <see cref="DelegationLookup"/>, a delegate who
    /// no longer holds at least the signatory's level does not count.
    /// </summary>
    private async Task<Dictionary<long, List<long>>> DelegatesTodayAsync(IEnumerable<long> signatoryIds, DateOnly today, CancellationToken ct)
    {
        var ids = signatoryIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new();
        }

        var windows = await db.ApprovalDelegations.AsNoTracking()
            .Where(d => ids.Contains(d.SignatoryUserId) && d.CancelledAt == null && d.StartsOn <= today && d.EndsOn >= today)
            .Select(d => new { d.SignatoryUserId, d.DelegateUserId })
            .ToListAsync(ct);
        if (windows.Count == 0)
        {
            return new();
        }

        var levels = await DelegationLookup.SignatoryLevelsAsync(
            db, windows.SelectMany(w => new[] { w.SignatoryUserId, w.DelegateUserId }), ct);
        return windows
            .Where(w => levels.TryGetValue(w.SignatoryUserId, out var level) && levels.GetValueOrDefault(w.DelegateUserId) >= level)
            .GroupBy(w => w.SignatoryUserId)
            .ToDictionary(g => g.Key, g => g.Select(w => w.DelegateUserId).Distinct().ToList());
    }

    /// <summary>Steps filed before scope routing have no level; their first step stands in for Level 1.</summary>
    private static int Level(StepRow step) => step.SignatoryLevel ?? (step.StepOrder <= 1 ? 1 : 2);

    private static string Status(bool accepted, bool rejected) => accepted ? Sent : rejected ? Failed : Unconfirmed;

    private static DateOnly LocalDate(DateTimeOffset at, TimeSpan offset) => DateOnly.FromDateTime(at.ToOffset(offset).DateTime);

    private static string Name(IReadOnlyDictionary<long, Contact> users, long id) =>
        users.TryGetValue(id, out var u) && !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : "the employee";

    private sealed record Contact(long Id, string FullName, string? Email, string? MobileNo, bool IsActive);
}

/// <summary>An approval step whose turn it is, with the report facts a reminder needs.</summary>
public sealed record StepRow(
    long StepId,
    Guid ReportId,
    int StepOrder,
    short? SignatoryLevel,
    long ApproverUserId,
    ApprovalStatus Status,
    DateTimeOffset? ActedAt,
    string? ReferenceNo,
    long EmployeeUserId,
    DateTimeOffset? FiledAt);

/// <summary>One reminder due today: reminder <see cref="Number"/> about <see cref="Step"/> for <see cref="Audience"/>.</summary>
public sealed record ReminderPlan(StepRow Step, int Number, string Audience, IReadOnlyList<long> RecipientUserIds, int WorkingDaysWaited);

public sealed record ReminderText(string Subject, string Sms, string EmailBody);
