using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.ApprovalReminders;
using ERSystem.Web.Domain.ApprovalReminders;

namespace ERSystem.Reminders.Worker;

public sealed class ApprovalReminderWorker(
    IServiceScopeFactory scopeFactory,
    ApprovalReminderSettings settings,
    IClock clock,
    ILogger<ApprovalReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Approval reminder worker started; activation polling every {PollSeconds} seconds; " +
            "scheduled reminders run at {RunTime} in {TimeZone} on day {InitialDelayDay} and every " +
            "{ReminderDay}; email {EmailState}; reminder SMS {SmsState}",
            settings.ActivationPollIntervalSeconds,
            settings.RunAtLocalTime,
            settings.TimeZoneId,
            settings.InitialDelayDays,
            settings.ReminderDayOfWeek,
            settings.EmailEnabled ? "enabled" : "disabled",
            settings.SmsEnabled ? "enabled" : "disabled");

        await Task.WhenAll(
            RunActivationLoopAsync(stoppingToken),
            RunScheduledLoopAsync(stoppingToken));
    }

    private async Task RunActivationLoopAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(settings.ActivationPollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IApprovalReminderService>();
                var summary = await service.RunActivationNotificationsAsync(stoppingToken);
                LogSummary("Activation scan", summary);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Approval activation scan failed with code ACTIVATION_SCAN_FAILED");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunScheduledLoopAsync(CancellationToken stoppingToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunUtc = ApprovalReminderSchedule.GetNextRunUtc(
                clock.UtcNow,
                timeZone,
                settings.RunAtLocalTime);
            var delay = nextRunUtc - clock.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                logger.LogInformation("Next scheduled approval reminder run is {NextRunUtc}", nextRunUtc);
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IApprovalReminderService>();
                var summary = await service.RunScheduledRemindersAsync(stoppingToken);
                LogSummary("Scheduled reminder run", summary);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled approval reminder run failed with code REMINDER_RUN_FAILED");
            }
        }
    }

    private void LogSummary(string operation, ApprovalReminderRunSummary summary)
    {
        if (summary.CandidatesFound == 0 &&
            summary.Sent == 0 &&
            summary.Queued == 0 &&
            summary.Failed == 0 &&
            summary.Skipped == 0)
        {
            logger.LogDebug("{Operation} completed with no actionable candidates", operation);
            return;
        }

        logger.LogInformation(
            "{Operation} completed: candidates {Candidates}, due {Due}, email sent {Sent}, " +
            "SMS queued {Queued}, failed {Failed}, skipped {Skipped}, already claimed {AlreadyClaimed}",
            operation,
            summary.CandidatesFound,
            summary.DueCandidates,
            summary.Sent,
            summary.Queued,
            summary.Failed,
            summary.Skipped,
            summary.AlreadyClaimed);
    }
}
