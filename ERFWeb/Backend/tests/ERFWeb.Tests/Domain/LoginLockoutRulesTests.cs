using ERFWeb.Domain.Administration;
using ERFWeb.Domain.Authentication;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class LoginLockoutRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly LoginLockoutPolicy Policy = LoginLockoutPolicy.Default;

    [Fact]
    public void FifthFailureInARow_LocksForFifteenMinutes()
    {
        int count = 0;
        DateTimeOffset? last = null;
        for (var attempt = 1; attempt < Policy.MaxFailedAttempts; attempt++)
        {
            var (next, lockUntil) = LoginLockoutRules.AfterFailure(count, last, Now.AddSeconds(attempt), Policy);
            Assert.Null(lockUntil);
            Assert.Equal(attempt, next);
            (count, last) = (next, Now.AddSeconds(attempt));
        }

        var (afterLock, lockedUntil) = LoginLockoutRules.AfterFailure(count, last, Now.AddSeconds(5), Policy);
        Assert.Equal(0, afterLock);
        Assert.Equal(Now.AddSeconds(5) + Policy.LockoutDuration, lockedUntil);
    }

    [Fact]
    public void FailuresOlderThanTheWindow_NoLongerCount()
    {
        var (count, lockUntil) = LoginLockoutRules.AfterFailure(4, Now, Now + Policy.LockoutDuration, Policy);

        Assert.Equal(1, count);
        Assert.Null(lockUntil);
    }

    [Fact]
    public void CustomPolicy_LocksOnItsOwnCountForItsOwnTime()
    {
        var policy = new LoginLockoutPolicy(3, 5);

        var (afterTwo, notYet) = LoginLockoutRules.AfterFailure(1, Now, Now.AddSeconds(1), policy);
        var (afterThree, lockedUntil) = LoginLockoutRules.AfterFailure(afterTwo, Now.AddSeconds(1), Now.AddSeconds(2), policy);

        Assert.Equal(2, afterTwo);
        Assert.Null(notYet);
        Assert.Equal(0, afterThree);
        Assert.Equal(Now.AddSeconds(2).AddMinutes(5), lockedUntil);
    }

    [Theory]
    [InlineData(5, 15, 0)]
    [InlineData(3, 1, 0)]
    [InlineData(20, 1440, 0)]
    [InlineData(2, 15, 1)]
    [InlineData(21, 15, 1)]
    [InlineData(5, 0, 1)]
    [InlineData(0, 1441, 2)]
    public void Validate_KeepsAttemptsAndMinutesInRange(int attempts, int minutes, int errorCount) =>
        Assert.Equal(errorCount, LoginLockoutRules.Validate(attempts, minutes).Count);

    [Fact]
    public void Parse_FallsBackToTheDefaultForMissingOrBadValues()
    {
        Assert.Equal(LoginLockoutPolicy.Default, LoginLockoutRules.Parse(null, null));
        Assert.Equal(new LoginLockoutPolicy(5, 30), LoginLockoutRules.Parse(1, 30));
        Assert.Equal(new LoginLockoutPolicy(8, 15), LoginLockoutRules.Parse(8, 99999));
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
