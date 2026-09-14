using ERFWeb.Application.Common;
using ERFWeb.Infrastructure.Persistence;

namespace ERFWeb.Infrastructure.Services;

/// <summary>
/// Appends a workflow audit row. Called inside the workflow transaction so the
/// audit record commits or rolls back together with the operation it describes.
/// </summary>
public sealed class WorkflowAuditWriter(ErfDbContext db, IClock clock) : IWorkflowAuditWriter
{
    public async Task WriteAsync(WorkflowAuditEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        db.WorkflowAudits.Add(new WorkflowAudit
        {
            Event = entry.Event,
            ReportId = entry.ReportId,
            ActorUserId = entry.ActorUserId,
            Detail = entry.Detail,
            OccurredAt = clock.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }
}
