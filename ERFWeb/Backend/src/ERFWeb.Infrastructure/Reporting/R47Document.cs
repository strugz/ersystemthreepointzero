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
/// Geometry and type sizes are measured from the legacy Crystal printout (MARER08OCT2026,
/// US Letter landscape, 792×612 pt).
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

    // Type sizes of the reference printout (pt).
    private const float BodySize = 7.1f;
    private const float GridTextSize = 5.8f;
    private const float AmountSize = 6.6f;
    private const float DateSize = 6.2f;
    private const float SmallSize = 6.2f;

    private const float TotalsRowHeight = 15.2f;
    private const float GrandTotalRowHeight = 13.5f;
    private const float CellPadding = 1.5f;

    // Grid column widths (pt); sum = 748 = page width − margins.
    private static readonly float[] GridColumns = [45, 148, 51, 50, 53, 53, 50, 60, 57.5f, 54, 36, 37, 53.5f];

    // PARTICULARS wraps inside a fixed text box, as Crystal does, so the rendered wrap matches
    // R47TextMetrics.ParticularsCapacity (20,500/1000 em × 5.8 pt ≈ 118.9 pt).
    private const float ParticularsTextWidth = 119f;
    private const float GroupHeaderIndent = 3f;
    private const float DetailIndent = 5f;

    // The DATE stamp and the blank box under it sit over the last four grid columns
    // (Representation, Other Exp., VAT, Grand Total) so their lines continue the grid's; the
    // DATE label spans Representation + Other Exp. so its divider meets the VAT column line.
    private static readonly float DateBoxWidth = GridColumns[^4..].Sum();
    private static readonly float DateLabelWidth = GridColumns[^4] + GridColumns[^3];

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
        page.MarginLeft(25);
        page.MarginRight(19);
        page.MarginTop(8);
        page.MarginBottom(12);
        page.DefaultTextStyle(s => s.FontFamily(opt.BodyFont).FontSize(BodySize));

        page.Content().Column(col =>
        {
            col.Item().Element(c => ComposeHeader(c, data.PageNumber));
            col.Item().Element(ComposePrintedDate);
            col.Item().Element(ComposeEmployeeBlock);
            col.Item().Element(c => ComposeGrid(c, data));
            col.Item().Element(c => ComposeRevolvingFundRow(c, data));
            col.Item().PaddingTop(1).Element(c => ComposeCashAdvanceBlock(c, data));
            col.Item().Element(ComposeFooter);
            col.Item().PaddingTop(3).Text(text =>
            {
                text.Span("ER Reference Number:   ").FontSize(SmallSize);
                text.Span(dto.ReferenceNo ?? string.Empty).FontSize(SmallSize).FontColor(Red);
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
                cols.ConstantColumn(118);
                cols.RelativeColumn();
                cols.ConstantColumn(104);
                cols.ConstantColumn(102);
            });

            // Reference form: the logo sits in the same box as the company name (no divider).
            t.Cell().RowSpan(2).Padding(6).AlignCenter().AlignMiddle()
                .Height(76).Image(logo).FitArea();

            t.Cell().BorderBottom(Border).BorderRight(Border).AlignCenter().AlignMiddle().PaddingVertical(4)
                .Text(opt.CompanyName).FontFamily(opt.HeaderFont).FontSize(14.8f).Bold();

            InfoCell(t, "Document Code:", opt.DocumentCode, borderRight: true);
            InfoCell(t, "Page No.", pageNumber.ToString(CultureInfo.InvariantCulture), borderRight: false);

            t.Cell().BorderRight(Border).AlignCenter().AlignMiddle().PaddingVertical(6)
                .Text(opt.Title).FontFamily(opt.HeaderFont).FontSize(19.6f).Bold();

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
            col.Item().Text(label).FontFamily(opt.HeaderFont).FontSize(8.2f).Bold();
            col.Item().PaddingTop(5).AlignCenter().Text(value).FontFamily(opt.HeaderFont).FontSize(13.1f).Bold();
        });
    }

    private void ComposePrintedDate(IContainer c)
    {
        var local = printedAt.ToOffset(TimeSpan.FromHours(opt.UtcOffsetHours));
        var stamp = string.Create(CultureInfo.InvariantCulture,
            $"{local:MM/dd/yyyy}  {local:h:mm:ss}{local.ToString("tt", CultureInfo.InvariantCulture).ToLowerInvariant()}");

        c.AlignRight().Width(DateBoxWidth).Border(Border).Row(r =>
        {
            r.ConstantItem(DateLabelWidth).BorderRight(Border).PaddingLeft(4).PaddingVertical(1).Text("DATE").Bold();
            r.RelativeItem().PaddingLeft(4).PaddingVertical(1).Text(stamp);
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
                    r.ConstantItem(105).PaddingLeft(5).PaddingVertical(2).Text("EMPLOYEE:").Bold();
                    r.RelativeItem().PaddingVertical(2).Text(dto.EmployeeFullName);
                });
                col.Item().Row(r =>
                {
                    r.ConstantItem(105).PaddingLeft(5).PaddingVertical(2).Text("PURPOSE OF EXPENSE:").Bold();
                    r.RelativeItem().PaddingVertical(2).Text(dto.Purpose);
                });
            });

            // Blank box under the DATE stamp, as printed by the legacy report.
            row.ConstantItem(DateBoxWidth);
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
            HeaderCell(t, "Representation", Red, rowSpan: 2, size: SmallSize);
            HeaderCell(t, "Other Exp.", Red, rowSpan: 2, size: SmallSize);
            HeaderCell(t, "VAT", Red, rowSpan: 2);
            HeaderCell(t, "GRAND\nTOTAL", Red, rowSpan: 2);
            // Header row 2 (auto-placed into the free slots under LOCAL TRAVEL)
            foreach (var sub in new[] { "MEALS", "TRANSP.", "TOLL", "PARKING", "OTHERS", "TOTAL" })
            {
                HeaderCell(t, sub, Colors.Black, size: SmallSize);
            }

            // Printed rows are ruled and sized by their estimated wrapped height (Crystal fills by height).
            foreach (var row in data.Rows)
            {
                var h = row.Height;
                switch (row)
                {
                    case R47GroupHeaderRow g:
                        RuledCell(t, h).AlignCenter()
                            .Text(g.ShowDate ? g.Date.ToString("d-MMM-yy", CultureInfo.InvariantCulture) : string.Empty)
                            .FontSize(DateSize);
                        GridText(RuledCell(t, h).PaddingLeft(GroupHeaderIndent).AlignLeft().Width(ParticularsTextWidth), g.Text);
                        for (var k = 2; k < GridColumns.Length; k++) RuledCell(t, h);
                        break;
                    case R47DetailRow d:
                        RuledCell(t, h);
                        GridText(RuledCell(t, h).PaddingLeft(DetailIndent).AlignLeft().Width(ParticularsTextWidth), d.Particulars);
                        GridText(RuledCell(t, h), d.InvoiceOrNo ?? string.Empty);
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
            var spare = Math.Max(0f, opt.DetailHeight - data.UsedHeight);
            if (spare > 0)
            {
                for (var k = 0; k < GridColumns.Length; k++)
                {
                    t.Cell().BorderVertical(Border).MinHeight(spare);
                }
            }

            // Page totals row
            var tot = data.PageTotals;
            RuledCell(t, TotalsRowHeight);
            RuledCell(t, TotalsRowHeight);
            RuledCell(t, TotalsRowHeight);
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

    private static void HeaderCell(TableDescriptor t, string text, Color color, uint rowSpan = 1, uint colSpan = 1, bool bold = true, float size = BodySize)
    {
        var cell = t.Cell();
        if (rowSpan > 1) cell = cell.RowSpan(rowSpan);
        if (colSpan > 1) cell = cell.ColumnSpan(colSpan);

        var span = cell.Border(Border).Padding(1.5f).AlignCenter().AlignMiddle().Text(text).FontSize(size).FontColor(color);
        if (bold) span.Bold();
    }

    /// <summary>A fully ruled grid cell of a fixed height (one or more text lines).</summary>
    private static IContainer RuledCell(TableDescriptor t, float height) =>
        t.Cell().Border(Border).MinHeight(height).PaddingHorizontal(CellPadding).PaddingVertical(1).AlignMiddle();

    /// <summary>Grid text at the reference size and line pitch, so wrapped rows match <see cref="R47Layout.RowHeight"/>.</summary>
    private void GridText(IContainer c, string text) =>
        c.Text(text).FontFamily(opt.GridFont).FontSize(GridTextSize).LineHeight(R47Layout.WrappedLineHeight / GridTextSize);

    private void AmountCell(TableDescriptor t, decimal value, float height) =>
        RuledCell(t, height).AlignRight().Text(value == 0 ? string.Empty : Money(value)).FontFamily(opt.GridFont).FontSize(AmountSize);

    private void TotalCell(TableDescriptor t, decimal value, bool dashZero) =>
        RuledCell(t, TotalsRowHeight).AlignRight().Text(value == 0 ? (dashZero ? "-" : string.Empty) : Money(value))
            .FontFamily(opt.GridFont).FontSize(AmountSize).Bold();

    // ---- Revolving fund + GRAND TOTAL row (same column grid for alignment) ----

    private void ComposeRevolvingFundRow(IContainer c, R47Page data)
    {
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                foreach (var w in GridColumns) cols.ConstantColumn(w);
            });

            t.Cell().ColumnSpan(3).AlignMiddle().PaddingLeft(6)
                .Text("IF WITH REVOLVING FUND. PLEASE INDICATE THE AMOUNT HERE").FontSize(SmallSize).FontColor(Blue);
            t.Cell().Border(Border).MinHeight(GrandTotalRowHeight).PaddingHorizontal(2).AlignRight().AlignMiddle()
                .Text(dto.RevolvingFundAmount == 0 ? "0" : Money(dto.RevolvingFundAmount)).FontFamily(opt.GridFont).FontSize(AmountSize);
            t.Cell().ColumnSpan(5);
            t.Cell().ColumnSpan(3).Border(Border).MinHeight(GrandTotalRowHeight).PaddingLeft(4).AlignMiddle()
                .Text("GRAND TOTAL").Bold();
            t.Cell().Border(Border).MinHeight(GrandTotalRowHeight).PaddingHorizontal(2).AlignRight().AlignMiddle()
                .Text(Money(data.PageTotals.GrandTotal, dashZero: true)).FontFamily(opt.GridFont).FontSize(AmountSize).Bold();
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
            row.ConstantItem(379).Border(Border).Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(65);
                    cols.RelativeColumn();
                    cols.ConstantColumn(70);
                    cols.ConstantColumn(80);
                });

                t.Cell().ColumnSpan(4).BorderBottom(Border).AlignCenter().PaddingVertical(1.5f)
                    .Text("CASH ADVANCE RECEIVED").FontSize(7.9f).Bold().FontColor(Red);

                foreach (var h in new[] { "DATE", "REFERENCE DOCUMENT", "REF. NO.", "Amount" })
                {
                    t.Cell().BorderBottom(Border).BorderRight(Border).AlignCenter().PaddingVertical(1.5f).Text(h).FontSize(8);
                }

                foreach (var ca in rows)
                {
                    t.Cell().BorderRight(Border).PaddingVertical(1.5f).AlignCenter()
                        .Text(ca.AdvanceDate?.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture) ?? string.Empty);
                    t.Cell().BorderRight(Border).PaddingVertical(1.5f).PaddingLeft(3).Text(ca.ReferenceDocument ?? string.Empty);
                    t.Cell().BorderRight(Border).PaddingVertical(1.5f).PaddingLeft(3).Text(ca.ReferenceNo ?? string.Empty);
                    t.Cell().BorderRight(Border).PaddingVertical(1.5f).PaddingRight(3).AlignRight()
                        .Text(ca.Amount.ToString("#,##0.00", CultureInfo.InvariantCulture)).FontFamily(opt.GridFont).FontSize(AmountSize);
                }

                t.Cell().BorderTop(Border);
                t.Cell().ColumnSpan(2).BorderTop(Border).PaddingLeft(20).PaddingVertical(1.5f).Text("TOTAL").Bold();
                t.Cell().BorderTop(Border).PaddingVertical(1.5f).PaddingRight(3).AlignRight()
                    .Text(dto.CashAdvanceTotal.ToString("#,##0.00", CultureInfo.InvariantCulture)).FontFamily(opt.GridFont).FontSize(AmountSize).Bold();
            });

            row.ConstantItem(4);

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
                        text.Span($"      (If  due to {opt.CompanyShortName}, Cashier to issue OR; indicate Ref. and OR date.)").FontSize(5.7f).FontColor(Red);
                    });
                    AmountBox(r, Money(rec.BalanceDue, dashZero: true));
                });
            });
        });
    }

    private void ReconLine(ColumnDescriptor col, string label, Color color, decimal value)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem().PaddingVertical(2).Text(label).Bold().FontColor(color);
            AmountBox(r, Money(value, dashZero: true));
        });
    }

    /// <summary>A reconciliation amount, boxed under the GRAND TOTAL column.</summary>
    private void AmountBox(RowDescriptor r, string text) =>
        r.ConstantItem(GridColumns[^1]).Border(Border).PaddingHorizontal(2).AlignRight().AlignMiddle()
            .Text(text).FontFamily(opt.GridFont).FontSize(AmountSize).Bold();

    // ---- Footer: promissory + employee signature | signatories ----

    private void ComposeFooter(IContainer c)
    {
        var sig = dto.Signatories;

        c.Border(Border).Row(row =>
        {
            row.ConstantItem(300).BorderRight(Border).PaddingHorizontal(6).PaddingTop(5).PaddingBottom(3).Column(col =>
            {
                col.Item().Text(
                        "I PROMISE TO RETURN ANY AMOUNT DUE TO THE COMPANY,\n" +
                        "TOGETHER WITH THIS EXPENSE REPORT LIQUIDATING ANY CASH\n" +
                        "ADVANCES RECEIVED. THIS ALSO AUTHORIZES THE COMPANY\n" +
                        "TO DEDUCT FROM MY SALARY, ANY UNLIQUIDATED PORTION\n" +
                        "PURSUANT TO THE POLICY ON CASH ADVANCES.")
                    .FontColor(Blue).LineHeight(1.45f);

                // Signature over printed name, like the signatory lines: the signature first so the
                // name is drawn on top of an opaque (JPEG or non-transparent PNG) background.
                col.Item().AlignCenter().Width(160).Height(20).Layers(layers =>
                {
                    if (dto.EmployeeSignature is { Length: > 0 } bytes)
                    {
                        // FitArea keeps the aspect ratio but pins the image to the left of the box;
                        // align it so a wide or short signature sits centred over the line.
                        layers.Layer().AlignCenter().AlignMiddle().Image(bytes).FitArea();
                    }

                    layers.PrimaryLayer().AlignCenter().AlignBottom().Text(dto.EmployeeFullName);
                });
                col.Item().AlignCenter().Width(160).LineHorizontal(0.8f);
                col.Item().AlignCenter().Text("EMPLOYEE'S SIGNATURE").Bold().FontSize(7.9f);
            });

            row.RelativeItem().Column(col =>
            {
                col.Item().PaddingLeft(20).PaddingTop(3).Text("(Signature Over Printed Name)").FontColor(Red);
                SignatoryLine(col, "ENDORSED BY :", sig.EndorsedBy, sig.EndorserSignature, topBorder: false);
                SignatoryLine(col, "REVIEWED BY :", sig.ReviewedBy, null, topBorder: true);
                SignatoryLine(col, "APPROVED BY :", sig.ApprovedBy, sig.ApproverSignature, topBorder: true, tall: true);
            });
        });
    }

    /// <summary>A signatory row: the approver's signature is drawn over the printed name.</summary>
    private static void SignatoryLine(ColumnDescriptor col, string label, string? name, byte[]? signature, bool topBorder, bool tall = false)
    {
        var item = col.Item();
        if (topBorder) item = item.BorderTop(Border);

        item.MinHeight(tall ? 28 : 18).PaddingLeft(20).AlignMiddle().Row(r =>
        {
            r.ConstantItem(70).AlignMiddle().Text(label).Bold();
            r.RelativeItem().Height(tall ? 28 : 18).Layers(layers =>
            {
                // Signature first so the name is drawn on top: a JPEG (or a PNG without
                // transparency) has an opaque background that would otherwise hide the name.
                if (signature is { Length: > 0 } bytes)
                {
                    layers.Layer().PaddingLeft(10).AlignMiddle().Width(90).Image(bytes).FitArea();
                }

                layers.PrimaryLayer().AlignMiddle().Text(name ?? string.Empty);
            });
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
