using System.Globalization;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Expenses;
using ERFWeb.Domain.Reporting;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// The R47 "EXPENSE REPORT" controlled form drawn with PDFsharp (<c>R47Report:Engine</c> = <c>PdfSharp</c>).
/// It prints the same pages as the QuestPDF <see cref="R47Document"/> from the same Domain layout
/// (<see cref="R47Layout"/>, <see cref="R47Signatures"/>), but PDFsharp has no layout engine, so every
/// box, rule and text is placed at a fixed position measured from the legacy Crystal printout
/// (US Letter landscape, 792×612 pt). Grid text wraps with the same Tahoma metrics pagination uses
/// (<see cref="R47TextMetrics"/>), so a row always holds the lines it was paginated for.
/// </summary>
internal sealed class R47PdfSharpDocument(
    ExpenseReportDocumentDto dto,
    IReadOnlyList<R47Page> pages,
    DateTimeOffset printedAt,
    R47ReportOptions opt,
    byte[] logo)
{
    private static readonly XColor Red = XColor.FromArgb(0xC0, 0x00, 0x00);
    private static readonly XColor Blue = XColor.FromArgb(0x1F, 0x1F, 0xBF);
    private static readonly XColor WatermarkGrey = XColor.FromArgb(0x9A, 0x9A, 0x9A);
    private static readonly XColor Black = XColors.Black;

    private const double PageWidth = 792;
    private const double PageHeight = 612;
    private const double Left = 25;
    private const double Top = 8;
    private const double Width = 748;
    private const double Border = 0.6;

    // Type sizes of the reference printout (pt), as in R47Document.
    private const double BodySize = 7.1;
    private const double GridTextSize = 5.8;
    private const double AmountSize = 6.6;
    private const double DateSize = 6.2;
    private const double SmallSize = 6.2;
    private const double StampSize = 5.6;

    // Fixed heights of the boxes QuestPDF sizes from their content.
    private const double HeaderHeight = 88;
    private const double DateStampHeight = 10.5;
    private const double EmployeeLineHeight = 12.3;
    private const double GridHeadingTop = 11;
    private const double GridHeadingBottom = 10.5;
    private const double TotalsRowHeight = 15.2;
    private const double GrandTotalRowHeight = 13.5;
    private const double ReconLineHeight = 12.3;
    private const double FooterHeight = 90;

    private const double CellPadding = 1.5;
    private const double GroupHeaderIndent = 3;
    private const double DetailIndent = 5;

    // Grid column widths (pt); sum = 748 = page width − margins.
    private static readonly double[] GridColumns = [45, 148, 51, 50, 53, 53, 50, 60, 57.5, 54, 36, 37, 53.5];
    private static readonly double[] ColumnX = ColumnStarts();
    private static readonly double DateBoxWidth = GridColumns[^4..].Sum();
    private static readonly double DateLabelWidth = GridColumns[^4] + GridColumns[^3];

    private readonly Dictionary<(string Family, double Size, XFontStyleEx Style), XFont> _fonts = [];

    public byte[] GeneratePdf()
    {
        PdfSharpFonts.EnsureRegistered();

        using var document = new PdfDocument();
        document.Info.Title = $"{opt.Title} — {dto.EmployeeFullName}";
        document.Info.Author = opt.CompanyShortName;
        document.Info.CreationDate = printedAt.ToOffset(TimeSpan.FromHours(opt.UtcOffsetHours)).DateTime;

        var images = new Images(
            Logo: Image(logo)!,
            Employee: Image(dto.EmployeeSignature),
            Endorser: Image(dto.Signatories.EndorserSignature),
            Approver: Image(dto.Signatories.ApproverSignature));

        foreach (var data in pages)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(PageWidth);
            page.Height = XUnit.FromPoint(PageHeight);

            using var gfx = XGraphics.FromPdfPage(page);
            DrawPage(gfx, data, images);
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private sealed record Images(XImage Logo, XImage? Employee, XImage? Endorser, XImage? Approver);

    private static XImage? Image(byte[]? bytes) =>
        bytes is { Length: > 0 } ? XImage.FromStream(new MemoryStream(bytes, writable: false)) : null;

    private static double[] ColumnStarts()
    {
        var x = new double[GridColumns.Length + 1];
        x[0] = Left;
        for (var i = 0; i < GridColumns.Length; i++)
        {
            x[i + 1] = x[i] + GridColumns[i];
        }

        return x;
    }

    private void DrawPage(XGraphics g, R47Page data, Images images)
    {
        var y = Top;
        y = DrawHeader(g, y, data.PageNumber, images.Logo);
        y = DrawPrintedDate(g, y);
        y = DrawEmployeeBlock(g, y);
        y = DrawGrid(g, y, data);
        y = DrawRevolvingFundRow(g, y, data);
        y = DrawCashAdvanceBlock(g, y + 1, data);
        y = DrawFooter(g, y, images);

        var label = "ER Reference Number:   ";
        var small = Font(opt.BodyFont, SmallSize);
        var textTop = y + 3;
        g.DrawString(label, small, Brush(Black), new XRect(Left, textTop, Width, 8), XStringFormats.TopLeft);
        g.DrawString(dto.ReferenceNo ?? string.Empty, small, Brush(Red),
            new XRect(Left + g.MeasureString(label, small).Width, textTop, Width, 8), XStringFormats.TopLeft);
    }

    // ---- Header: logo | company + title | Document Code / Effectivity | Page / Revision ----

    private double DrawHeader(XGraphics g, double y, int pageNumber, XImage logoImage)
    {
        const double logoColumn = 118;
        const double codeColumn = 104;
        const double pageColumn = 102;
        const double half = HeaderHeight / 2;
        var titleColumn = Width - logoColumn - codeColumn - pageColumn;
        var titleX = Left + logoColumn;
        var codeX = titleX + titleColumn;
        var pageX = codeX + codeColumn;

        Box(g, Left, y, Width, HeaderHeight);
        // The logo shares its box with the company name (no divider), as on the reference form.
        Line(g, titleX, y + half, Left + Width, y + half);
        Line(g, codeX, y, codeX, y + HeaderHeight);
        Line(g, pageX, y, pageX, y + HeaderHeight);

        DrawImageFit(g, logoImage, new XRect(Left + 6, y + 6, logoColumn - 12, HeaderHeight - 12), XStringFormats.Center);

        g.DrawString(opt.CompanyName, Font(opt.HeaderFont, 14.8, XFontStyleEx.Bold), Brush(Black),
            new XRect(titleX, y, titleColumn, half), XStringFormats.Center);
        g.DrawString(opt.Title, Font(opt.HeaderFont, 19.6, XFontStyleEx.Bold), Brush(Black),
            new XRect(titleX, y + half, titleColumn, half), XStringFormats.Center);

        InfoCell(g, codeX, y, codeColumn, "Document Code:", opt.DocumentCode);
        InfoCell(g, pageX, y, pageColumn, "Page No.", pageNumber.ToString(CultureInfo.InvariantCulture));
        InfoCell(g, codeX, y + half, codeColumn, "Effectivity Date", opt.EffectivityDate);
        InfoCell(g, pageX, y + half, pageColumn, "Revision No.", opt.RevisionNo);

        return y + HeaderHeight;
    }

    private void InfoCell(XGraphics g, double x, double y, double width, string label, string value)
    {
        g.DrawString(label, Font(opt.HeaderFont, 8.2, XFontStyleEx.Bold), Brush(Black),
            new XRect(x + 4, y + 4, width - 8, 10), XStringFormats.TopLeft);
        g.DrawString(value, Font(opt.HeaderFont, 13.1, XFontStyleEx.Bold), Brush(Black),
            new XRect(x + 4, y + 19, width - 8, 16), XStringFormats.TopCenter);
    }

    private double DrawPrintedDate(XGraphics g, double y)
    {
        var local = printedAt.ToOffset(TimeSpan.FromHours(opt.UtcOffsetHours));
        var stamp = string.Create(CultureInfo.InvariantCulture,
            $"{local:MM/dd/yyyy}  {local:h:mm:ss}{local.ToString("tt", CultureInfo.InvariantCulture).ToLowerInvariant()}");

        var x = Left + Width - DateBoxWidth;
        Box(g, x, y, DateBoxWidth, DateStampHeight);
        Line(g, x + DateLabelWidth, y, x + DateLabelWidth, y + DateStampHeight);
        g.DrawString("DATE", Font(opt.BodyFont, BodySize, XFontStyleEx.Bold), Brush(Black),
            new XRect(x + 4, y, DateLabelWidth - 4, DateStampHeight), XStringFormats.CenterLeft);
        g.DrawString(stamp, Font(opt.BodyFont, BodySize), Brush(Black),
            new XRect(x + DateLabelWidth + 4, y, DateBoxWidth - DateLabelWidth - 4, DateStampHeight), XStringFormats.CenterLeft);

        return y + DateStampHeight;
    }

    private double DrawEmployeeBlock(XGraphics g, double y)
    {
        var height = 2 * EmployeeLineHeight;
        var divider = Left + Width - DateBoxWidth;
        Box(g, Left, y, Width, height);
        Line(g, divider, y, divider, y + height);
        Line(g, Left, y + EmployeeLineHeight, divider, y + EmployeeLineHeight);

        LabelValue(g, y, "EMPLOYEE:", dto.EmployeeFullName);
        LabelValue(g, y + EmployeeLineHeight, "PURPOSE OF EXPENSE:", dto.Purpose);

        // The box under the DATE stamp stays blank, as printed by the legacy report.
        return y + height;
    }

    private void LabelValue(XGraphics g, double y, string label, string value)
    {
        g.DrawString(label, Font(opt.BodyFont, BodySize, XFontStyleEx.Bold), Brush(Black),
            new XRect(Left + 5, y, 100, EmployeeLineHeight), XStringFormats.CenterLeft);
        g.DrawString(value, Font(opt.BodyFont, BodySize), Brush(Black),
            new XRect(Left + 105, y, Width - DateBoxWidth - 110, EmployeeLineHeight), XStringFormats.CenterLeft);
    }

    // ---- Grid ----

    private double DrawGrid(XGraphics g, double y, R47Page data)
    {
        var headingHeight = GridHeadingTop + GridHeadingBottom;

        HeadingCell(g, y, 0, 1, headingHeight, ["DATE"], Red);
        HeadingCell(g, y, 1, 1, headingHeight, ["PARTICULARS"], Red);
        HeadingCell(g, y, 2, 1, headingHeight, ["Invoice/OR", "Number"], Red);
        HeadingCell(g, y, 3, 6, GridHeadingTop, ["LOCAL TRAVEL"], Red, bold: false);
        HeadingCell(g, y, 9, 1, headingHeight, ["Representation"], Red, size: SmallSize);
        HeadingCell(g, y, 10, 1, headingHeight, ["Other Exp."], Red, size: SmallSize);
        HeadingCell(g, y, 11, 1, headingHeight, ["VAT"], Red);
        HeadingCell(g, y, 12, 1, headingHeight, ["GRAND", "TOTAL"], Red);
        string[] local = ["MEALS", "TRANSP.", "TOLL", "PARKING", "OTHERS", "TOTAL"];
        for (var k = 0; k < local.Length; k++)
        {
            HeadingCell(g, y + GridHeadingTop, 3 + k, 1, GridHeadingBottom, [local[k]], Black, size: SmallSize);
        }

        y += headingHeight;

        // Printed rows are ruled and sized by their estimated wrapped height (Crystal fills by height).
        foreach (var row in data.Rows)
        {
            var h = (double)row.Height;
            RuleRow(g, y, h);
            switch (row)
            {
                case R47GroupHeaderRow header:
                    if (header.ShowDate)
                    {
                        g.DrawString(header.Date.ToString("d-MMM-yy", CultureInfo.InvariantCulture), Font(opt.BodyFont, DateSize),
                            Brush(Black), CellRect(0, y, h), XStringFormats.Center);
                    }

                    GridText(g, ColumnX[1] + CellPadding + GroupHeaderIndent, y, h, header.Text, R47TextMetrics.ParticularsCapacity, header.Lines);
                    break;

                case R47DetailRow detail:
                    GridText(g, ColumnX[1] + CellPadding + DetailIndent, y, h, detail.Particulars, R47TextMetrics.ParticularsCapacity, detail.Lines);
                    GridText(g, ColumnX[2] + CellPadding, y, h, detail.InvoiceOrNo, R47TextMetrics.InvoiceCapacity, detail.Lines);
                    var b = detail.Buckets;
                    decimal[] amounts = [b.Meals, b.Transportation, b.Toll, b.Parking, b.Others, b.LocalTotal, b.Representation, b.Oth, b.Vat, b.GrandTotal];
                    for (var k = 0; k < amounts.Length; k++)
                    {
                        Amount(g, 3 + k, y, h, amounts[k] == 0 ? string.Empty : Money(amounts[k]), bold: false);
                    }

                    break;
            }

            y += h;
        }

        // The rest of the detail area stays blank (column rules only) so the totals row is anchored.
        var spare = Math.Max(0d, opt.DetailHeight - data.UsedHeight);
        if (spare > 0)
        {
            foreach (var x in ColumnX)
            {
                Line(g, x, y, x, y + spare);
            }

            y += spare;
        }

        var t = data.PageTotals;
        RuleRow(g, y, TotalsRowHeight);
        (decimal Value, bool DashZero)[] totals =
        [
            (t.Meals, true), (t.Transportation, true), (t.Toll, true), (t.Parking, true), (t.Others, true), (t.LocalTotal, true),
            (t.Representation, false), (t.Oth, false), (t.Vat, false), (t.GrandTotal, true),
        ];
        for (var k = 0; k < totals.Length; k++)
        {
            var (value, dashZero) = totals[k];
            Amount(g, 3 + k, y, TotalsRowHeight, value == 0 ? (dashZero ? "-" : string.Empty) : Money(value), bold: true);
        }

        return y + TotalsRowHeight;
    }

    private void HeadingCell(XGraphics g, double y, int column, int span, double height, string[] lines, XColor color, bool bold = true, double size = BodySize)
    {
        var x = ColumnX[column];
        var width = ColumnX[column + span] - x;
        Box(g, x, y, width, height);

        var font = Font(opt.BodyFont, size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular);
        var pitch = size * 1.17;
        var top = y + (height - lines.Length * pitch) / 2;
        for (var i = 0; i < lines.Length; i++)
        {
            g.DrawString(lines[i], font, Brush(color), new XRect(x, top + i * pitch, width, pitch), XStringFormats.Center);
        }
    }

    private static void RuleRow(XGraphics g, double y, double height)
    {
        for (var k = 0; k < GridColumns.Length; k++)
        {
            Box(g, ColumnX[k], y, GridColumns[k], height);
        }
    }

    private static XRect CellRect(int column, double y, double height) =>
        new(ColumnX[column] + CellPadding, y, GridColumns[column] - 2 * CellPadding, height);

    /// <summary>
    /// Grid text wrapped with the pagination metrics, at the reference line pitch, centred in its row.
    /// A row never holds more lines than pagination gave it; if the measured wrap ever differs, the
    /// pitch tightens instead of spilling into the next row.
    /// </summary>
    private void GridText(XGraphics g, double x, double y, double height, string? text, int capacity, int rowLines)
    {
        var lines = R47TextMetrics.Wrap(text, capacity);
        var pitch = Math.Min(R47Layout.WrappedLineHeight, (height - 2) / Math.Max(lines.Count, rowLines));
        var top = y + (height - lines.Count * pitch) / 2;
        var font = Font(opt.GridFont, GridTextSize);
        for (var i = 0; i < lines.Count; i++)
        {
            g.DrawString(lines[i], font, Brush(Black), new XRect(x, top + i * pitch, 200, pitch), XStringFormats.CenterLeft);
        }
    }

    private void Amount(XGraphics g, int column, double y, double height, string text, bool bold)
    {
        if (text.Length == 0)
        {
            return;
        }

        g.DrawString(text, Font(opt.GridFont, AmountSize, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular), Brush(Black),
            CellRect(column, y, height), XStringFormats.CenterRight);
    }

    // ---- Revolving fund + GRAND TOTAL row (same column grid for alignment) ----

    private double DrawRevolvingFundRow(XGraphics g, double y, R47Page data)
    {
        const double h = GrandTotalRowHeight;
        g.DrawString("IF WITH REVOLVING FUND. PLEASE INDICATE THE AMOUNT HERE", Font(opt.BodyFont, SmallSize), Brush(Blue),
            new XRect(ColumnX[0] + 6, y, ColumnX[3] - ColumnX[0] - 6, h), XStringFormats.CenterLeft);

        Box(g, ColumnX[3], y, GridColumns[3], h);
        g.DrawString(dto.RevolvingFundAmount == 0 ? "0" : Money(dto.RevolvingFundAmount), Font(opt.GridFont, AmountSize), Brush(Black),
            new XRect(ColumnX[3] + 2, y, GridColumns[3] - 4, h), XStringFormats.CenterRight);

        Box(g, ColumnX[9], y, ColumnX[12] - ColumnX[9], h);
        g.DrawString("GRAND TOTAL", Font(opt.BodyFont, BodySize, XFontStyleEx.Bold), Brush(Black),
            new XRect(ColumnX[9] + 4, y, ColumnX[12] - ColumnX[9] - 4, h), XStringFormats.CenterLeft);

        Box(g, ColumnX[12], y, GridColumns[12], h);
        g.DrawString(Money(data.PageTotals.GrandTotal, dashZero: true), Font(opt.GridFont, AmountSize, XFontStyleEx.Bold), Brush(Black),
            new XRect(ColumnX[12] + 2, y, GridColumns[12] - 4, h), XStringFormats.CenterRight);

        return y + h;
    }

    // ---- Cash advance received (left) + reconciliation (right) ----

    private double DrawCashAdvanceBlock(XGraphics g, double y, R47Page data)
    {
        const double boxWidth = 379;
        const double titleHeight = 12;
        const double headingHeight = 12.5;
        const double rowHeight = 11.5;
        double[] widths = [65, boxWidth - 65 - 70 - 80, 70, 80];
        var x = new double[widths.Length + 1];
        x[0] = Left;
        for (var i = 0; i < widths.Length; i++)
        {
            x[i + 1] = x[i] + widths[i];
        }

        // At least one (blank) row so the block keeps its printed shape.
        var rows = dto.CashAdvances.Count > 0 ? dto.CashAdvances : [new R47CashAdvanceRow(null, null, null, 0m)];
        var tableHeight = titleHeight + headingHeight + rows.Count * rowHeight + rowHeight;
        var height = Math.Max(tableHeight, 3 * ReconLineHeight);

        Box(g, Left, y, boxWidth, height);
        g.DrawString("CASH ADVANCE RECEIVED", Font(opt.BodyFont, 7.9, XFontStyleEx.Bold), Brush(Red),
            new XRect(Left, y, boxWidth, titleHeight), XStringFormats.Center);
        Line(g, Left, y + titleHeight, Left + boxWidth, y + titleHeight);

        var headingTop = y + titleHeight;
        string[] headings = ["DATE", "REFERENCE DOCUMENT", "REF. NO.", "Amount"];
        for (var i = 0; i < headings.Length; i++)
        {
            g.DrawString(headings[i], Font(opt.BodyFont, 8), Brush(Black), new XRect(x[i], headingTop, widths[i], headingHeight), XStringFormats.Center);
        }

        var rowsTop = headingTop + headingHeight;
        var totalTop = rowsTop + rows.Count * rowHeight;
        Line(g, Left, rowsTop, Left + boxWidth, rowsTop);
        for (var i = 1; i < widths.Length; i++)
        {
            Line(g, x[i], headingTop, x[i], totalTop);
        }

        var body = Font(opt.BodyFont, BodySize);
        var amount = Font(opt.GridFont, AmountSize);
        for (var r = 0; r < rows.Count; r++)
        {
            var ca = rows[r];
            var top = rowsTop + r * rowHeight;
            g.DrawString(ca.AdvanceDate?.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture) ?? string.Empty, body, Brush(Black),
                new XRect(x[0], top, widths[0], rowHeight), XStringFormats.Center);
            g.DrawString(ca.ReferenceDocument ?? string.Empty, body, Brush(Black), new XRect(x[1] + 3, top, widths[1] - 3, rowHeight), XStringFormats.CenterLeft);
            g.DrawString(ca.ReferenceNo ?? string.Empty, body, Brush(Black), new XRect(x[2] + 3, top, widths[2] - 3, rowHeight), XStringFormats.CenterLeft);
            g.DrawString(ca.Amount.ToString("#,##0.00", CultureInfo.InvariantCulture), amount, Brush(Black),
                new XRect(x[3], top, widths[3] - 3, rowHeight), XStringFormats.CenterRight);
        }

        Line(g, Left, totalTop, Left + boxWidth, totalTop);
        g.DrawString("TOTAL", Font(opt.BodyFont, BodySize, XFontStyleEx.Bold), Brush(Black),
            new XRect(x[1] + 20, totalTop, widths[1] - 20, rowHeight), XStringFormats.CenterLeft);
        g.DrawString(dto.CashAdvanceTotal.ToString("#,##0.00", CultureInfo.InvariantCulture), Font(opt.GridFont, AmountSize, XFontStyleEx.Bold),
            Brush(Black), new XRect(x[3], totalTop, widths[3] - 3, rowHeight), XStringFormats.CenterRight);

        // Reconciliation, each amount boxed under the GRAND TOTAL column.
        var rec = data.Reconciliation;
        var reconX = Left + boxWidth + 4;
        var bold = Font(opt.BodyFont, BodySize, XFontStyleEx.Bold);
        ReconLine(g, reconX, y, "CASH ADVANCE, if any", rec.CarriedBalance);
        ReconLine(g, reconX, y + ReconLineHeight, "LESS EXPENSE (THIS EXPENSE REPORT)", rec.LessExpense);

        var balTop = y + 2 * ReconLineHeight;
        var lead = "BAL DUE TO:   ";
        // The legacy report never stored this; it follows the sign of the remainder.
        var dueTo = BalanceDueRules.DueTo(rec.BalanceDue, opt.CompanyShortName);
        var cursor = reconX;
        g.DrawString(lead, bold, Brush(Black), new XRect(cursor, balTop, 200, ReconLineHeight), XStringFormats.CenterLeft);
        cursor += g.MeasureString(lead, bold).Width;
        g.DrawString(dueTo, bold, Brush(Black), new XRect(cursor, balTop, 200, ReconLineHeight), XStringFormats.CenterLeft);
        cursor += g.MeasureString(dueTo, bold).Width;
        g.DrawString($"      (If  due to {opt.CompanyShortName}, Cashier to issue OR; indicate Ref. and OR date.)", Font(opt.BodyFont, 5.7),
            Brush(Red), new XRect(cursor, balTop, 300, ReconLineHeight), XStringFormats.CenterLeft);
        AmountBox(g, balTop, Money(rec.BalanceDue, dashZero: true));

        return y + height;
    }

    private void ReconLine(XGraphics g, double x, double y, string label, decimal value)
    {
        g.DrawString(label, Font(opt.BodyFont, BodySize, XFontStyleEx.Bold), Brush(Red),
            new XRect(x, y, 300, ReconLineHeight), XStringFormats.CenterLeft);
        AmountBox(g, y, Money(value, dashZero: true));
    }

    private void AmountBox(XGraphics g, double y, string text)
    {
        var width = GridColumns[^1];
        var x = Left + Width - width;
        Box(g, x, y, width, ReconLineHeight);
        g.DrawString(text, Font(opt.GridFont, AmountSize, XFontStyleEx.Bold), Brush(Black),
            new XRect(x + 2, y, width - 4, ReconLineHeight), XStringFormats.CenterRight);
    }

    // ---- Footer: promissory + employee signature | signatories ----

    private double DrawFooter(XGraphics g, double y, Images images)
    {
        const double leftWidth = 300;
        Box(g, Left, y, Width, FooterHeight);
        Line(g, Left + leftWidth, y, Left + leftWidth, y + FooterHeight);

        string[] promise =
        [
            "I PROMISE TO RETURN ANY AMOUNT DUE TO THE COMPANY,",
            "TOGETHER WITH THIS EXPENSE REPORT LIQUIDATING ANY CASH",
            "ADVANCES RECEIVED. THIS ALSO AUTHORIZES THE COMPANY",
            "TO DEDUCT FROM MY SALARY, ANY UNLIQUIDATED PORTION",
            "PURSUANT TO THE POLICY ON CASH ADVANCES.",
        ];
        var body = Font(opt.BodyFont, BodySize);
        var pitch = BodySize * 1.45;
        for (var i = 0; i < promise.Length; i++)
        {
            g.DrawString(promise[i], body, Brush(Blue), new XRect(Left + 6, y + 5 + i * pitch, leftWidth - 12, pitch), XStringFormats.CenterLeft);
        }

        // Signature over printed name: signature first, so the name is drawn on top of an opaque
        // image; the electronic stamp sits at the top of the same box, so the footer keeps its height.
        var sig = new XRect(Left + (leftWidth - 160) / 2, y + 5 + promise.Length * pitch, 160, 20);
        if (images.Employee is { } employeeSignature)
        {
            DrawImageFit(g, employeeSignature, sig, XStringFormats.Center);
            Watermark(g, sig);
        }

        if (EmployeeStamp() is { } employeeStamp)
        {
            g.DrawString(employeeStamp, Font(opt.BodyFont, StampSize, XFontStyleEx.Italic), Brush(Blue), sig, XStringFormats.TopCenter);
        }

        g.DrawString(dto.EmployeeFullName, body, Brush(Black), sig, XStringFormats.BottomCenter);
        g.DrawLine(new XPen(Black, 0.8), sig.Left, sig.Bottom, sig.Right, sig.Bottom);
        g.DrawString("EMPLOYEE'S SIGNATURE", Font(opt.BodyFont, 7.9, XFontStyleEx.Bold), Brush(Black),
            new XRect(Left, sig.Bottom + 1, leftWidth, 10), XStringFormats.TopCenter);

        // Signatories.
        var s = dto.Signatories;
        var x = Left + leftWidth;
        var right = Left + Width;
        g.DrawString("(Signature Over Printed Name)", body, Brush(Red), new XRect(x + 20, y + 3, 200, 9), XStringFormats.CenterLeft);

        var top = y + 12;
        top = SignedSignatoryLine(g, x, right, top, "ENDORSED BY :", s.EndorsedBy, images.Endorser, SignatoryStamp(images.Endorser, s.EndorsedAt), topBorder: false);

        Line(g, x, top, right, top);
        g.DrawString("REVIEWED BY :", Font(opt.BodyFont, BodySize, XFontStyleEx.Bold), Brush(Black), new XRect(x + 20, top, 70, 18), XStringFormats.CenterLeft);
        g.DrawString(s.ReviewedBy ?? string.Empty, body, Brush(Black), new XRect(x + 90, top, right - x - 90, 18), XStringFormats.CenterLeft);
        top += 18;

        SignedSignatoryLine(g, x, right, top, "APPROVED BY :", s.ApprovedBy, images.Approver, SignatoryStamp(images.Approver, s.ApprovedAt), topBorder: true);

        return y + FooterHeight;
    }

    /// <summary>
    /// A signatory row whose signature sits in its own 18 pt band above the printed name, so the
    /// signature never covers the name; the band is kept when unsigned so the form keeps its height.
    /// A printed signature carries the ELECTRONIC SIGNATURE watermark, and the stamp sits beside it.
    /// </summary>
    private double SignedSignatoryLine(XGraphics g, double x, double right, double y, string label, string? name, XImage? signature, string? stamp, bool topBorder)
    {
        const double band = 18;
        const double nameHeight = 9;
        const double height = band + nameHeight + 3;
        if (topBorder)
        {
            Line(g, x, y, right, y);
        }

        g.DrawString(label, Font(opt.BodyFont, BodySize, XFontStyleEx.Bold), Brush(Black),
            new XRect(x + 20, y, 70, band + nameHeight), XStringFormats.BottomLeft);

        var bandLeft = x + 90;
        if (signature is not null)
        {
            var box = new XRect(bandLeft + 10, y, 90, band);
            DrawImageFit(g, signature, box, XStringFormats.BottomLeft);
            Watermark(g, box);
        }

        if (stamp is not null)
        {
            var stampLeft = signature is not null ? bandLeft + 104 : bandLeft + 10;
            g.DrawString(stamp, Font(opt.BodyFont, StampSize, XFontStyleEx.Italic), Brush(Blue),
                new XRect(stampLeft, y, right - stampLeft, band), XStringFormats.BottomLeft);
        }

        g.DrawString(name ?? string.Empty, Font(opt.BodyFont, BodySize), Brush(Black),
            new XRect(bandLeft, y + band, right - bandLeft, nameHeight), XStringFormats.TopLeft);

        return y + height;
    }

    private void Watermark(XGraphics g, XRect box) =>
        g.DrawString(R47Signatures.Watermark, Font(opt.BodyFont, 6, XFontStyleEx.Bold), Brush(WatermarkGrey), box, XStringFormats.Center);

    /// <summary>Same rule as <see cref="R47Document"/>: signed image → signed stamp; approved without one → approved stamp.</summary>
    private string? SignatoryStamp(XImage? signature, DateTimeOffset? approvedAt) => approvedAt switch
    {
        null => null,
        { } at when signature is not null => R47Signatures.SignedStamp(at, opt.UtcOffsetHours, dto.ReferenceNo),
        { } at => R47Signatures.ApprovedStamp(at, opt.UtcOffsetHours),
    };

    private string? EmployeeStamp() => dto.FiledAt switch
    {
        null => null,
        { } at when dto.EmployeeSignature is { Length: > 0 } => R47Signatures.SignedStamp(at, opt.UtcOffsetHours, dto.ReferenceNo),
        { } at => R47Signatures.FiledStamp(at, opt.UtcOffsetHours),
    };

    // ---- Drawing helpers ----

    /// <summary>Draws an image scaled to fit <paramref name="area"/>, keeping its aspect ratio, aligned within it.</summary>
    private static void DrawImageFit(XGraphics g, XImage image, XRect area, XStringFormat align)
    {
        var scale = Math.Min(area.Width / image.PixelWidth, area.Height / image.PixelHeight);
        var w = image.PixelWidth * scale;
        var h = image.PixelHeight * scale;
        var x = align.Alignment switch
        {
            XStringAlignment.Center => area.X + (area.Width - w) / 2,
            XStringAlignment.Far => area.Right - w,
            _ => area.X,
        };
        var y = align.LineAlignment switch
        {
            XLineAlignment.Center => area.Y + (area.Height - h) / 2,
            XLineAlignment.Far or XLineAlignment.BaseLine => area.Bottom - h,
            _ => area.Y,
        };
        g.DrawImage(image, x, y, w, h);
    }

    private static void Box(XGraphics g, double x, double y, double width, double height) =>
        g.DrawRectangle(new XPen(Black, Border), x, y, width, height);

    private static void Line(XGraphics g, double x1, double y1, double x2, double y2) =>
        g.DrawLine(new XPen(Black, Border), x1, y1, x2, y2);

    private static XSolidBrush Brush(XColor color) => new(color);

    private XFont Font(string family, double size, XFontStyleEx style = XFontStyleEx.Regular)
    {
        var key = (family, size, style);
        if (!_fonts.TryGetValue(key, out var font))
        {
            font = new XFont(family, size, style);
            _fonts[key] = font;
        }

        return font;
    }

    private static string Money(decimal value, bool dashZero = false) => R47Document.Money(value, dashZero);
}
