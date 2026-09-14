using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class BalanceDueRulesTests
{
    [Theory]
    [InlineData(-1601.00, "EMPLOYEE")]   // reimbursement: no advance, all due to the employee
    [InlineData(-250.50, "EMPLOYEE")]    // spent beyond the advance
    [InlineData(0, "EMPLOYEE")]          // fully liquidated keeps the legacy default label
    [InlineData(399.00, "MDMPI")]        // unspent advance is owed back to the company
    public void DueTo_FollowsSignOfBalance(decimal balance, string expected) =>
        Assert.Equal(expected, BalanceDueRules.DueTo(balance, "MDMPI"));

    [Fact]
    public void DueTo_UppercasesCompanyLabel() =>
        Assert.Equal("MDMPI", BalanceDueRules.DueTo(10m, " mdmpi "));

    [Fact]
    public void DueTo_RequiresCompanyLabel() =>
        Assert.Throws<ArgumentException>(() => BalanceDueRules.DueTo(10m, " "));

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsDueToCompany_OnlyForPositiveBalance(decimal balance, bool expected) =>
        Assert.Equal(expected, BalanceDueRules.IsDueToCompany(balance));
}
