using System.Security.Cryptography;
using System.Text;
using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.Authentication;
using ERSystem.Web.Infrastructure.Persistence;
using ERSystem.Web.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ERSystem.Web.Infrastructure.Authentication;

public sealed class LegacyAuthenticationService(
    IDbContextFactory<LegacyErDbContext> legacyFactory,
    IDbContextFactory<WebWorkflowDbContext> workflowFactory,
    LegacyPasswordCipher cipher,
    IClock clock) : IAuthenticationService
{
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<AuthenticatedUserDto?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = TextNormalization.NormalizeUsername(request.Username);
        var password = request.Password ?? string.Empty;
        if (username.Length is 0 or > 50 || password.Length is 0 or > 512) return null;

        await using var legacy = await legacyFactory.CreateDbContextAsync(cancellationToken);
        var user = await legacy.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Username != null && x.Username.Trim().ToUpper() == username, cancellationToken);
        if (user?.UserId is not int userId) return null;

        await using var workflow = await workflowFactory.CreateDbContextAsync(cancellationToken);
        var security = await workflow.LoginSecurity.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? new WebLoginSecurityEntity { UserId = userId };
        if (security.LockoutEndUtc > clock.UtcNow) return null;

        var encrypted = cipher.Encrypt(password);
        if (!FixedTimeEquals(encrypted, user.Password))
        {
            RegisterFailure(security);
            if (workflow.Entry(security).State == EntityState.Detached) workflow.LoginSecurity.Add(security);
            await workflow.SaveChangesAsync(cancellationToken);
            return null;
        }

        security.FailedAttemptCount = 0;
        security.FirstFailedAttemptUtc = null;
        security.LockoutEndUtc = null;
        security.LastSuccessfulLoginUtc = clock.UtcNow;
        if (workflow.Entry(security).State == EntityState.Detached) workflow.LoginSecurity.Add(security);
        await workflow.SaveChangesAsync(cancellationToken);

        var roles = new List<string>();
        if (string.Equals(user.UserLevel?.Trim(), "Finance", StringComparison.OrdinalIgnoreCase)) roles.Add("Finance");
        if (await legacy.UserAuthorities.AsNoTracking().AnyAsync(x => x.AuthorityId == userId, cancellationToken)) roles.Add("Manager");

        return new AuthenticatedUserDto(userId, username, user.FullName?.Trim() ?? username, user.UserLevel?.Trim() ?? string.Empty, roles);
    }

    private void RegisterFailure(WebLoginSecurityEntity security)
    {
        if (!security.FirstFailedAttemptUtc.HasValue || clock.UtcNow - security.FirstFailedAttemptUtc.Value > FailureWindow)
        {
            security.FirstFailedAttemptUtc = clock.UtcNow;
            security.FailedAttemptCount = 1;
            security.LockoutEndUtc = null;
            return;
        }

        security.FailedAttemptCount++;
        if (security.FailedAttemptCount >= 5) security.LockoutEndUtc = clock.UtcNow.Add(LockoutDuration);
    }

    private static bool FixedTimeEquals(string value, string? expected)
    {
        if (expected is null) return false;
        var left = Encoding.UTF8.GetBytes(value);
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
