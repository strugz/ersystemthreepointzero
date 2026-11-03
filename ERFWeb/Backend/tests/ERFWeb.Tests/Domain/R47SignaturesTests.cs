using ERFWeb.Domain.Reporting;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class R47SignaturesTests
{
    // 06:05 UTC is 2:05pm in Manila (+8).
    private static readonly DateTimeOffset At = new(2026, 10, 29, 6, 5, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(R47SignatureMode.ESignature, true, true, true)]
    [InlineData(R47SignatureMode.ESignature, true, false, false)] // waiting for Finance
    [InlineData(R47SignatureMode.ESignature, false, true, false)]
    [InlineData(R47SignatureMode.StatusOnly, true, true, false)]
    public void PrintsImage_OnlyApprovedImagesInESignatureMode(R47SignatureMode mode, bool hasImage, bool approved, bool expected) =>
        Assert.Equal(expected, R47Signatures.PrintsImage(mode, hasImage, approved));

    [Fact]
    public void Stamps_PrintLocalDateAndTime()
    {
        Assert.Equal("Electronically approved 10/29/2026 2:05pm", R47Signatures.ApprovedStamp(At, 8));
        Assert.Equal("Electronically filed 10/29/2026 2:05pm", R47Signatures.FiledStamp(At, 8));
        Assert.Equal("Electronically signed in ERFWeb 10/29/2026 2:05pm · ERF-2026-000142", R47Signatures.SignedStamp(At, 8, "ERF-2026-000142"));
        Assert.Equal("Electronically signed in ERFWeb 10/29/2026 2:05pm", R47Signatures.SignedStamp(At, 8, null));
    }

    [Theory]
    [InlineData("StatusOnly", R47SignatureMode.StatusOnly)]
    [InlineData("esignature", R47SignatureMode.ESignature)]
    [InlineData(null, R47SignatureMode.ESignature)]
    [InlineData("7", R47SignatureMode.ESignature)]
    [InlineData("Sometimes", R47SignatureMode.ESignature)]
    public void ParseMode_UnknownKeepsImages(string? value, R47SignatureMode expected) =>
        Assert.Equal(expected, R47Signatures.ParseMode(value));
}
