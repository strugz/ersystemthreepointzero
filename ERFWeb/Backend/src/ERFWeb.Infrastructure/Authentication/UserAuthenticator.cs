using ERFWeb.Application.Features.Authentication;
using ERFWeb.Domain;
using ERFWeb.Infrastructure.Persistence;
using ERFWeb.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Authentication;

/// <summary>
/// Validates credentials against the users table using PBKDF2 verification and a
/// fixed-time comparison. Lockout/rate-limiting is layered on in a later phase.
/// Roles come from the Active grants in <c>user_roles</c> plus the implicit Employee base
/// role; a Pending Activation or Revoked grant confers nothing (RBAC v0.6, spec 9.8).
/// </summary>
public sealed class UserAuthenticator(ErfDbContext db) : IUserAuthenticator
{
    public async Task<AuthenticatedUserDto?> ValidateAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var normalized = username.Trim();
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == normalized && u.IsActive, ct);

        if (user is null || !Pbkdf2PasswordHasher.Verify(user.PasswordHash, password))
        {
            return null;
        }

        var roles = await ActiveRolesAsync(user.Id, ct);
        return new AuthenticatedUserDto(user.Id, user.Username, user.FullName, roles.Select(r => r.ToString()).ToList());
    }

    /// <summary>Employee first, then the Active grants in enum order.</summary>
    public async Task<IReadOnlyList<UserRole>> ActiveRolesAsync(long userId, CancellationToken ct = default)
    {
        var granted = await db.UserRoleGrants.AsNoTracking()
            .Where(g => g.UserId == userId && g.Status == RoleGrantStatus.Active)
            .Select(g => g.Role)
            .ToListAsync(ct);

        return new[] { UserRole.Employee }.Concat(granted.Where(r => r != UserRole.Employee).OrderBy(r => r)).ToList();
    }
}
