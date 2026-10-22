using ERFWeb.Application.Common;
using ERFWeb.Domain;
using ERFWeb.Domain.Rates;
using ERFWeb.Domain.Scope;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Entitlements;

/// <summary>An active account that can be routed or rated.</summary>
public sealed record EntitlementSubject(long UserId, string FullName, string? JobGrade, long? DepartmentId, string? DepartmentName)
{
    public ScopeSubject Scope => new(UserId, DepartmentId);

    public RateSubject Rate => new(UserId, JobGrade, DepartmentId);
}

/// <summary>
/// Loads the data the scope and rate rules work on. Everything is read in full: the company has
/// hundreds of employees at most, and resolving in memory keeps the rules pure and testable.
/// </summary>
public sealed class EntitlementContext(ErfDbContext db, ICurrentUser user, IClock clock, IOptions<ReferenceNumberOptions> calendar)
{
    /// <summary>The business date, in the same local offset that dates ERF reference numbers.</summary>
    public DateOnly Today => DateOnly.FromDateTime(clock.UtcNow.ToOffset(TimeSpan.FromHours(calendar.Value.UtcOffsetHours)).DateTime);

    public async Task<EntitlementActor> ActorAsync(CancellationToken ct)
    {
        var profile = await db.Users.AsNoTracking()
            .Where(u => u.Id == user.UserId)
            .Select(u => new { u.JobGrade, u.DepartmentId })
            .FirstOrDefaultAsync(ct);
        return new EntitlementActor(user.UserId, new HashSet<UserRole>(user.Roles), profile?.JobGrade, profile?.DepartmentId);
    }

    /// <summary>Whether the account holds an Active Signatory grant at either level.</summary>
    public Task<bool> IsSignatoryAsync(long userId, CancellationToken ct) =>
        db.UserRoleGrants.AsNoTracking().AnyAsync(g => g.UserId == userId
            && g.Status == RoleGrantStatus.Active
            && (g.Role == UserRole.SignatoryLevel1 || g.Role == UserRole.SignatoryLevel2), ct);

    /// <summary>
    /// Scope assignments as resolver candidates. With <paramref name="validHoldersOnly"/>, an assignment
    /// counts only while its holder is an active account with the Active Signatory grant for its level,
    /// so revoking a role or deactivating an account takes the holder out of routing at once.
    /// </summary>
    public async Task<List<ScopeCandidate>> ScopeCandidatesAsync(bool validHoldersOnly, CancellationToken ct)
    {
        var rows = await db.ScopeAssignments.AsNoTracking().ToListAsync(ct);
        var members = await db.ScopeAssignmentMembers.AsNoTracking().ToListAsync(ct);
        var byAssignment = members.GroupBy(m => m.ScopeAssignmentId).ToDictionary(g => g.Key, g => (IReadOnlySet<long>)g.Select(m => m.UserId).ToHashSet());

        HashSet<(long UserId, int Level)>? valid = null;
        if (validHoldersOnly)
        {
            var grants = await (
                from g in db.UserRoleGrants.AsNoTracking()
                join u in db.Users.AsNoTracking() on g.UserId equals u.Id
                where u.IsActive && g.Status == RoleGrantStatus.Active
                    && (g.Role == UserRole.SignatoryLevel1 || g.Role == UserRole.SignatoryLevel2)
                select new { g.UserId, g.Role })
                .ToListAsync(ct);
            valid = grants.Select(g => (g.UserId, g.Role == UserRole.SignatoryLevel1 ? 1 : 2)).ToHashSet();
        }

        return rows
            .Where(r => valid is null || valid.Contains((r.HolderUserId, r.SignatoryLevel)))
            .Select(r => ToCandidate(r, byAssignment.TryGetValue(r.Id, out var m) ? m : new HashSet<long>()))
            .ToList();
    }

    public static ScopeCandidate ToCandidate(ScopeAssignment r, IReadOnlySet<long> members) =>
        new(r.Id, r.HolderUserId, r.SignatoryLevel, r.AssignmentType, r.DepartmentId, members,
            r.EffectiveFrom, r.EffectiveTo, r.IsActive, r.ApprovalLimit);

    /// <summary>Active accounts that file ERFs. System Administrators never file, so they are left out.</summary>
    public async Task<List<EntitlementSubject>> SubjectsAsync(CancellationToken ct)
    {
        var admins = db.UserRoleGrants.AsNoTracking()
            .Where(g => g.Role == UserRole.SystemAdministrator && g.Status == RoleGrantStatus.Active)
            .Select(g => g.UserId);

        return await (
            from u in db.Users.AsNoTracking()
            where u.IsActive && !admins.Contains(u.Id)
            join dj in db.Departments.AsNoTracking() on u.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            orderby u.FullName
            select new EntitlementSubject(u.Id, u.FullName, u.JobGrade, u.DepartmentId, d != null ? d.Name : null))
            .ToListAsync(ct);
    }

    public async Task<EntitlementSubject?> SubjectAsync(long userId, CancellationToken ct) =>
        await (
            from u in db.Users.AsNoTracking()
            where u.Id == userId
            join dj in db.Departments.AsNoTracking() on u.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            select new EntitlementSubject(u.Id, u.FullName, u.JobGrade, u.DepartmentId, d != null ? d.Name : null))
            .FirstOrDefaultAsync(ct);

    public async Task<List<RateAssignmentCandidate>> RateAssignmentsAsync(CancellationToken ct) =>
        await (
            from a in db.RateProfileAssignments.AsNoTracking()
            join p in db.RateProfiles.AsNoTracking() on a.RateProfileId equals p.Id
            where p.IsActive
            select new RateAssignmentCandidate(a.Id, a.RateProfileId, a.TargetType, a.UserId, a.JobGrade, a.DepartmentId, a.EffectiveFrom, a.EffectiveTo))
            .ToListAsync(ct);

    public async Task<List<RateLineCandidate>> RateLinesAsync(long profileId, CancellationToken ct) =>
        await db.RateProfileLines.AsNoTracking()
            .Where(l => l.RateProfileId == profileId)
            .Select(l => new RateLineCandidate(l.Id, l.RateProfileId, l.Category, l.SubCategory, l.RateType, l.Amount, l.RequiresReceipt, l.EffectiveFrom, l.EffectiveTo))
            .ToListAsync(ct);
}
