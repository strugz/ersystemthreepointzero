using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Domain.Approval;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Whether the daily SLA reminder job is on and what its last run did. The job records every run in
/// <c>workflow_audit</c> (<see cref="ReminderAuditEvents.Run"/> or <see cref="ReminderAuditEvents.RunFailed"/>),
/// so a run that had nothing to send still shows.
/// </summary>
public sealed class ReminderStatusService(
    ErfDbContext db,
    IClock clock,
    IOptions<ReminderOptions> options,
    IOptions<ReferenceNumberOptions> calendar,
    ISmsGateway smsGateway,
    IEmailGateway emailGateway) : IReminderStatusService
{
    private static readonly JsonSerializerOptions JsonDetail = new(JsonSerializerDefaults.Web);

    public async Task<ReminderStatusDto> GetAsync(CancellationToken ct = default)
    {
        var reminders = options.Value;
        var enabled = reminders.TryGetRunAt(out var runAt) && reminders.Enabled;
        DateTimeOffset? nextRun = enabled
            ? ApprovalReminderRules.NextRun(clock.UtcNow, TimeSpan.FromHours(calendar.Value.UtcOffsetHours), runAt)
            : null;

        var last = await db.WorkflowAudits.AsNoTracking()
            .Where(a => a.Event == ReminderAuditEvents.Run || a.Event == ReminderAuditEvents.RunFailed)
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .Select(a => new { a.Event, a.OccurredAt, a.Detail })
            .FirstOrDefaultAsync(ct);

        ReminderLastRunDto? lastRun = null;
        if (last is not null)
        {
            var result = last.Event == ReminderAuditEvents.Run && last.Detail is { Length: > 0 } detail
                ? JsonSerializer.Deserialize<ReminderRunResult>(detail, JsonDetail)
                : null;
            lastRun = new ReminderLastRunDto(
                last.OccurredAt, last.Event == ReminderAuditEvents.Run, result?.StepsWaiting, result?.Sent, result?.Failed, result?.Skipped);
        }

        return new ReminderStatusDto(enabled, reminders.RunAt, emailGateway.IsConfigured, smsGateway.IsConfigured, nextRun, lastRun);
    }
}
