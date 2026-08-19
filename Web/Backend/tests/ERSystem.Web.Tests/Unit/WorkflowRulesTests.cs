using ERSystem.Web.Application.Common;
using ERSystem.Web.Domain.Common;
using ERSystem.Web.Infrastructure.Security;
using ERSystem.Web.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace ERSystem.Web.Tests.Unit;

public sealed class WorkflowRulesTests
{
    [Fact]
    public void Approval_sequence_requires_every_previous_step()
    {
        Assert.True(ApprovalSequence.CanApprove(1, []));
        Assert.True(ApprovalSequence.CanApprove(3, [1, 2]));
        Assert.False(ApprovalSequence.CanApprove(3, [1]));
    }

    [Fact]
    public void Pagination_is_clamped_to_supported_bounds()
    {
        var request = new PagedRequest { Page = 0, PageSize = 500 };
        Assert.Equal(1, request.Page);
        Assert.Equal(100, request.PageSize);
    }

    [Fact]
    public void Legacy_cipher_matches_the_desktop_format()
    {
        var cipher = new LegacyPasswordCipher(Options.Create(new LegacyAuthenticationOptions { EncryptionKey = "test-key" }));
        Assert.Equal("56394F616748614E617A2B373257434178436F35545A4854423757634C384661", cipher.Encrypt("Password123"));
        Assert.Equal(
            "Password123",
            cipher.Decrypt("56394F616748614E617A2B373257434178436F35545A4854423757634C384661"));
    }

    [Fact]
    public void Legacy_cipher_round_trips_unicode_email_values()
    {
        var cipher = new LegacyPasswordCipher(
            Options.Create(new LegacyAuthenticationOptions { EncryptionKey = "test-key" }));
        const string value = "employee@example.test";

        Assert.Equal(value, cipher.Decrypt(cipher.Encrypt(value)));
    }

    [Fact]
    public void Row_version_round_trips_and_rejects_invalid_values()
    {
        var codec = new RowVersionCodec();
        var value = new byte[] { 1, 2, 3, 4 };
        var encoded = codec.Encode(value);
        Assert.True(codec.Matches(value, encoded));
        Assert.Throws<ValidationException>(() => codec.Decode("not-base64"));
    }
}
