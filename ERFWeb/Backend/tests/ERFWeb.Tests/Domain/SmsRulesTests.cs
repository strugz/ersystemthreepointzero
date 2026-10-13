using ERFWeb.Domain.Notifications;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class SmsRulesTests
{
    [Theory]
    [InlineData("09171234567", "09171234567")]
    [InlineData("+639181234567", "+639181234567")]
    [InlineData("639181234567", "+639181234567")]
    [InlineData(" 0917-123-4567 ", "09171234567")]
    [InlineData("(0917) 123 4567", "09171234567")]
    public void RecipientNumbers_NormalisesOneMobileNumber(string mobileNo, string expected) =>
        Assert.Equal([expected], SmsRules.RecipientNumbers(mobileNo));

    [Fact]
    public void RecipientNumbers_SplitsATextGroupAndDropsDuplicates() =>
        Assert.Equal(
            ["09171234567", "+639181234567"],
            SmsRules.RecipientNumbers("09171234567, +639181234567 / 0917 123 4567"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("(02) 8123 4567")]
    [InlineData("0917123456")]
    [InlineData("091712345678")]
    [InlineData("loc. 204")]
    public void RecipientNumbers_IsEmptyWithoutAMobileNumber(string? mobileNo) =>
        Assert.Empty(SmsRules.RecipientNumbers(mobileNo));

    [Fact]
    public void RecipientNumbers_SkipsLandlinesButKeepsMobiles() =>
        Assert.Equal(["09171234567"], SmsRules.RecipientNumbers("(02) 8123 4567; 09171234567"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void MessageError_RequiresText(string? message) =>
        Assert.NotNull(SmsRules.MessageError(message));

    [Fact]
    public void MessageError_EnforcesTheLengthLimit()
    {
        Assert.Null(SmsRules.MessageError(new string('a', SmsRules.MaxMessageLength)));
        Assert.Null(SmsRules.MessageError("  " + new string('a', SmsRules.MaxMessageLength) + "  "));
        Assert.NotNull(SmsRules.MessageError(new string('a', SmsRules.MaxMessageLength + 1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("3f2b8c1e-6a4d-4f0e-9b7a-2c1d5e6f7a8b")]
    [InlineData("followup:ERF-2026-000001.1")]
    public void IdempotencyKeyError_AcceptsAbsentOrSafeKeys(string? key) =>
        Assert.Null(SmsRules.IdempotencyKeyError(key));

    [Theory]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    [InlineData("")]
    public void IdempotencyKeyError_RejectsUnsafeKeys(string key) =>
        Assert.NotNull(SmsRules.IdempotencyKeyError(key));

    [Fact]
    public void IdempotencyKeyError_RejectsOverlongKeys() =>
        Assert.NotNull(SmsRules.IdempotencyKeyError(new string('k', SmsRules.MaxIdempotencyKeyLength + 1)));
}
