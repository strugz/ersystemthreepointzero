using System.Text;
using System.Text.RegularExpressions;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Reporting;
using Microsoft.Extensions.Options;
using Xunit;

namespace ERFWeb.Tests.Reporting;

public class R47PdfRendererTests
{
    /// <summary>Page-1 data from the reference printout (MUMER16AUG2026): 3 dates, 11 lines, totals 1,601.00.</summary>
    public static ExpenseReportDocumentDto SampleDocument()
    {
        var sort = 0;
        R47Line L(int day, string loc, string particulars, string? inv, ExpenseCategory cat, decimal amt) =>
            new(new DateOnly(2026, 8, day), particulars, inv, ExpenseType.Local, cat, amt, 0m, amt, loc, null, null, null, null, ++sort);

        var lines = new List<R47Line>
        {
            L(17, "MDMPI", "SUCAT - NICHOLS (TOLL)", "2667789054", ExpenseCategory.Toll, 84m),
            L(17, "MDMPI", "NICHOLS - SUCAT (TOLL)", "2668067280", ExpenseCategory.Toll, 84m),
            L(17, "MDMPI", "MEAL", null, ExpenseCategory.Meals, 250m),
            L(18, "MDMPI", "SUCAT - NICHOLS (TOLL)", "2668374400", ExpenseCategory.Toll, 84m),
            L(18, "MDMPI", "NICHOLS - SUCAT (TOLL)", "2668759469", ExpenseCategory.Toll, 84m),
            L(18, "MDMPI", "MEAL", null, ExpenseCategory.Meals, 250m),
            L(19, "CALMC", "SUCAT - CALAMBA (TOLL)", "2668960663", ExpenseCategory.Toll, 171m),
            L(19, "CALMC", "CALAMBA - NICHOLS (TOLL)", "2669103839", ExpenseCategory.Toll, 255m),
            L(19, "CALMC", "PARKING", null, ExpenseCategory.Parking, 40m),
            L(19, "CALMC", "NICHOLS - BICUTAN (TOLL)", "2669393763", ExpenseCategory.Toll, 49m),
            L(19, "CALMC", "MEAL", null, ExpenseCategory.Meals, 250m),
        };

        return new ExpenseReportDocumentDto(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            "ERF-2026-000142",
            "Marjon U. Macaraig",
            "reimbursement 08/16/2026 - 08/31/2026",
            RevolvingFundAmount: 0m,
            CashAdvances: [],
            new R47Signatories("RBG", null, "RBO"),
            EmployeeSignature: null,
            lines);
    }

    private static R47PdfRenderer Renderer(R47PdfEngine engine) => new(Options.Create(new R47ReportOptions { Engine = engine }));

    /// <summary>The QuestPDF samples keep their original names; the PDFsharp ones get a suffix for side-by-side comparison.</summary>
    private static string SampleName(string name, R47PdfEngine engine) =>
        engine == R47PdfEngine.QuestPdf ? $"{name}.pdf" : $"{name}-pdfsharp.pdf";

    [Theory]
    [InlineData(R47PdfEngine.QuestPdf)]
    [InlineData(R47PdfEngine.PdfSharp)]
    public void Render_ProducesSinglePagePdf_ForSamplePage1Data(R47PdfEngine engine)
    {
        var pdf = Renderer(engine).Render(SampleDocument(), new DateTimeOffset(2026, 9, 4, 5, 45, 12, TimeSpan.Zero));

        Assert.True(pdf.Length > 2_000, "PDF unexpectedly small");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(1, CountPages(pdf));

        // Written for the visual-diff check against the reference printout.
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, SampleName("R47-sample-page1", engine)), pdf);
    }

    [Theory]
    [InlineData(R47PdfEngine.QuestPdf)]
    [InlineData(R47PdfEngine.PdfSharp)]
    public void Render_PaginatesByDetailHeight(R47PdfEngine engine)
    {
        var renderer = new R47PdfRenderer(Options.Create(new R47ReportOptions { DetailHeight = 5 * R47Layout.SingleRowHeight, Engine = engine }));
        var pdf = renderer.Render(SampleDocument(), DateTimeOffset.UtcNow);

        // 14 single-line rows at 5 rows/page with Crystal's rules (no orphaned header, header repeated
        // when a group continues): [h,d,d,d] [h,d,d,d] [h,d,d,d,d] [h(repeat),d] → 4 pages.
        Assert.Equal(4, CountPages(pdf));
    }

    [Theory]
    [InlineData(R47PdfEngine.QuestPdf)]
    [InlineData(R47PdfEngine.PdfSharp)]
    public void Render_FullPagesOfWrappedRows_NeverOverflowAPage(R47PdfEngine engine)
    {
        // Long group headers and particulars wrap to 2–3 lines and invoice numbers break mid-token.
        // If the rendered rows were taller than R47Layout estimates, QuestPDF would spill a page.
        var sort = 0;
        var lines = Enumerable.Range(1, 60).Select(i => new R47Line(
            new DateOnly(2026, 10, 1 + i % 28),
            i % 3 == 0 ? "SUCAT - NICHOLS (TOLL) - MEDICARD INSTRUMENTS INVENTORY - SLMCGC - AU480 - 2022054754 WORK WITH JSO" : "Toll",
            i % 4 == 0 ? "SI-0001234567-89" : "2667789054",
            ExpenseType.Local, ExpenseCategory.Toll, 100m, 0m, 100m,
            i % 2 == 0 ? "Alabang Medical Clinic - Almanza - DXH 520 - (SN: BF020105) - BF020105" : "MDMPI",
            null, null, null, null, ++sort)).ToList();
        var doc = SampleDocument() with { Lines = lines };

        var expected = R47Layout.Paginate(R47Layout.BuildRows(lines), 0m).Count;
        var pdf = Renderer(engine).Render(doc, DateTimeOffset.UtcNow);

        Assert.True(expected > 1);
        Assert.Equal(expected, CountPages(pdf));
    }

    [Theory]
    [InlineData(R47PdfEngine.QuestPdf)]
    [InlineData(R47PdfEngine.PdfSharp)]
    public void Render_EmptyReport_StillProducesOnePage(R47PdfEngine engine)
    {
        var doc = SampleDocument() with { Lines = [] };
        var pdf = Renderer(engine).Render(doc, DateTimeOffset.UtcNow);
        Assert.Equal(1, CountPages(pdf));
    }

    [Theory]
    [InlineData(R47PdfEngine.QuestPdf)]
    [InlineData(R47PdfEngine.PdfSharp)]
    public void Render_Signatures_StaysOnOnePage(R47PdfEngine engine)
    {
        var signature = SignaturePng();
        var doc = SampleDocument() with
        {
            Signatories = new R47Signatories("Test Manager One", null, "Test Manager Two", signature, signature),
            EmployeeSignature = signature,
        };

        var pdf = Renderer(engine).Render(doc, DateTimeOffset.UtcNow);

        Assert.Equal(1, CountPages(pdf));
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, SampleName("R47-sample-signed", engine)), pdf);
    }

    [Theory]
    [InlineData(R47PdfEngine.QuestPdf)]
    [InlineData(R47PdfEngine.PdfSharp)]
    public void Render_ESignatures_WithWatermarkAndStamps_StaysOnOnePage(R47PdfEngine engine)
    {
        var signature = SignaturePng();
        var doc = SampleDocument() with
        {
            Signatories = new R47Signatories(
                "Test Manager One", null, "Test Manager Two", signature, signature,
                EndorsedAt: new DateTimeOffset(2026, 10, 28, 1, 30, 0, TimeSpan.Zero),
                ApprovedAt: new DateTimeOffset(2026, 10, 29, 6, 5, 0, TimeSpan.Zero)),
            EmployeeSignature = signature,
            FiledAt = new DateTimeOffset(2026, 10, 27, 9, 0, 0, TimeSpan.Zero),
        };

        var pdf = Renderer(engine).Render(doc, DateTimeOffset.UtcNow);

        Assert.Equal(1, CountPages(pdf));
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, SampleName("R47-sample-esignature", engine)), pdf);
    }

    [Theory]
    [InlineData(R47PdfEngine.QuestPdf)]
    [InlineData(R47PdfEngine.PdfSharp)]
    public void Render_StatusOnly_PrintsStampsWithoutImages_StaysOnOnePage(R47PdfEngine engine)
    {
        // Status only: the loader passes no images, so every signed place prints its electronic status.
        var doc = SampleDocument() with
        {
            Signatories = new R47Signatories(
                "Test Manager One", null, "Test Manager Two",
                EndorsedAt: new DateTimeOffset(2026, 10, 28, 1, 30, 0, TimeSpan.Zero),
                ApprovedAt: new DateTimeOffset(2026, 10, 29, 6, 5, 0, TimeSpan.Zero)),
            FiledAt = new DateTimeOffset(2026, 10, 27, 9, 0, 0, TimeSpan.Zero),
        };

        var pdf = Renderer(engine).Render(doc, DateTimeOffset.UtcNow);

        Assert.Equal(1, CountPages(pdf));
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, SampleName("R47-sample-status-only", engine)), pdf);
    }

    [Theory]
    [InlineData(R47PdfEngine.QuestPdf)]
    [InlineData(R47PdfEngine.PdfSharp)]
    public void Render_RulesPageFormat_ChoosesTheEngine(R47PdfEngine saved)
    {
        // The server says the other engine; the format saved on the Rules page wins.
        var server = saved == R47PdfEngine.QuestPdf ? R47PdfEngine.PdfSharp : R47PdfEngine.QuestPdf;
        var format = new R47ReportOptions().DefaultFormat() with { Engine = saved, DocumentCode = "R48" };
        var doc = SampleDocument() with { Format = format };

        var pdf = Renderer(server).Render(doc, DateTimeOffset.UtcNow);

        // PDFsharp names itself in plain text in the PDF; QuestPDF does not use that name.
        var madeByPdfSharp = Encoding.Latin1.GetString(pdf).Contains("PDFsharp", StringComparison.Ordinal);
        Assert.Equal(saved == R47PdfEngine.PdfSharp, madeByPdfSharp);
        Assert.Equal(1, CountPages(pdf));
    }

    [Fact]
    public void WithFormat_ReplacesHeaderDetails_KeepsServerFontsAndSizes()
    {
        var server = new R47ReportOptions { GridFont = "Arial", DetailHeight = 300, UtcOffsetHours = 7 };
        var format = server.DefaultFormat() with { Engine = R47PdfEngine.PdfSharp, CompanyName = "New Co.", RevisionNo = "3" };

        var options = server.WithFormat(format);

        Assert.Equal((R47PdfEngine.PdfSharp, "New Co.", "3"), (options.Engine, options.CompanyName, options.RevisionNo));
        Assert.Equal(("Arial", 300f, 7), (options.GridFont, options.DetailHeight, options.UtcOffsetHours));
        Assert.Equal(R47PdfEngine.QuestPdf, server.Engine);
    }

    /// <summary>A 160x40 RGBA PNG with a wavy blue stroke, standing in for an uploaded signature.</summary>
    private static byte[] SignaturePng()
    {
        const int w = 160, h = 40;
        var raw = new byte[h * (1 + w * 4)];
        for (var x = 4; x < w - 4; x++)
        {
            var y = (int)(h / 2 + Math.Sin(x / 9.0) * 12);
            for (var dy = -1; dy <= 1; dy++)
            {
                var i = (y + dy) * (1 + w * 4) + 1 + x * 4;
                raw[i + 2] = 160;
                raw[i + 3] = 255;
            }
        }

        using var idat = new MemoryStream();
        using (var z = new System.IO.Compression.ZLibStream(idat, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(raw);
        }

        using var png = new MemoryStream();
        png.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        WriteChunk(png, "IHDR", [0, 0, 0, w, 0, 0, 0, h, 8, 6, 0, 0, 0]);
        WriteChunk(png, "IDAT", idat.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream s, string type, byte[] data)
    {
        var typed = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        s.Write(BigEndian((uint)data.Length));
        s.Write(typed);
        s.Write(BigEndian(Crc32(typed)));
    }

    private static byte[] BigEndian(uint v) => [(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v];

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }

    private static int CountPages(byte[] pdf) =>
        Regex.Matches(Encoding.Latin1.GetString(pdf), @"/Type\s*/Page[^s]").Count;
}
