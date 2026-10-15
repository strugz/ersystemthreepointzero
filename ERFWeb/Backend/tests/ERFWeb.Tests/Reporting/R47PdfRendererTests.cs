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

    private static R47PdfRenderer Renderer() => new(Options.Create(new R47ReportOptions()));

    [Fact]
    public void Render_ProducesSinglePagePdf_ForSamplePage1Data()
    {
        var pdf = Renderer().Render(SampleDocument(), new DateTimeOffset(2026, 9, 4, 5, 45, 12, TimeSpan.Zero));

        Assert.True(pdf.Length > 2_000, "PDF unexpectedly small");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(1, CountPages(pdf));

        // Written for the visual-diff check against the reference printout.
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "R47-sample-page1.pdf"), pdf);
    }

    [Fact]
    public void Render_PaginatesByRowsPerPage()
    {
        var renderer = new R47PdfRenderer(Options.Create(new R47ReportOptions { RowsPerPage = 5 }));
        var pdf = renderer.Render(SampleDocument(), DateTimeOffset.UtcNow);

        // 14 single-line rows at 5 lines/page with Crystal's rules (no orphaned header, header repeated
        // when a group continues): [h,d,d,d] [h,d,d,d] [h,d,d,d,d] [h(repeat),d] → 4 pages.
        Assert.Equal(4, CountPages(pdf));
    }

    [Fact]
    public void Render_EmptyReport_StillProducesOnePage()
    {
        var doc = SampleDocument() with { Lines = [] };
        var pdf = Renderer().Render(doc, DateTimeOffset.UtcNow);
        Assert.Equal(1, CountPages(pdf));
    }

    [Fact]
    public void Render_Signatures_StaysOnOnePage()
    {
        var signature = SignaturePng();
        var doc = SampleDocument() with
        {
            Signatories = new R47Signatories("Test Manager One", null, "Test Manager Two", signature, signature),
            EmployeeSignature = signature,
        };

        var pdf = Renderer().Render(doc, DateTimeOffset.UtcNow);

        Assert.Equal(1, CountPages(pdf));
        File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "R47-sample-signed.pdf"), pdf);
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
