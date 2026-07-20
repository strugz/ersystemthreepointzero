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
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        logger.LogInformation(
            "Approval reminder worker started; schedule {RunTime} in {TimeZone}; email {EmailState}; SMS {SmsState}",
            settings.RunAtLocalTime,
            settings.TimeZoneId,
            settings.EmailEnabled ? "enabled" : "disabled",
            settings.SmsEnabled ? "enabled" : "disabled");

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunUtc = ApprovalReminderSchedule.GetNextRunUtc(
                clock.UtcNow,
                timeZone,
                settings.RunAtLocalTime);
            var delay = nextRunUtc - clock.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                logger.LogInformation("Next approval reminder run is scheduled for {NextRunUtc}", nextRunUtc);
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IApprovalReminderService>();
                var summary = await service.RunAsync(stoppingToken);
                logger.LogInformation(
                    "Approval reminder run completed: candidates {Candidates}, due {Due}, email sent {Sent}, " +
                    "SMS queued {Queued}, failed {Failed}, skipped {Skipped}, already claimed {AlreadyClaimed}",
                    summary.CandidatesFound,
                    summary.DueCandidates,
                    summary.Sent,
                    summary.Queued,
                    summary.Failed,
                    summary.Skipped,
                    summary.AlreadyClaimed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Approval reminder run failed with code REMINDER_RUN_FAILED");
            }
        }
    }
}
