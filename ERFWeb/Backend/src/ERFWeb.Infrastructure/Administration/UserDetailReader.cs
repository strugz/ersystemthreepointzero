using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain;
using ERFWeb.Domain.Administration;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>Shared read model for one account with its role grants (used after every admin mutation).</summary>
public sealed class UserDetailReader(ErfDbContext db, ISystemSettingsService settings, IClock clock)
{
    public async Task<UserDetailDto> ReadAsync(long userId, CancellationToken ct)
    {
        var row = await (
            from u in db.Users.AsNoTracking()
            where u.Id == userId
            join dj in db.Departments.AsNoTracking() on u.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            select new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.Position,
                u.JobGrade,
                u.Email,
                u.MobileNo,
                u.DepartmentId,
                DepartmentName = d != null ? d.Name : null,
                u.CostCenterId,
                u.IsActive,
                HasSignature = u.Signature != null && u.Signature.Length > 0,
                u.WorkWithEnabled,
                u.CreatedAt,
                RowVersion = EF.Property<uint>(u, DbContextExtensions.RowVersionProperty),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User not found.");

        var grants = await (
            from g in db.UserRoleGrants.AsNoTracking()
            where g.UserId == userId
            join gb in db.Users.AsNoTracking() on g.GrantedByUserId equals (long?)gb.Id into gbs
            from grantedBy in gbs.DefaultIfEmpty()
            join cb in db.Users.AsNoTracking() on g.CountersignedByUserId equals (long?)cb.Id into cbs
            from countersignedBy in cbs.DefaultIfEmpty()
            join rb in db.Users.AsNoTracking() on g.RevokedByUserId equals (long?)rb.Id into rbs
            from revokedBy in rbs.DefaultIfEmpty()
            orderby g.Role
            select new RoleGrantDto(
                g.Role,
                g.Status,
                g.GrantedByUserId,
                grantedBy != null ? grantedBy.FullName : null,
                g.GrantedAt,
                g.CountersignedByUserId,
                countersignedBy != null ? countersignedBy.FullName : null,
                g.CountersignedAt,
                g.RevokedByUserId,
                revokedBy != null ? revokedBy.FullName : null,
                g.RevokedAt,
                g.IsBootstrap,
                g.Note,
                EF.Property<uint>(g, DbContextExtensions.RowVersionProperty)))
            .ToListAsync(ct);

        var coolingDays = await settings.GetCoolingPeriodDaysAsync(ct);

        return new UserDetailDto(
            row.Id,
            row.Username,
            row.FullName,
            row.Position,
            row.JobGrade,
            row.Email,
            row.MobileNo,
            row.DepartmentId,
            row.DepartmentName,
            row.CostCenterId,
            row.IsActive,
            row.HasSignature,
            row.WorkWithEnabled,
            row.CreatedAt,
            AccountRules.CoolingPeriodElapsed(row.CreatedAt, clock.UtcNow, coolingDays),
            grants,
            row.RowVersion);
    }

    /// <summary>Roles an account holds that count for stacking: every grant not revoked.</summary>
    public Task<List<UserRole>> HeldRolesAsync(long userId, CancellationToken ct) =>
        db.UserRoleGrants.AsNoTracking()
            .Where(g => g.UserId == userId && g.Status != RoleGrantStatus.Revoked)
            .Select(g => g.Role)
            .ToListAsync(ct);
}
