using ERFWeb.Infrastructure.Security;
using Xunit;

namespace ERFWeb.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify_RoundTrips()
    {
        var hash = Pbkdf2PasswordHasher.Hash("s3cret!");
        Assert.True(Pbkdf2PasswordHasher.Verify(hash, "s3cret!"));
        Assert.False(Pbkdf2PasswordHasher.Verify(hash, "wrong"));
    }

    /// <summary>Vector produced by Python hashlib.pbkdf2_hmac — the same way Database/002_seed_admin.sql was generated.</summary>
    [Fact]
    public void Verify_AcceptsExternallyGeneratedSeedHash()
    {
        const string seedHash = "100000.AAECAwQFBgcICQoLDA0ODw==.kpK+gJzktI0tnEyjtQ5th5XnlO6DIt+v8u07qWECeH0=";
        Assert.True(Pbkdf2PasswordHasher.Verify(seedHash, "ChangeMe!123"));
        Assert.False(Pbkdf2PasswordHasher.Verify(seedHash, "ChangeMe!124"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not.a.valid.hash")]
    [InlineData("abc.def")]
    public void Verify_RejectsMalformedHashes(string? stored)
    {
        Assert.False(Pbkdf2PasswordHasher.Verify(stored, "anything"));
    }
}
