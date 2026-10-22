using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Authentication;
using ERFWeb.Domain;
using ERFWeb.Domain.Administration;
using ERFWeb.Domain.Authentication;
using ERFWeb.Infrastructure.Persistence;
using ERFWeb.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Authentication;

/// <summary>
/// Validates credentials against the users table using PBKDF2 verification and a fixed-time comparison,
/// with the sign-in lockout of <see cref="LoginLockoutRules"/> kept in <c>login_security</c> (one row per
/// username tried, known or not). Roles come from the Active grants in <c>user_roles</c> plus the implicit
/// Employee base role; a Pending Activation or Revoked grant confers nothing (RBAC v0.6, spec 9.8).
/// </summary>
public sealed class UserAuthenticator(ErfDbContext db, IClock clock, IWorkflowAuditWriter audit) : IUserAuthenticator
{
    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var normalized = username?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || string.IsNullOrEmpty(password))
        {
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        // Longer than any username can be: refuse without keeping a lockout row for it.
        var tracked = normalized.Length <= AccountRules.MaxUsernameLength;
        var now = clock.UtcNow;
        var security = tracked ? await db.LoginSecurities.FirstOrDefaultAsync(l => l.Username == normalized, ct) : null;
        if (security is { LockoutUntil: { } lockedUntil } && LoginLockoutRules.IsLocked(lockedUntil, now))
        {
            return new LoginResult(LoginOutcome.LockedOut, LockedUntil: lockedUntil);
        }

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == normalized && u.IsActive, ct);

        if (user is null || !Pbkdf2PasswordHasher.Verify(user.PasswordHash, password))
        {
            if (!tracked)
            {
                return new LoginResult(LoginOutcome.InvalidCredentials);
            }

            var lockedNow = await RecordFailureAsync(normalized, security, now, ct);
            return lockedNow is { } until
                ? new LoginResult(LoginOutcome.LockedOut, LockedUntil: until)
                : new LoginResult(LoginOutcome.InvalidCredentials);
        }

        if (security is not null)
        {
            db.LoginSecurities.Remove(security);
            await db.SaveChangesAsync(ct);
        }

        var roles = await ActiveRolesAsync(user.Id, ct);
        return new LoginResult(
            LoginOutcome.Success,
            new AuthenticatedUserDto(user.Id, user.Username, user.FullName, roles.Select(r => r.ToString()).ToList(), user.MustChangePassword));
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct)
            ?? throw new NotFoundException("Your account could not be found.");

        if (string.IsNullOrEmpty(request.CurrentPassword) || !Pbkdf2PasswordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            throw new ValidationException("currentPassword", "Your current password is not correct.");
        }

        if (AccountRules.NewPasswordError(request.NewPassword, user.Username, request.CurrentPassword) is { } passwordError)
        {
            throw new ValidationException("newPassword", passwordError);
        }

        user.PasswordHash = Pbkdf2PasswordHasher.Hash(request.NewPassword!);
        user.MustChangePassword = false;
        await db.SaveChangesAsync(ct);

        // Who and when only; never the password or its hash.
        await audit.WriteAsync(
            new WorkflowAuditEntry(AuthAuditEvents.PasswordChanged, null, userId, JsonSerializer.Serialize(new { userId, user.Username })), ct);
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

    /// <summary>Counts a failed attempt; returns the lock end when this attempt locks the username.</summary>
    private async Task<DateTimeOffset?> RecordFailureAsync(string username, LoginSecurity? security, DateTimeOffset now, CancellationToken ct)
    {
        var (failedAttempts, lockoutUntil) = LoginLockoutRules.AfterFailure(security?.FailedAttempts ?? 0, security?.LastAttemptAt, now);
        if (security is null)
        {
            security = new LoginSecurity { Username = username };
            db.LoginSecurities.Add(security);
        }

        security.FailedAttempts = failedAttempts;
        security.LockoutUntil = lockoutUntil;
        security.LastAttemptAt = now;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // A simultaneous failed attempt created the row first; that attempt is counted, this one is not.
            db.Entry(security).State = EntityState.Detached;
            return null;
        }

        if (lockoutUntil is not null)
        {
            // The username tried is recorded (it may not exist); the password never is.
            await audit.WriteAsync(
                new WorkflowAuditEntry(AuthAuditEvents.LockedOut, null, null, JsonSerializer.Serialize(new { username, lockoutUntil })), ct);
        }

        return lockoutUntil;
    }
}
