using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>
/// Read-only view of the audit trail (spec 9.9 and 9.10). The rows are append-only; there is no
/// edit or delete path anywhere in the application. Finance uses this view to watch the System
/// Administrator's account and role activity.
/// </summary>
public sealed class AdminAuditService(ErfDbContext db) : IAdminAuditService
{
    public async Task<PagedResult<AdminAuditEntryDto>> ListAsync(AdminAuditQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = db.WorkflowAudits.AsNoTracking().AsQueryable();
        if (query.AdministrationOnly)
        {
            rows = rows.Where(a => a.Event.StartsWith(AdminAuditEvents.Prefix));
        }

        if (!string.IsNullOrWhiteSpace(query.Event))
        {
            var ev = query.Event.Trim();
            rows = rows.Where(a => a.Event == ev);
        }

        if (query.ActorUserId is { } actor)
        {
            rows = rows.Where(a => a.ActorUserId == actor);
        }

        if (query.From is { } from)
        {
            rows = rows.Where(a => a.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            rows = rows.Where(a => a.OccurredAt < to);
        }

        var total = await rows.CountAsync(ct);
        var ordered = query.Direction == SortDirection.Asc
            ? rows.OrderBy(a => a.OccurredAt).ThenBy(a => a.Id)
            : rows.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id);

        var items = await (
            from a in ordered.Skip(query.Skip).Take(query.NormalizedPageSize)
            join uj in db.Users.AsNoTracking() on a.ActorUserId equals (long?)uj.Id into us
            from u in us.DefaultIfEmpty()
            select new AdminAuditEntryDto(
                a.Id, a.Event, a.ActorUserId, u != null ? u.FullName : null, a.ReportId, a.OccurredAt, a.Detail, a.CorrelationId))
            .ToListAsync(ct);

        return new PagedResult<AdminAuditEntryDto>(items, total, query.NormalizedPage, query.NormalizedPageSize);
    }
}
