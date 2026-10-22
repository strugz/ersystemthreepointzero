using ERFWeb.Domain.Administration;
using ERFWeb.Domain.Authentication;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class LoginLockoutRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FifthFailureInARow_LocksForFifteenMinutes()
    {
        int count = 0;
        DateTimeOffset? last = null;
        for (var attempt = 1; attempt < LoginLockoutRules.MaxFailedAttempts; attempt++)
        {
            var (next, lockUntil) = LoginLockoutRules.AfterFailure(count, last, Now.AddSeconds(attempt));
            Assert.Null(lockUntil);
            Assert.Equal(attempt, next);
            (count, last) = (next, Now.AddSeconds(attempt));
        }

        var (afterLock, lockedUntil) = LoginLockoutRules.AfterFailure(count, last, Now.AddSeconds(5));
        Assert.Equal(0, afterLock);
        Assert.Equal(Now.AddSeconds(5) + LoginLockoutRules.LockoutDuration, lockedUntil);
    }

    [Fact]
    public void FailuresOlderThanTheWindow_NoLongerCount()
    {
        var (count, lockUntil) = LoginLockoutRules.AfterFailure(4, Now, Now + LoginLockoutRules.LockoutDuration);

        Assert.Equal(1, count);
        Assert.Null(lockUntil);
    }

    [Fact]
    public void Lock_HoldsUntilItsEnd()
    {
        var until = Now.AddMinutes(15);

        Assert.True(LoginLockoutRules.IsLocked(until, Now));
        Assert.False(LoginLockoutRules.IsLocked(until, until));
        Assert.False(LoginLockoutRules.IsLocked(null, Now));
    }

    [Theory]
    [InlineData(900, 15)]
    [InlineData(61, 2)]
    [InlineData(1, 1)]
    public void MinutesLeft_RoundsUpToAtLeastOne(int secondsLeft, int expected) =>
        Assert.Equal(expected, LoginLockoutRules.MinutesLeft(Now.AddSeconds(secondsLeft), Now));

    [Theory]
    [InlineData("short", "Password must be at least 8 characters.")]
    [InlineData("Employee1", "Password cannot be the same as your username.")]
    [InlineData("Current!123", "Choose a password different from your current one.")]
    public void NewPassword_IsRefused(string newPassword, string expected) =>
        Assert.Equal(expected, AccountRules.NewPasswordError(newPassword, "employee1", "Current!123"));

    [Fact]
    public void NewPassword_IsAccepted() =>
        Assert.Null(AccountRules.NewPasswordError("A-new-pass-2026", "employee1", "Current!123"));
}
