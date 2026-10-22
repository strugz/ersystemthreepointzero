using ERFWeb.Domain.Notifications;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class EmailRulesTests
{
    [Theory]
    [InlineData("jay@mdmpi.com.ph", "jay@mdmpi.com.ph")]
    [InlineData("  jay@mdmpi.com.ph ", "jay@mdmpi.com.ph")]
    public void RecipientAddress_AcceptsOnePlainMailbox(string email, string expected) =>
        Assert.Equal(expected, EmailRules.RecipientAddress(email));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-address")]
    [InlineData("Jay <jay@mdmpi.com.ph>")]
    [InlineData("a@mdmpi.com.ph, b@mdmpi.com.ph")]
    public void RecipientAddress_SkipsAnythingElse(string? email) =>
        Assert.Null(EmailRules.RecipientAddress(email));

    [Fact]
    public void DistinctAddresses_KeepsOrderAndDropsCaseInsensitiveDuplicates() =>
        Assert.Equal(
            ["jay@mdmpi.com.ph", "l1@mdmpi.com.ph"],
            EmailRules.DistinctAddresses(["jay@mdmpi.com.ph", null, "bad", "l1@mdmpi.com.ph", "JAY@mdmpi.com.ph"]));
}
