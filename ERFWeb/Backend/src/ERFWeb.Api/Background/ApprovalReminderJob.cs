using System.Globalization;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Domain.Approval;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Notifications;
using Microsoft.Extensions.Options;

namespace ERFWeb.Api.Background;

/// <summary>
/// Runs <see cref="IApprovalReminderService"/> once a working day at <c>Reminders:RunAt</c> (business
/// time zone). Hosted inside the API service so there is nothing extra to install. A service started
/// after the run time on a working day runs at once; the service's once-per-reminder claims make a
/// second run that day harmless. A failed run is logged and the next day's run goes ahead.
/// </summary>
public sealed class ApprovalReminderJob(
    IServiceScopeFactory scopes,
    IOptions<ReminderOptions> options,
    IOptions<ReferenceNumberOptions> calendar,
    ILogger<ApprovalReminderJob> logger) : BackgroundService
{
    private readonly TimeProvider time = TimeProvider.System;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("SLA reminders are off (Reminders:Enabled is not true).");
            return;
        }

        if (!TimeOnly.TryParseExact(options.Value.RunAt, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var runAt))
        {
            logger.LogError("SLA reminders are off: Reminders:RunAt must be a time such as 08:00.");
            return;
        }

        var offset = TimeSpan.FromHours(calendar.Value.UtcOffsetHours);
        if (ApprovalReminderRules.RunDueToday(time.GetUtcNow(), offset, runAt))
        {
            await RunOnceAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var next = ApprovalReminderRules.NextRun(time.GetUtcNow(), offset, runAt);
            logger.LogInformation("Next SLA reminder run at {NextRun:yyyy-MM-dd HH:mm zzz}.", next);
            try
            {
                await Task.Delay(next - time.GetUtcNow(), time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IApprovalReminderService>().RunAsync(ct);
            logger.LogInformation(
                "SLA reminder run: {StepsWaiting} step(s) waiting, {Sent} sent, {Failed} failed, {Skipped} skipped (no contact).",
                result.StepsWaiting, result.Sent, result.Failed, result.Skipped);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SLA reminder run failed; the next run goes ahead as scheduled.");
        }
    }
}
