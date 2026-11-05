using System.Text.Json;
using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class R47DesignRulesTests
{
    private static readonly R47Design Standard = R47Design.Standard;

    [Fact]
    public void Validate_TheStandardForm_IsValidAndFitsThePage()
    {
        Assert.Empty(R47DesignRules.Validate(Standard));
        Assert.Equal(748f, Standard.ContentWidth(), precision: 2);
        Assert.Equal(Standard.ContentWidth(), Standard.GridColumns.Sum(), precision: 2);
        Assert.True(Standard.FormHeight() <= R47Design.PageHeight);
    }

    [Fact]
    public void Validate_GridColumnsThatDoNotFillTheWidth_AreRejected()
    {
        var columns = Standard.GridColumns.ToArray();
        columns[1] += 10;

        var errors = R47DesignRules.Validate(Standard with { GridColumns = columns });

        Assert.Equal(["r47Design.gridColumns"], errors.Keys);
    }

    [Fact]
    public void Validate_WiderMargins_NeedNarrowerColumns()
    {
        var columns = Standard.GridColumns.ToArray();
        columns[1] -= 20;

        Assert.Empty(R47DesignRules.Validate(Standard with { MarginLeft = 35, MarginRight = 29, GridColumns = columns }));
    }

    [Fact]
    public void Validate_SizesOutOfRange_AreReportedPerField()
    {
        var errors = R47DesignRules.Validate(Standard with { BodySize = 30, HeaderHeight = 10, LabelColor = "red" });

        Assert.Equal(["r47Design.bodySize", "r47Design.headerHeight", "r47Design.labelColor"], errors.Keys.Order());
    }

    [Fact]
    public void Validate_FormTallerThanThePage_IsRejected()
    {
        var errors = R47DesignRules.Validate(Standard with { DetailHeight = 300 });

        Assert.Equal(["r47Design.detailHeight"], errors.Keys);
    }

    [Fact]
    public void Validate_PromiseThatDoesNotFitTheFooter_IsRejected()
    {
        var errors = R47DesignRules.Validate(Standard with { PromiseText = Standard.PromiseText + "\nONE MORE LINE" });

        Assert.Equal(["r47Design.footerHeight"], errors.Keys);
        Assert.Empty(R47DesignRules.Validate(Standard with { PromiseText = Standard.PromiseText + "\nONE MORE LINE", FooterHeight = 100, DetailHeight = 245 }));
    }

    [Fact]
    public void PromiseAlign_IsLeftOnTheStandardForm_AndOnlyLeftOrCenter()
    {
        Assert.Equal(R47DesignRules.AlignLeft, Standard.PromiseAlign);
        Assert.Empty(R47DesignRules.Validate(Standard with { PromiseAlign = R47DesignRules.AlignCenter }));
        Assert.Equal(["r47Design.promiseAlign"], R47DesignRules.Validate(Standard with { PromiseAlign = "Right" }).Keys);
        Assert.Equal(R47DesignRules.AlignCenter, R47DesignRules.Normalize(Standard with { PromiseAlign = " center " }).PromiseAlign);
    }

    [Fact]
    public void Validate_NarrowTitle_IsRejected() =>
        Assert.Contains("r47Design.logoWidth", R47DesignRules.Validate(Standard with { LogoWidth = 250, DocumentCodeWidth = 200, PageNoWidth = 200 }).Keys);

    [Fact]
    public void Normalize_TrimsTextsAndPromiseLines()
    {
        var design = R47DesignRules.Normalize(Standard with { PromiseText = "  LINE ONE \r\n\r\n LINE TWO ", NoteColor = " #1f1fbf ", RevolvingFundText = " X " });

        Assert.Equal("LINE ONE\nLINE TWO", design.PromiseText);
        Assert.Equal("#1F1FBF", design.NoteColor);
        Assert.Equal("X", design.RevolvingFundText);
    }

    [Fact]
    public void GridMetrics_OfTheStandardForm_AreTheMeasuredConstants() =>
        Assert.Equal(R47GridMetrics.Standard, Standard.GridMetrics());

    [Fact]
    public void GridMetrics_FollowColumnWidthsAndTypeSize()
    {
        var columns = Standard.GridColumns.ToArray();
        columns[1] += 30;
        columns[3] -= 30;

        var wider = (Standard with { GridColumns = columns }).GridMetrics();
        var larger = (Standard with { GridTextSize = 7f }).GridMetrics();

        Assert.True(wider.ParticularsCapacity > R47TextMetrics.ParticularsCapacity);
        Assert.Equal(R47TextMetrics.InvoiceCapacity, wider.InvoiceCapacity);
        Assert.True(larger.ParticularsCapacity < R47TextMetrics.ParticularsCapacity);
        Assert.True(larger.LineHeight > R47Layout.WrappedLineHeight);
    }

    [Fact]
    public void BuildRows_WithAWiderParticularsColumn_WrapsLess()
    {
        const string text = "SUCAT - NICHOLS (TOLL) - MEDICARD INSTRUMENTS INVENTORY - SLMCGC - AU480 - 2022054754 WORK WITH JSO";
        var line = new R47Line(new DateOnly(2026, 10, 1), text, null, ExpenseType.Local, ExpenseCategory.Toll, 1m, 0m, 1m, "MDMPI", null, null, null, null, 1);
        var columns = Standard.GridColumns.ToArray();
        columns[1] += 60;
        columns[3] -= 30;
        columns[4] -= 30;

        var standardRow = R47Layout.BuildRows([line])[1];
        var widerRow = R47Layout.BuildRows([line], (Standard with { GridColumns = columns }).GridMetrics())[1];

        Assert.True(widerRow.Lines < standardRow.Lines);
        Assert.True(widerRow.Height < standardRow.Height);
    }

    [Fact]
    public void Json_MissingProperties_KeepTheStandardValues()
    {
        var design = JsonSerializer.Deserialize<R47Design>("""{"detailHeight":200,"labelColor":"#000000"}""", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.Equal(200f, design.DetailHeight);
        Assert.Equal("#000000", design.LabelColor);
        Assert.Equal(Standard.GridColumns, design.GridColumns);
        Assert.Equal(Standard.PromiseText, design.PromiseText);
    }
}
