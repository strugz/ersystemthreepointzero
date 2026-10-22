namespace ERFWeb.Domain.Authentication;

/// <summary>
/// Sign-in lockout (spec section 9): after five failed attempts in a row a username is locked for
/// fifteen minutes. Unknown usernames are counted the same way, so a lockout never reveals whether an
/// account exists. Failures older than the lockout window no longer count.
/// </summary>
public static class LoginLockoutRules
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static bool IsLocked(DateTimeOffset? lockoutUntil, DateTimeOffset now) => lockoutUntil > now;

    /// <summary>The attempt count and lock end to store after a failed sign-in.</summary>
    public static (int FailedAttempts, DateTimeOffset? LockoutUntil) AfterFailure(
        int failedAttempts, DateTimeOffset? lastAttemptAt, DateTimeOffset now)
    {
        // A burst that went quiet for a whole window (or an expired lock) starts counting again.
        var previous = lastAttemptAt is { } last && now - last < LockoutDuration ? failedAttempts : 0;
        var count = previous + 1;
        return count >= MaxFailedAttempts ? (0, now + LockoutDuration) : (count, null);
    }

    /// <summary>Whole minutes left on a lock, at least one, for the "try again in N minutes" message.</summary>
    public static int MinutesLeft(DateTimeOffset lockoutUntil, DateTimeOffset now) =>
        Math.Max(1, (int)Math.Ceiling((lockoutUntil - now).TotalMinutes));
}
