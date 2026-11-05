using ERFWeb.Domain.Reporting;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class R47FormatRulesTests
{
    private static R47Format Valid() =>
        new(R47PdfEngine.QuestPdf, "Marsman Drysdale Medical Products, Inc.", "MDMPI", "EXPENSE REPORT", "R47", "May 15, 2023", "0");

    [Fact]
    public void Validate_TheFormAsPrintedToday_IsValid() => Assert.Empty(R47FormatRules.Validate(Valid()));

    [Fact]
    public void Validate_BlankAndTooLongValues_AreReportedPerField()
    {
        var format = Valid() with { DocumentCode = "  ", EffectivityDate = new string('x', R47FormatRules.EffectivityDateMaxLength + 1) };

        var errors = R47FormatRules.Validate(format);

        Assert.Equal(["r47Format.documentCode", "r47Format.effectivityDate"], errors.Keys.Order());
    }

    [Fact]
    public void Validate_UnknownEngine_IsRejected() =>
        Assert.Contains("r47Format.engine", R47FormatRules.Validate(Valid() with { Engine = (R47PdfEngine)7 }).Keys);

    [Theory]
    [InlineData("PdfSharp", R47PdfEngine.PdfSharp)]
    [InlineData("questpdf", R47PdfEngine.QuestPdf)]
    [InlineData(null, null)]
    [InlineData("1", null)]
    [InlineData("Word", null)]
    public void ParseEngine_OnlyKnownNames(string? value, R47PdfEngine? expected) =>
        Assert.Equal(expected, R47FormatRules.ParseEngine(value));

    [Fact]
    public void Normalize_TrimsEveryText()
    {
        var format = R47FormatRules.Normalize(Valid() with { DocumentCode = "  R48 ", RevisionNo = " 2 " });

        Assert.Equal("R48", format.DocumentCode);
        Assert.Equal("2", format.RevisionNo);
    }
}
