using ERFWeb.Application.Features.Authentication;
using ERFWeb.Infrastructure.Persistence;
using ERFWeb.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Authentication;

/// <summary>
/// Validates credentials against the users table using PBKDF2 verification and a
/// fixed-time comparison. Lockout/rate-limiting is layered on in a later phase.
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

        return new AuthenticatedUserDto(user.Id, user.Username, user.FullName, [user.Role.ToString()]);
    }
}
