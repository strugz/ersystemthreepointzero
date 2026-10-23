using ERFWeb.Domain.Administration;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class EmailDomainRulesTests
{
    [Fact]
    public void Normalize_TrimsLowerCasesDropsTheAtSignAndDuplicatesKeepingOrder()
    {
        var (domains, error) = EmailDomainRules.Normalize([" @MarsmanDrysdale.com ", "mdmpi.com.ph", "marsmandrysdale.com", ""]);

        Assert.Null(error);
        Assert.Equal(["marsmandrysdale.com", "mdmpi.com.ph"], domains);
    }

    [Theory]
    [InlineData("marsmandrysdale")]
    [InlineData("bad domain.com")]
    [InlineData("-x.com")]
    [InlineData("x@y.com")]
    public void Normalize_RefusesAnInvalidDomain(string domain) =>
        Assert.NotNull(EmailDomainRules.Normalize(["marsmandrysdale.com", domain]).Error);

    [Fact]
    public void Normalize_NeedsAtLeastOneDomain() =>
        Assert.NotNull(EmailDomainRules.Normalize([" ", null]).Error);

    [Fact]
    public void Normalize_RefusesTooManyDomains() =>
        Assert.NotNull(EmailDomainRules.Normalize(Enumerable.Range(1, EmailDomainRules.MaxDomains + 1).Select(i => $"d{i}.com")).Error);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a domain")]
    public void Parse_FallsBackToTheDefault(string? stored) =>
        Assert.Equal([EmailDomainRules.DefaultDomain], EmailDomainRules.Parse(stored));

    [Fact]
    public void Parse_ReadsTheStoredListBack() =>
        Assert.Equal(["mdmpi.com.ph", "marsmandrysdale.com"], EmailDomainRules.Parse(EmailDomainRules.Format(["mdmpi.com.ph", "marsmandrysdale.com"])));
}
