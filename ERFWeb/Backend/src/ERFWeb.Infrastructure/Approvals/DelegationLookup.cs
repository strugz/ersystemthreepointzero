using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Infrastructure.Entitlements;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Approvals;

/// <summary>
/// Resolves the signatories the current user stands in for today. The level rule is checked again
/// here, not only when the delegation was made: if the delegate has since lost the level (or either
/// account was deactivated), the delegation stops working at once. Cached per request.
/// </summary>
public sealed class DelegationLookup(ErfDbContext db, ICurrentUser user, EntitlementContext context) : IDelegationLookup
{
    private IReadOnlyDictionary<long, long>? cached;

    public async Task<IReadOnlyDictionary<long, long>> ActingForAsync(CancellationToken ct = default)
    {
        if (cached is not null)
        {
            return cached;
        }

        if (!user.IsAuthenticated)
        {
            return cached = new Dictionary<long, long>();
        }

        var me = user.UserId;
        var today = context.Today;
        var windows = await db.ApprovalDelegations.AsNoTracking()
            .Where(d => d.DelegateUserId == me && d.CancelledAt == null && d.StartsOn <= today && d.EndsOn >= today)
            .Select(d => new DelegationWindow(d.Id, d.SignatoryUserId, d.DelegateUserId, d.StartsOn, d.EndsOn, false))
            .ToListAsync(ct);
        if (windows.Count == 0)
        {
            return cached = new Dictionary<long, long>();
        }

        var levels = await SignatoryLevelsAsync(db, windows.Select(w => w.SignatoryUserId).Append(me), ct);
        var myLevel = levels.GetValueOrDefault(me);
        var valid = windows.Where(w => levels.TryGetValue(w.SignatoryUserId, out var level) && myLevel >= level);
        return cached = DelegationRules.ActingFor(valid, me, today);
    }

    /// <summary>The highest Active signatory level of each active account; accounts without one are left out.</summary>
    public static async Task<Dictionary<long, int>> SignatoryLevelsAsync(ErfDbContext db, IEnumerable<long>? userIds, CancellationToken ct)
    {
        var grants =
            from g in db.UserRoleGrants.AsNoTracking()
            join u in db.Users.AsNoTracking() on g.UserId equals u.Id
            where u.IsActive && g.Status == RoleGrantStatus.Active
                && (g.Role == UserRole.SignatoryLevel1 || g.Role == UserRole.SignatoryLevel2)
            select new { g.UserId, g.Role };
        if (userIds is not null)
        {
            var ids = userIds.Distinct().ToList();
            grants = grants.Where(g => ids.Contains(g.UserId));
        }

        var rows = await grants.ToListAsync(ct);
        return rows
            .GroupBy(g => g.UserId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.Role == UserRole.SignatoryLevel2 ? 2 : 1));
    }
}
