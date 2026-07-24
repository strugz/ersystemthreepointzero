using ERSystem.Web.Application.Common;
using ERSystem.Web.Domain.ApprovalReminders;

namespace ERSystem.Web.Application.Features.ApprovalReminders;

public sealed class ApprovalReminderService(
    IApprovalReminderRepository repository,
    IEmailReminderSender emailSender,
    ISmsReminderSender smsSender,
    ApprovalReminderMessageFactory messageFactory,
    ApprovalReminderSettings settings,
    IClock clock) : IApprovalReminderService
{
    private const int ActivationReminderNumber = 0;

    public async Task<ApprovalReminderRunSummary> RunActivationNotificationsAsync(
        CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var candidates = await repository.GetActionableApprovalsAsync(cancellationToken);
        var nowUtc = clock.UtcNow;
        var totals = new DeliveryTotals();
        var dueCandidates = 0;

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var elapsedDays = ApprovalReminderSchedule.GetElapsedCalendarDays(
                candidate.ActiveAtUtc,
                nowUtc,
                timeZone);
            var isExpired = elapsedDays >= settings.InitialDelayDays;
            if (!isExpired)
            {
                dueCandidates++;
            }

            var skipCode = isExpired
                ? "ACTIVATION_EXPIRED"
                : settings.EmailEnabled
                    ? null
                    : "EMAIL_DISABLED";
            var messages = messageFactory.CreateActivation(candidate);

            var managerOutcome = await ProcessEmailAsync(
                candidate,
                ActivationReminderNumber,
                ReminderAudience.Manager,
                candidate.ManagerUserId,
                candidate.ManagerNotificationEmail,
                messages.ManagerEmail,
                skipCode,
                cancellationToken);
            totals.Increment(managerOutcome);

            var employeeOutcome = await ProcessEmailAsync(
                candidate,
                ActivationReminderNumber,
                ReminderAudience.Employee,
                candidate.EmployeeUserId,
                candidate.EmployeeNotificationEmail,
                messages.EmployeeEmail,
                skipCode,
                cancellationToken);
            totals.Increment(employeeOutcome);
        }

        return totals.ToSummary(candidates.Count, dueCandidates);
    }

    public async Task<ApprovalReminderRunSummary> RunScheduledRemindersAsync(
        CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var candidates = await repository.GetActionableApprovalsAsync(cancellationToken);
        var nowUtc = clock.UtcNow;
        var totals = new DeliveryTotals();
        var dueCandidates = 0;

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reminderNumber = ApprovalReminderSchedule.GetLatestDueReminderNumber(
                candidate.ActiveAtUtc,
                nowUtc,
                timeZone,
                settings.InitialDelayDays,
                settings.ReminderDayOfWeek);

            if (reminderNumber == 0)
            {
                continue;
            }

            dueCandidates++;
            if (!settings.EmailEnabled && !settings.SmsEnabled)
            {
                continue;
            }

            var elapsedDays = ApprovalReminderSchedule.GetElapsedCalendarDays(
                candidate.ActiveAtUtc,
                nowUtc,
                timeZone);
            var messages = messageFactory.CreateReminder(candidate, elapsedDays);

            if (settings.EmailEnabled)
            {
                var managerOutcome = await ProcessEmailAsync(
                    candidate,
                    reminderNumber,
                    ReminderAudience.Manager,
                    candidate.ManagerUserId,
                    candidate.ManagerNotificationEmail,
                    messages.ManagerEmail,
                    null,
                    cancellationToken);
                totals.Increment(managerOutcome);

                var employeeOutcome = await ProcessEmailAsync(
                    candidate,
                    reminderNumber,
                    ReminderAudience.Employee,
                    candidate.EmployeeUserId,
                    candidate.EmployeeNotificationEmail,
                    messages.EmployeeEmail,
                    null,
                    cancellationToken);
                totals.Increment(employeeOutcome);
            }

            if (settings.SmsEnabled)
            {
                var claim = await repository.TryClaimAsync(
                    candidate,
                    reminderNumber,
                    ReminderChannel.SmsGateway,
                    ReminderAudience.ManagerAndEmployee,
                    null,
                    Guid.NewGuid(),
                    cancellationToken);

                if (!claim.HasValue)
                {
                    totals.Increment(DeliveryOutcome.AlreadyClaimed);
                    continue;
                }

                ReminderSendResult result;
                try
                {
                    result = await smsSender.QueueAsync(candidate, messages.SmsMessage, cancellationToken);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    result = ReminderSendResult.Failed("SMS_QUEUE_UNEXPECTED");
                }

                if (result.Succeeded)
                {
                    await repository.CompleteAsync(
                        claim.Value,
                        ReminderDeliveryStatus.Queued,
                        null,
                        cancellationToken);
                    totals.Increment(DeliveryOutcome.Queued);
                }
                else
                {
                    await repository.CompleteAsync(
                        claim.Value,
                        ReminderDeliveryStatus.Failed,
                        NormalizeFailureCode(result.FailureCode, "SMS_QUEUE_FAILED"),
                        cancellationToken);
                    totals.Increment(DeliveryOutcome.Failed);
                }
            }
        }

        return totals.ToSummary(candidates.Count, dueCandidates);
    }

    private async Task<DeliveryOutcome> ProcessEmailAsync(
        ApprovalReminderCandidate candidate,
        int reminderNumber,
        ReminderAudience audience,
        int recipientUserId,
        string? recipientAddress,
        ReminderEmail message,
        string? skipCode,
        CancellationToken cancellationToken)
    {
        var claim = await repository.TryClaimAsync(
            candidate,
            reminderNumber,
            ReminderChannel.Email,
            audience,
            recipientUserId,
            Guid.NewGuid(),
            cancellationToken);

        if (!claim.HasValue)
        {
            return DeliveryOutcome.AlreadyClaimed;
        }

        if (!string.IsNullOrWhiteSpace(skipCode))
        {
            await repository.CompleteAsync(
                claim.Value,
                ReminderDeliveryStatus.Skipped,
                skipCode,
                cancellationToken);
            return DeliveryOutcome.Skipped;
        }

        if (string.IsNullOrWhiteSpace(recipientAddress))
        {
            await repository.CompleteAsync(
                claim.Value,
                ReminderDeliveryStatus.Skipped,
                "EMAIL_ADDRESS_MISSING",
                cancellationToken);
            return DeliveryOutcome.Skipped;
        }

        ReminderSendResult result;
        try
        {
            result = await emailSender.SendAsync(recipientAddress, message, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            result = ReminderSendResult.Failed("EMAIL_SEND_UNEXPECTED");
        }

        if (result.Succeeded)
        {
            await repository.CompleteAsync(
                claim.Value,
                ReminderDeliveryStatus.Sent,
                null,
                cancellationToken);
            return DeliveryOutcome.Sent;
        }

        await repository.CompleteAsync(
            claim.Value,
            ReminderDeliveryStatus.Failed,
            NormalizeFailureCode(result.FailureCode, "EMAIL_SEND_FAILED"),
            cancellationToken);
        return DeliveryOutcome.Failed;
    }

    private static string NormalizeFailureCode(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();
        var safe = new string(
            normalized.Where(character => char.IsAsciiLetterOrDigit(character) || character == '_').ToArray());
        if (string.IsNullOrEmpty(safe))
        {
            safe = fallback;
        }

        return safe.Length <= 100 ? safe : safe[..100];
    }

    private enum DeliveryOutcome
    {
        Sent,
        Queued,
        Failed,
        Skipped,
        AlreadyClaimed
    }

    private sealed class DeliveryTotals
    {
        private int Sent { get; set; }
        private int Queued { get; set; }
        private int Failed { get; set; }
        private int Skipped { get; set; }
        private int AlreadyClaimed { get; set; }

        public void Increment(DeliveryOutcome outcome)
        {
            switch (outcome)
            {
                case DeliveryOutcome.Sent:
                    Sent++;
                    break;
                case DeliveryOutcome.Queued:
                    Queued++;
                    break;
                case DeliveryOutcome.Failed:
                    Failed++;
                    break;
                case DeliveryOutcome.Skipped:
                    Skipped++;
                    break;
                case DeliveryOutcome.AlreadyClaimed:
                    AlreadyClaimed++;
                    break;
            }
        }

        public ApprovalReminderRunSummary ToSummary(int candidatesFound, int dueCandidates) =>
            new(candidatesFound, dueCandidates, Sent, Queued, Failed, Skipped, AlreadyClaimed);
    }
}
