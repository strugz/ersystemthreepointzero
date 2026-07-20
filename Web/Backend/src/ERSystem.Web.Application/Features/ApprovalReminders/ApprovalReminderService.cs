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
    public async Task<ApprovalReminderRunSummary> RunAsync(CancellationToken cancellationToken)
    {
        if (!settings.EmailEnabled && !settings.SmsEnabled)
        {
            return new ApprovalReminderRunSummary(0, 0, 0, 0, 0, 0, 0);
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        var candidates = await repository.GetActionableApprovalsAsync(cancellationToken);
        var sent = 0;
        var queued = 0;
        var failed = 0;
        var skipped = 0;
        var alreadyClaimed = 0;
        var dueCandidates = 0;

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reminderNumber = ApprovalReminderSchedule.GetDueReminderNumber(
                candidate.ActiveAtUtc,
                clock.UtcNow,
                timeZone,
                settings.InitialDelayDays,
                settings.RepeatIntervalDays);

            if (reminderNumber == 0)
            {
                continue;
            }

            dueCandidates++;
            var elapsedDays = GetElapsedLocalDays(candidate.ActiveAtUtc, clock.UtcNow, timeZone);
            var messages = messageFactory.Create(candidate, elapsedDays);

            if (settings.EmailEnabled)
            {
                var managerOutcome = await ProcessEmailAsync(
                    candidate,
                    reminderNumber,
                    ReminderAudience.Manager,
                    candidate.ManagerUserId,
                    candidate.ManagerNotificationEmail,
                    messages.ManagerEmail,
                    cancellationToken);
                Increment(managerOutcome, ref sent, ref failed, ref skipped, ref alreadyClaimed);

                var employeeOutcome = await ProcessEmailAsync(
                    candidate,
                    reminderNumber,
                    ReminderAudience.Employee,
                    candidate.EmployeeUserId,
                    candidate.EmployeeNotificationEmail,
                    messages.EmployeeEmail,
                    cancellationToken);
                Increment(employeeOutcome, ref sent, ref failed, ref skipped, ref alreadyClaimed);
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
                    alreadyClaimed++;
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
                    await repository.CompleteAsync(claim.Value, ReminderDeliveryStatus.Queued, null, cancellationToken);
                    queued++;
                }
                else
                {
                    await repository.CompleteAsync(
                        claim.Value,
                        ReminderDeliveryStatus.Failed,
                        NormalizeFailureCode(result.FailureCode, "SMS_QUEUE_FAILED"),
                        cancellationToken);
                    failed++;
                }
            }
        }

        return new ApprovalReminderRunSummary(
            candidates.Count,
            dueCandidates,
            sent,
            queued,
            failed,
            skipped,
            alreadyClaimed);
    }

    private async Task<DeliveryOutcome> ProcessEmailAsync(
        ApprovalReminderCandidate candidate,
        int reminderNumber,
        ReminderAudience audience,
        int recipientUserId,
        string? recipientAddress,
        ReminderEmail message,
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
            await repository.CompleteAsync(claim.Value, ReminderDeliveryStatus.Sent, null, cancellationToken);
            return DeliveryOutcome.Sent;
        }
        else
        {
            await repository.CompleteAsync(
                claim.Value,
                ReminderDeliveryStatus.Failed,
                NormalizeFailureCode(result.FailureCode, "EMAIL_SEND_FAILED"),
                cancellationToken);
            return DeliveryOutcome.Failed;
        }
    }

    private static void Increment(
        DeliveryOutcome outcome,
        ref int sent,
        ref int failed,
        ref int skipped,
        ref int alreadyClaimed)
    {
        switch (outcome)
        {
            case DeliveryOutcome.Sent:
                sent++;
                break;
            case DeliveryOutcome.Failed:
                failed++;
                break;
            case DeliveryOutcome.Skipped:
                skipped++;
                break;
            case DeliveryOutcome.AlreadyClaimed:
                alreadyClaimed++;
                break;
        }
    }

    private static int GetElapsedLocalDays(DateTime activeAtUtc, DateTime nowUtc, TimeZoneInfo timeZone)
    {
        var activeDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(activeAtUtc, DateTimeKind.Utc), timeZone).Date;
        var currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), timeZone).Date;
        return Math.Max(0, (currentDate - activeDate).Days);
    }

    private static string NormalizeFailureCode(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();
        var safe = new string(normalized.Where(character => char.IsAsciiLetterOrDigit(character) || character == '_').ToArray());
        if (string.IsNullOrEmpty(safe))
        {
            safe = fallback;
        }

        return safe.Length <= 100 ? safe : safe[..100];
    }

    private enum DeliveryOutcome
    {
        Sent,
        Failed,
        Skipped,
        AlreadyClaimed
    }
}
