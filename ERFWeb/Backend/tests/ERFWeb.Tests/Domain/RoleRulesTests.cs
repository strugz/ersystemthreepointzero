using ERFWeb.Domain;
using ERFWeb.Domain.Administration;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class RoleRulesTests
{
    [Theory]
    [InlineData(UserRole.SystemAdministrator, UserRole.SignatoryLevel1)]
    [InlineData(UserRole.SystemAdministrator, UserRole.SignatoryLevel2)]
    [InlineData(UserRole.SystemAdministrator, UserRole.Finance)]
    [InlineData(UserRole.SystemAdministrator, UserRole.ExpenseVerifier)]
    [InlineData(UserRole.SystemAdministrator, UserRole.BusinessOwner)]
    [InlineData(UserRole.Finance, UserRole.SystemAdministrator)]
    public void SystemAdministrator_cannot_hold_a_transacting_role(UserRole held, UserRole adding)
    {
        var violation = RoleRules.StackingViolation([held], adding);
        Assert.NotNull(violation);
        Assert.Contains("System Administrator", violation);
    }

    [Theory]
    [InlineData(UserRole.Finance, UserRole.SignatoryLevel1)]
    [InlineData(UserRole.Finance, UserRole.SignatoryLevel2)]
    [InlineData(UserRole.SignatoryLevel1, UserRole.Finance)]
    public void Finance_cannot_also_be_a_signatory(UserRole held, UserRole adding)
    {
        var violation = RoleRules.StackingViolation([held], adding);
        Assert.Equal("Finance cannot also be a Signatory.", violation);
    }

    [Theory]
    [InlineData(UserRole.Finance, UserRole.ExpenseVerifier)]
    [InlineData(UserRole.ExpenseVerifier, UserRole.Finance)]
    public void Finance_cannot_also_be_a_verifier(UserRole held, UserRole adding)
    {
        var violation = RoleRules.StackingViolation([held], adding);
        Assert.Equal("Finance cannot also be an Expense Verifier.", violation);
    }

    [Theory]
    [InlineData(UserRole.SignatoryLevel1, UserRole.SignatoryLevel2)]
    [InlineData(UserRole.SignatoryLevel2, UserRole.BusinessOwner)]
    [InlineData(UserRole.Finance, UserRole.BusinessOwner)]
    [InlineData(UserRole.ExpenseVerifier, UserRole.SignatoryLevel1)]
    [InlineData(UserRole.Employee, UserRole.SystemAdministrator)]
    public void Allowed_combinations_return_no_violation(UserRole held, UserRole adding) =>
        Assert.Null(RoleRules.StackingViolation([held], adding));

    [Fact]
    public void Employee_is_the_base_role_and_everything_else_is_privileged()
    {
        Assert.True(RoleRules.IsBaseRole(UserRole.Employee));
        Assert.False(RoleRules.IsPrivileged(UserRole.Employee));
        foreach (var role in Enum.GetValues<UserRole>().Where(r => r != UserRole.Employee))
        {
            Assert.True(RoleRules.IsPrivileged(role), role.ToString());
        }
    }

    [Fact]
    public void Self_change_is_detected_by_id() =>
        Assert.True(RoleRules.IsSelfChange(7, 7));

    [Fact]
    public void Business_owner_countersigns_without_bootstrap()
    {
        var decision = RoleRules.Countersign(countersignerUserId: 3, [UserRole.Employee, UserRole.BusinessOwner], granterUserId: 1, targetUserId: 2, businessOwnerExists: true);
        Assert.True(decision.IsAllowed);
        Assert.False(decision.IsBootstrap);
    }

    [Fact]
    public void Second_administrator_countersigns_as_bootstrap_only_while_no_owner_exists()
    {
        var bootstrap = RoleRules.Countersign(3, [UserRole.SystemAdministrator], granterUserId: 1, targetUserId: 2, businessOwnerExists: false);
        Assert.True(bootstrap.IsAllowed);
        Assert.True(bootstrap.IsBootstrap);

        var refused = RoleRules.Countersign(3, [UserRole.SystemAdministrator], granterUserId: 1, targetUserId: 2, businessOwnerExists: true);
        Assert.False(refused.IsAllowed);
        Assert.Contains("Business Owner", refused.Reason);
    }

    [Fact]
    public void Granter_and_target_can_never_countersign()
    {
        var granter = RoleRules.Countersign(1, [UserRole.BusinessOwner], granterUserId: 1, targetUserId: 2, businessOwnerExists: true);
        Assert.False(granter.IsAllowed);

        var target = RoleRules.Countersign(2, [UserRole.BusinessOwner], granterUserId: 1, targetUserId: 2, businessOwnerExists: true);
        Assert.False(target.IsAllowed);
    }

    [Fact]
    public void Plain_employee_cannot_countersign()
    {
        var decision = RoleRules.Countersign(3, [UserRole.Employee, UserRole.Finance], granterUserId: 1, targetUserId: 2, businessOwnerExists: false);
        Assert.False(decision.IsAllowed);
    }
}

public class AccountRulesTests
{
    private static readonly DateTimeOffset Registered = new(2026, 10, 13, 8, 0, 0, TimeSpan.FromHours(8));

    [Theory]
    [InlineData(3, 2, false)]
    [InlineData(3, 3, true)]
    [InlineData(3, 10, true)]
    [InlineData(0, 0, true)]
    public void Cooling_period_elapses_after_the_configured_days(int days, int daysLater, bool expected) =>
        Assert.Equal(expected, AccountRules.CoolingPeriodElapsed(Registered, Registered.AddDays(daysLater), days));

    [Fact]
    public void Negative_cooling_period_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AccountRules.CoolingPeriodElapsed(Registered, Registered, -1));

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("jay bryan", false)]
    [InlineData("jbryan", true)]
    public void Username_shape_is_validated(string username, bool valid) =>
        Assert.Equal(valid, AccountRules.UsernameError(username) is null);

    [Theory]
    [InlineData("short", false)]
    [InlineData("longenough1", true)]
    public void Password_length_is_validated(string password, bool valid) =>
        Assert.Equal(valid, AccountRules.PasswordError(password) is null);
}
