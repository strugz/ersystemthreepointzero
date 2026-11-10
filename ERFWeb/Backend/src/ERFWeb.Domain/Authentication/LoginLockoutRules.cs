namespace ERFWeb.Domain.Authentication;

/// <summary>How many failed sign-ins in a row lock a username, and for how long (Rules page).</summary>
public sealed record LoginLockoutPolicy(int MaxFailedAttempts, int LockoutMinutes)
{
    public static readonly LoginLockoutPolicy Default = new(5, 15);

    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(LockoutMinutes);
}

/// <summary>
/// Sign-in lockout (spec section 9): after <see cref="LoginLockoutPolicy.MaxFailedAttempts"/> failed attempts in a
/// row a username is locked for <see cref="LoginLockoutPolicy.LockoutMinutes"/> (five and fifteen unless the System
/// Administrator changes them). Unknown usernames are counted the same way, so a lockout never reveals whether an
/// account exists. Failures older than the lockout window no longer count.
/// </summary>
public static class LoginLockoutRules
{
    public const int MinAttempts = 3;
    public const int MaxAttempts = 20;
    public const int MinMinutes = 1;
    public const int MaxMinutes = 1440;

    public static bool IsLocked(DateTimeOffset? lockoutUntil, DateTimeOffset now) => lockoutUntil > now;

    /// <summary>The attempt count and lock end to store after a failed sign-in.</summary>
    public static (int FailedAttempts, DateTimeOffset? LockoutUntil) AfterFailure(
        int failedAttempts, DateTimeOffset? lastAttemptAt, DateTimeOffset now, LoginLockoutPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // A burst that went quiet for a whole window (or an expired lock) starts counting again.
        var previous = lastAttemptAt is { } last && now - last < policy.LockoutDuration ? failedAttempts : 0;
        var count = previous + 1;
        return count >= policy.MaxFailedAttempts ? (0, now + policy.LockoutDuration) : (count, null);
    }

    /// <summary>Whole minutes left on a lock, at least one, for the "try again in N minutes" message.</summary>
    public static int MinutesLeft(DateTimeOffset lockoutUntil, DateTimeOffset now) =>
        Math.Max(1, (int)Math.Ceiling((lockoutUntil - now).TotalMinutes));

    /// <summary>Field errors for a policy the administrator wants to save; a lockout can be tuned, not switched off.</summary>
    public static IReadOnlyList<(string Field, string Message)> Validate(int maxFailedAttempts, int lockoutMinutes)
    {
        var errors = new List<(string, string)>();
        if (maxFailedAttempts is < MinAttempts or > MaxAttempts)
        {
            errors.Add(("loginMaxFailedAttempts", $"Enter {MinAttempts} to {MaxAttempts} attempts."));
        }

        if (lockoutMinutes is < MinMinutes or > MaxMinutes)
        {
            errors.Add(("loginLockoutMinutes", $"Enter {MinMinutes} to {MaxMinutes} minutes."));
        }

        return errors;
    }

    /// <summary>The stored values, each falling back to the default when missing or out of range.</summary>
    public static LoginLockoutPolicy Parse(int? maxFailedAttempts, int? lockoutMinutes) => new(
        maxFailedAttempts is >= MinAttempts and <= MaxAttempts ? maxFailedAttempts.Value : LoginLockoutPolicy.Default.MaxFailedAttempts,
        lockoutMinutes is >= MinMinutes and <= MaxMinutes ? lockoutMinutes.Value : LoginLockoutPolicy.Default.LockoutMinutes);
}
