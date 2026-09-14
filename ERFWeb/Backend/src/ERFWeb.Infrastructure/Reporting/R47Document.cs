using System.Globalization;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Expenses;
using ERFWeb.Domain.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// The R47 "EXPENSE REPORT" controlled form as a QuestPDF document. One <c>Page</c> is
/// emitted per pre-paginated <see cref="R47Page"/>, each carrying the full header, grid,
/// per-page totals, cash-advance reconciliation, promissory footer, and signatory blocks.
/// Geometry is calibrated to the legacy printout (US Letter landscape, 792×612 pt).
/// </summary>
internal sealed class R47Document(
    ExpenseReportDocumentDto dto,
    IReadOnlyList<R47Page> pages,
    DateTimeOffset printedAt,
    R47ReportOptions opt,
    byte[] logo) : IDocument
{
    private static readonly Color Red = Color.FromHex("#C00000");
    private static readonly Color Blue = Color.FromHex("#1F1FBF");
    private const float Border = 0.6f;
    private const float RowHeight = 13f;

    // Grid column widths (pt); sum = 740 = page width − margins.
    private static readonly float[] GridColumns = [42, 146, 52, 50, 52, 52, 50, 52, 65, 52, 15, 60, 52];

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{opt.Title} — {dto.EmployeeFullName}",
        Author = opt.CompanyShortName,
        CreationDate = printedAt,
    };

    public void Compose(IDocumentContainer container)
    {
        foreach (var page in pages)
        {
            container.Page(p => ComposePage(p, page));
        }
    }

    private void ComposePage(PageDescriptor page, R47Page data)
    {
        page.Size(PageSizes.Letter.Landscape());
        page.MarginLeft(26);
        page.MarginRight(26);
        page.MarginTop(17);
        page.MarginBottom(20);
        page.DefaultTextStyle(s => s.FontFamily(opt.BodyFont).FontSize(8));

        page.Content().Column(col =>
        {
            col.Item().Element(c => ComposeHeader(c, data.PageNumber));
            col.Item().PaddingTop(3).Element(ComposePrintedDate);
            col.Item().Element(ComposeEmployeeBlock);
            col.Item().Element(c => ComposeGrid(c, data));
            col.Item().Element(c => ComposeRevolvingFundRow(c, data));
            col.Item().PaddingTop(7).Element(c => ComposeCashAdvanceBlock(c, data));
            col.Item().PaddingTop(6).Element(ComposeFooter);
            col.Item().PaddingTop(4).PaddingLeft(2).Text(text =>
            {
                text.Span("ER Reference Number:   ").FontSize(7.5f);
                text.Span(dto.ReferenceNo ?? string.Empty).FontSize(7.5f).FontColor(Red);
            });
        });
    }

    // ---- Header: logo | company + title | Document Code / Effectivity | Page / Revision ----

    private void ComposeHeader(IContainer c, int pageNumber)
    {
        c.Border(Border).Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(105);
                cols.RelativeColumn();
                cols.ConstantColumn(112);
                cols.ConstantColumn(112);
            });

            // Reference form: the logo sits in the same box as the company name (no divider).
            t.Cell().RowSpan(2).Padding(6).AlignCenter().AlignMiddle()
                .Height(72).Image(logo).FitArea();

            t.Cell().BorderBottom(Border).BorderRight(Border).AlignCenter().AlignMiddle().PaddingVertical(4)
                .Text(opt.CompanyName).FontFamily(opt.HeaderFont).FontSize(13).Bold();

            InfoCell(t, "Document Code:", opt.DocumentCode, borderRight: true);
            InfoCell(t, "Page No.", pageNumber.ToString(CultureInfo.InvariantCulture), borderRight: false);

            t.Cell().BorderRight(Border).AlignCenter().AlignMiddle().PaddingVertical(6)
                .Text(opt.Title).FontFamily(opt.HeaderFont).FontSize(17).Bold();

            InfoCell(t, "Effectivity Date", opt.EffectivityDate, borderRight: true, bottomBorder: false);
            InfoCell(t, "Revision No.", opt.RevisionNo, borderRight: false, bottomBorder: false);
        });
    }

    private void InfoCell(TableDescriptor t, string label, string value, bool borderRight, bool bottomBorder = true)
    {
        // Borders go on the cell BEFORE padding: a border applied after Padding() is drawn on the
        // inset inner box, so the lines stop short of the cell edges and never meet.
        IContainer cell = t.Cell();
        if (borderRight) cell = cell.BorderRight(Border);
        if (bottomBorder) cell = cell.BorderBottom(Border);

        cell.Padding(4).Column(col =>
        {
            col.Item().Text(label).FontFamily(opt.HeaderFont).FontSize(8.5f).Bold();
            col.Item().PaddingTop(5).AlignCenter().Text(value).FontFamily(opt.HeaderFont).FontSize(13).Bold();
        });
    }

    private void ComposePrintedDate(IContainer c)
    {
        var local = printedAt.ToOffset(TimeSpan.FromHours(opt.UtcOffsetHours));
        var stamp = string.Create(CultureInfo.InvariantCulture,
            $"{local:MM/dd/yyyy}   {local:h:mm:ss}{local.ToString("tt", CultureInfo.InvariantCulture).ToLowerInvariant()}");

        c.AlignRight().Width(183).Border(Border).Row(r =>
        {
            r.ConstantItem(55).BorderRight(Border).PaddingLeft(4).PaddingVertical(1).Text("DATE").Bold().FontSize(7.5f);
            r.RelativeItem().PaddingLeft(4).PaddingVertical(1).Text(stamp).FontSize(7.5f);
        });
    }

    private void ComposeEmployeeBlock(IContainer c)
    {
        c.Border(Border).Row(row =>
        {
            row.RelativeItem().BorderRight(Border).Column(col =>
            {
                col.Item().BorderBottom(Border).Row(r =>
                {
                    r.ConstantItem(105).PaddingLeft(4).PaddingVertical(1.5f).Text("EMPLOYEE:").Bold();
                    r.RelativeItem().PaddingVertical(1.5f).Text(dto.EmployeeFullName);
                });
                col.Item().Row(r =>
                {
                    r.ConstantItem(105).PaddingLeft(4).PaddingVertical(1.5f).Text("PURPOSE OF EXPENSE:").Bold();
                    r.RelativeItem().PaddingVertical(1.5f).Text(dto.Purpose);
                });
            });

            // Blank box under the DATE stamp, as printed by the legacy report.
            row.ConstantItem(183);
        });
    }

    // ---- Grid ----

    private void ComposeGrid(IContainer c, R47Page data)
    {
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                foreach (var w in GridColumns) cols.ConstantColumn(w);
            });

            // Header row 1
            HeaderCell(t, "DATE", Red, rowSpan: 2);
            HeaderCell(t, "PARTICULARS", Red, rowSpan: 2);
            HeaderCell(t, "Invoice/OR\nNumber", Red, rowSpan: 2);
            HeaderCell(t, "LOCAL TRAVEL", Red, colSpan: 6, bold: false);
            HeaderCell(t, "Representation", Red, rowSpan: 2, size: 6.5f);
            HeaderCell(t, "Oth", Red, rowSpan: 2, size: 6.5f);
            HeaderCell(t, "VAT", Red, rowSpan: 2);
            HeaderCell(t, "GRAND\nTOTAL", Red, rowSpan: 2);
            // Header row 2 (auto-placed into the free slots under LOCAL TRAVEL)
            foreach (var sub in new[] { "MEALS", "TRANSP.", "TOLL", "PARKING", "OTHERS", "TOTAL" })
            {
                HeaderCell(t, sub, Colors.Black, size: 6.5f);
            }

            // Printed rows are ruled and sized by their estimated line count (Crystal fills by height).
            foreach (var row in data.Rows)
            {
                var h = RowHeight * row.Lines;
                switch (row)
                {
                    case R47GroupHeaderRow g:
                        RuledCell(t, h).Text(g.ShowDate ? g.Date.ToString("dd-MMM-yy", CultureInfo.InvariantCulture) : string.Empty);
                        RuledCell(t, h).PaddingLeft(4).Text(g.Text).Bold();
                        for (var k = 2; k < GridColumns.Length; k++) RuledCell(t, h);
                        break;
                    case R47DetailRow d:
                        RuledCell(t, h);
                        RuledCell(t, h).PaddingLeft(6).Text(d.Particulars);
                        RuledCell(t, h).Text(d.InvoiceOrNo ?? string.Empty);
                        AmountCell(t, d.Buckets.Meals, h);
                        AmountCell(t, d.Buckets.Transportation, h);
                        AmountCell(t, d.Buckets.Toll, h);
                        AmountCell(t, d.Buckets.Parking, h);
                        AmountCell(t, d.Buckets.Others, h);
                        AmountCell(t, d.Buckets.LocalTotal, h);
                        AmountCell(t, d.Buckets.Representation, h);
                        AmountCell(t, d.Buckets.Oth, h);
                        AmountCell(t, d.Buckets.Vat, h);
                        AmountCell(t, d.Buckets.GrandTotal, h);
                        break;
                }
            }

            // The rest of the detail area stays blank (column rules only) so the totals row is anchored.
            var spare = Math.Max(0, opt.RowsPerPage - data.UsedLines);
            if (spare > 0)
            {
                for (var k = 0; k < GridColumns.Length; k++)
                {
                    t.Cell().BorderVertical(Border).MinHeight(RowHeight * spare);
                }
            }

            // Page totals row
            var tot = data.PageTotals;
            RuledCell(t, RowHeight);
            RuledCell(t, RowHeight);
            RuledCell(t, RowHeight);
            TotalCell(t, tot.Meals, dashZero: true);
            TotalCell(t, tot.Transportation, dashZero: true);
            TotalCell(t, tot.Toll, dashZero: true);
            TotalCell(t, tot.Parking, dashZero: true);
            TotalCell(t, tot.Others, dashZero: true);
            TotalCell(t, tot.LocalTotal, dashZero: true);
            TotalCell(t, tot.Representation, dashZero: false);
            TotalCell(t, tot.Oth, dashZero: false);
            TotalCell(t, tot.Vat, dashZero: false);
            TotalCell(t, tot.GrandTotal, dashZero: true);
        });
    }

    private static void HeaderCell(TableDescriptor t, string text, Color color, uint rowSpan = 1, uint colSpan = 1, bool bold = true, float size = 7f)
    {
        var cell = t.Cell();
        if (rowSpan > 1) cell = cell.RowSpan(rowSpan);
        if (colSpan > 1) cell = cell.ColumnSpan(colSpan);

        var span = cell.Border(Border).Padding(1.5f).AlignCenter().AlignMiddle().Text(text).FontSize(size).FontColor(color);
        if (bold) span.Bold();
    }

    /// <summary>A fully ruled grid cell of a fixed height (one or more text lines).</summary>
    private static IContainer RuledCell(TableDescriptor t, float height) =>
        t.Cell().Border(Border).MinHeight(height).PaddingHorizontal(2).PaddingVertical(1).AlignMiddle();

    private static void AmountCell(TableDescriptor t, decimal value, float height) =>
        RuledCell(t, height).AlignRight().Text(value == 0 ? string.Empty : Money(value));

    private static void TotalCell(TableDescriptor t, decimal value, bool dashZero) =>
        RuledCell(t, RowHeight).AlignRight().Text(value == 0 ? (dashZero ? "-" : string.Empty) : Money(value)).Bold();

    // ---- Revolving fund + GRAND TOTAL row (same column grid for alignment) ----

    private void ComposeRevolvingFundRow(IContainer c, R47Page data)
    {
        c.PaddingTop(3).Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                foreach (var w in GridColumns) cols.ConstantColumn(w);
            });

            t.Cell().ColumnSpan(3).AlignMiddle().PaddingLeft(4)
                .Text("IF WITH REVOLVING FUND. PLEASE INDICATE THE AMOUNT HERE").FontSize(6.5f).FontColor(Blue);
            t.Cell().Border(Border).MinHeight(RowHeight).PaddingHorizontal(2).AlignRight().AlignMiddle()
                .Text(dto.RevolvingFundAmount == 0 ? "0" : Money(dto.RevolvingFundAmount));
            t.Cell().ColumnSpan(5);
            t.Cell().ColumnSpan(3).Border(Border).MinHeight(RowHeight).PaddingLeft(4).AlignMiddle()
                .Text("GRAND TOTAL").Bold();
            t.Cell().Border(Border).MinHeight(RowHeight).PaddingHorizontal(2).AlignRight().AlignMiddle()
                .Text(Money(data.PageTotals.GrandTotal, dashZero: true)).Bold();
        });
    }

    // ---- Cash advance received (left) + reconciliation (right) ----

    private void ComposeCashAdvanceBlock(IContainer c, R47Page data)
    {
        var rec = data.Reconciliation;
        // At least one (blank) row so the block keeps its printed shape.
        var rows = dto.CashAdvances.Count > 0 ? dto.CashAdvances : [new R47CashAdvanceRow(null, null, null, 0m)];

        c.Row(row =>
        {
            row.ConstantItem(375).Border(Border).Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(60);
                    cols.RelativeColumn();
                    cols.ConstantColumn(70);
                    cols.ConstantColumn(80);
                });

                t.Cell().ColumnSpan(4).BorderBottom(Border).AlignCenter().PaddingVertical(1.5f)
                    .Text("CASH ADVANCE RECEIVED").Bold().FontColor(Red);

                foreach (var h in new[] { "DATE", "REFERENCE DOCUMENT", "REF. NO.", "Amount" })
                {
                    t.Cell().BorderBottom(Border).BorderRight(Border).AlignCenter().PaddingVertical(1).Text(h);
                }

                foreach (var ca in rows)
                {
                    t.Cell().BorderRight(Border).PaddingVertical(1).AlignCenter()
                        .Text(ca.AdvanceDate?.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture) ?? string.Empty);
                    t.Cell().BorderRight(Border).PaddingVertical(1).PaddingLeft(3).Text(ca.ReferenceDocument ?? string.Empty);
                    t.Cell().BorderRight(Border).PaddingVertical(1).PaddingLeft(3).Text(ca.ReferenceNo ?? string.Empty);
                    t.Cell().BorderRight(Border).PaddingVertical(1).PaddingRight(3).AlignRight().Text(ca.Amount.ToString("#,##0.00", CultureInfo.InvariantCulture));
                }

                t.Cell().BorderTop(Border);
                t.Cell().ColumnSpan(2).BorderTop(Border).PaddingLeft(20).PaddingVertical(1.5f).Text("TOTAL").Bold();
                t.Cell().BorderTop(Border).PaddingVertical(1.5f).PaddingRight(3).AlignRight().Text(dto.CashAdvanceTotal.ToString("#,##0.00", CultureInfo.InvariantCulture)).Bold();
            });

            row.ConstantItem(6);

            row.RelativeItem().Column(col =>
            {
                ReconLine(col, "CASH ADVANCE, if any", Red, rec.CarriedBalance);
                ReconLine(col, "LESS EXPENSE (THIS EXPENSE REPORT)", Red, rec.LessExpense);
                col.Item().Row(r =>
                {
                    r.RelativeItem().PaddingVertical(2).Text(text =>
                    {
                        text.Span("BAL DUE TO:   ").Bold();
                        // The legacy report never stored this; it follows the sign of the remainder.
                        text.Span(BalanceDueRules.DueTo(rec.BalanceDue, opt.CompanyShortName)).Bold();
                        text.Span($"      (If  due to {opt.CompanyShortName}, Cashier to issue OR; indicate Ref. and OR date.)").FontSize(6.5f).FontColor(Red);
                    });
                    r.ConstantItem(52).Border(Border).PaddingHorizontal(2).AlignRight().AlignMiddle()
                        .Text(Money(rec.BalanceDue, dashZero: true)).Bold();
                });
            });
        });
    }

    private static void ReconLine(ColumnDescriptor col, string label, Color color, decimal value)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem().PaddingVertical(2).Text(label).Bold().FontColor(color);
            r.ConstantItem(52).Border(Border).PaddingHorizontal(2).AlignRight().AlignMiddle()
                .Text(Money(value, dashZero: true)).Bold();
        });
    }

    // ---- Footer: promissory + employee signature | signatories ----

    private void ComposeFooter(IContainer c)
    {
        var sig = dto.Signatories;

        c.Border(Border).Row(row =>
        {
            row.ConstantItem(300).BorderRight(Border).Padding(6).Column(col =>
            {
                col.Item().Text(
                        "I PROMISE TO RETURN ANY AMOUNT DUE TO THE COMPANY,\n" +
                        "TOGETHER WITH THIS EXPENSE REPORT LIQUIDATING ANY CASH\n" +
                        "ADVANCES RECEIVED. THIS ALSO AUTHORIZES THE COMPANY\n" +
                        "TO DEDUCT FROM MY SALARY, ANY UNLIQUIDATED PORTION\n" +
                        "PURSUANT TO THE POLICY ON CASH ADVANCES.")
                    .FontSize(8.5f).FontColor(Blue).LineHeight(1.25f);

                col.Item().PaddingTop(2).AlignCenter().Width(120).Height(22).Element(e =>
                {
                    if (dto.EmployeeSignature is { Length: > 0 } bytes)
                    {
                        e.Image(bytes).FitArea();
                    }
                });
                col.Item().AlignCenter().Width(120).LineHorizontal(0.8f);
                col.Item().AlignCenter().Text("EMPLOYEE'S SIGNATURE").Bold().FontSize(7.5f);
            });

            row.RelativeItem().Column(col =>
            {
                col.Item().PaddingLeft(20).PaddingTop(3).Text("(Signature Over Printed Name)").FontSize(7).FontColor(Red);
                SignatoryLine(col, "ENDORSED BY :", sig.EndorsedBy, topBorder: false);
                SignatoryLine(col, "REVIEWED BY :", sig.ReviewedBy, topBorder: true);
                SignatoryLine(col, "APPROVED BY :", sig.ApprovedBy, topBorder: true, tall: true);
            });
        });
    }

    private static void SignatoryLine(ColumnDescriptor col, string label, string? initials, bool topBorder, bool tall = false)
    {
        var item = col.Item();
        if (topBorder) item = item.BorderTop(Border);

        item.MinHeight(tall ? 28 : 18).PaddingLeft(20).AlignMiddle().Row(r =>
        {
            r.ConstantItem(70).Text(label).Bold().FontSize(7.5f);
            r.RelativeItem().Text(initials ?? string.Empty);
        });
    }

    // ---- Formatting ----

    /// <summary>"#,##0.00" with negatives in parentheses; zero → "-" when dashZero, else "0.00".</summary>
    private static string Money(decimal value, bool dashZero = false)
    {
        if (value == 0)
        {
            return dashZero ? "-" : "0.00";
        }

        var text = Math.Abs(value).ToString("#,##0.00", CultureInfo.InvariantCulture);
        return value < 0 ? $"({text})" : text;
    }
}
