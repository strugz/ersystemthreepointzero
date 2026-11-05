using System.Globalization;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Expenses;
using ERFWeb.Domain.Reporting;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Lays out the R47 "EXPENSE REPORT" controlled form from an <see cref="R47Design"/>: every box, rule,
/// text and image at its position on the page, band by band (header, DATE stamp, employee block, grid,
/// revolving fund row, cash advance and reconciliation, footer, ER reference). The standard design
/// reproduces the legacy Crystal printout (MARER08OCT2026). Grid text is wrapped with the design's grid
/// metrics, the same ones pagination counted, so a row always holds the lines it was paginated for.
/// </summary>
internal sealed class R47Composer
{
    private const string Black = "#000000";
    private const string WatermarkGrey = "#9A9A9A";
    private const double Border = 0.6;
    private const double CellPadding = 1.5;
    private const double GroupHeaderIndent = 3;
    private const double DetailIndent = 5;
    private const double StampSize = 5.6;
    private const double WatermarkSize = 6;

    private readonly ExpenseReportDocumentDto _dto;
    private readonly DateTimeOffset _printedAt;
    private readonly R47ReportOptions _opt;
    private readonly R47Design _d;
    private readonly R47GridMetrics _metrics;
    private readonly byte[] _logo;
    private readonly double _left;
    private readonly double _width;
    private readonly double[] _columns;
    private readonly double[] _columnX;
    private List<R47Shape> _shapes = [];

    public R47Composer(ExpenseReportDocumentDto dto, DateTimeOffset printedAt, R47ReportOptions opt, R47Design design, byte[] logo)
    {
        _dto = dto;
        _printedAt = printedAt;
        _opt = opt;
        _d = design;
        _metrics = design.GridMetrics();
        _logo = logo;
        _left = design.MarginLeft;
        _width = design.ContentWidth();
        _columns = design.GridColumns.Select(w => (double)w).ToArray();
        _columnX = new double[_columns.Length + 1];
        _columnX[0] = _left;
        for (var i = 0; i < _columns.Length; i++)
        {
            _columnX[i + 1] = _columnX[i] + _columns[i];
        }
    }

    public R47Drawing Compose(IReadOnlyList<R47Page> pages)
    {
        var drawn = new List<IReadOnlyList<R47Shape>>(pages.Count);
        foreach (var page in pages)
        {
            _shapes = [];
            ComposePage(page);
            drawn.Add(_shapes);
        }

        return new R47Drawing($"{_opt.Title} — {_dto.EmployeeFullName}", _opt.CompanyShortName, _printedAt, drawn);
    }

    private double Right => _left + _width;
    private double DateBoxWidth => _d.DateBoxWidth();
    private string LabelColor => _d.LabelColor;
    private string NoteColor => _d.NoteColor;

    private void ComposePage(R47Page data)
    {
        var y = (double)_d.MarginTop;
        y = Header(y, data.PageNumber);
        y = PrintedDate(y);
        y = EmployeeBlock(y);
        y = Grid(y, data);
        y = RevolvingFundRow(y, data);
        y = CashAdvanceBlock(y + R47Design.CashAdvanceGap, data);
        y = Footer(y);

        Runs(new R47Box(_left, y + 3, _width, 8), R47Align.Start,
            new R47Run("ER Reference Number:   ", Body(_d.SmallSize)),
            new R47Run(_dto.ReferenceNo ?? string.Empty, Body(_d.SmallSize, color: LabelColor)));
    }

    // ---- Header: logo | company + title | Document Code / Effectivity | Page / Revision ----

    private double Header(double y, int pageNumber)
    {
        double height = _d.HeaderHeight;
        var half = height / 2;
        var titleX = _left + _d.LogoWidth;
        var codeX = titleX + _d.TitleWidth();
        var pageX = codeX + _d.DocumentCodeWidth;

        Box(_left, y, _width, height);
        // The logo shares its box with the company name (no divider), as on the reference form.
        Line(titleX, y + half, Right, y + half);
        Line(codeX, y, codeX, y + height);
        Line(pageX, y, pageX, y + height);

        _shapes.Add(new R47ImageShape(new R47Box(_left + 6, y + 6, _d.LogoWidth - 12, height - 12), _logo, R47Align.Center, R47Align.Center));

        var header = new R47TextStyle(R47FontRole.Header, _d.CompanyNameSize, Black, Bold: true);
        Text(new R47Box(titleX, y, _d.TitleWidth(), half), _opt.CompanyName, header, R47Align.Center, R47Align.Center);
        Text(new R47Box(titleX, y + half, _d.TitleWidth(), half), _opt.Title, header with { Size = _d.TitleSize }, R47Align.Center, R47Align.Center);

        InfoCell(codeX, y, _d.DocumentCodeWidth, "Document Code:", _opt.DocumentCode);
        InfoCell(pageX, y, _d.PageNoWidth, "Page No.", pageNumber.ToString(CultureInfo.InvariantCulture));
        InfoCell(codeX, y + half, _d.DocumentCodeWidth, "Effectivity Date", _opt.EffectivityDate);
        InfoCell(pageX, y + half, _d.PageNoWidth, "Revision No.", _opt.RevisionNo);

        return y + height;
    }

    private void InfoCell(double x, double y, double width, string label, string value)
    {
        var labelHeight = _d.InfoLabelSize * 1.4;
        Text(new R47Box(x + 4, y + 4, width - 8, labelHeight), label,
            new R47TextStyle(R47FontRole.Header, _d.InfoLabelSize, Black, Bold: true), R47Align.Start, R47Align.Start);
        Text(new R47Box(x + 4, y + 7.5 + labelHeight, width - 8, _d.InfoValueSize * 1.3), value,
            new R47TextStyle(R47FontRole.Header, _d.InfoValueSize, Black, Bold: true), R47Align.Center, R47Align.Start);
    }

    private double PrintedDate(double y)
    {
        var local = _printedAt.ToOffset(TimeSpan.FromHours(_opt.UtcOffsetHours));
        var stamp = string.Create(CultureInfo.InvariantCulture,
            $"{local:MM/dd/yyyy}  {local:h:mm:ss}{local.ToString("tt", CultureInfo.InvariantCulture).ToLowerInvariant()}");

        double height = _d.DateStampHeight;
        var x = Right - DateBoxWidth;
        var labelWidth = (double)_d.DateLabelWidth();
        Box(x, y, DateBoxWidth, height);
        Line(x + labelWidth, y, x + labelWidth, y + height);
        Text(new R47Box(x + 4, y, labelWidth - 4, height), "DATE", Body(_d.BodySize, bold: true), R47Align.Start, R47Align.Center);
        Text(new R47Box(x + labelWidth + 4, y, DateBoxWidth - labelWidth - 4, height), stamp, Body(_d.BodySize), R47Align.Start, R47Align.Center);

        return y + height;
    }

    private double EmployeeBlock(double y)
    {
        double line = _d.EmployeeLineHeight;
        var divider = Right - DateBoxWidth;
        Box(_left, y, _width, 2 * line);
        Line(divider, y, divider, y + 2 * line);
        Line(_left, y + line, divider, y + line);

        LabelValue(y, "EMPLOYEE:", _dto.EmployeeFullName);
        LabelValue(y + line, "PURPOSE OF EXPENSE:", _dto.Purpose);

        // The box under the DATE stamp stays blank, as printed by the legacy report.
        return y + 2 * line;
    }

    private void LabelValue(double y, string label, string value)
    {
        double line = _d.EmployeeLineHeight;
        double labelWidth = _d.EmployeeLabelWidth;
        Text(new R47Box(_left + 5, y, labelWidth, line), label, Body(_d.BodySize, bold: true), R47Align.Start, R47Align.Center);
        Text(new R47Box(_left + 5 + labelWidth, y, _width - DateBoxWidth - labelWidth - 10, line), value, Body(_d.BodySize), R47Align.Start, R47Align.Center);
    }

    // ---- Grid ----

    private double Grid(double y, R47Page data)
    {
        double top = _d.GridHeadingHeight;
        double bottom = _d.GridSubHeadingHeight;
        var headingHeight = top + bottom;

        HeadingCell(y, 0, 1, headingHeight, ["DATE"], LabelColor);
        HeadingCell(y, 1, 1, headingHeight, ["PARTICULARS"], LabelColor);
        HeadingCell(y, 2, 1, headingHeight, ["Invoice/OR", "Number"], LabelColor);
        HeadingCell(y, 3, 6, top, ["LOCAL TRAVEL"], LabelColor, bold: false);
        HeadingCell(y, 9, 1, headingHeight, ["Representation"], LabelColor, size: _d.SmallSize);
        HeadingCell(y, 10, 1, headingHeight, ["Other Exp."], LabelColor, size: _d.SmallSize);
        HeadingCell(y, 11, 1, headingHeight, ["VAT"], LabelColor);
        HeadingCell(y, 12, 1, headingHeight, ["GRAND", "TOTAL"], LabelColor);
        string[] local = ["MEALS", "TRANSP.", "TOLL", "PARKING", "OTHERS", "TOTAL"];
        for (var k = 0; k < local.Length; k++)
        {
            HeadingCell(y + top, 3 + k, 1, bottom, [local[k]], Black, size: _d.SmallSize);
        }

        y += headingHeight;

        // Printed rows are ruled and sized by their estimated wrapped height (Crystal fills by height).
        foreach (var row in data.Rows)
        {
            double h = row.Height;
            RuleRow(y, h);
            switch (row)
            {
                case R47GroupHeaderRow header:
                    if (header.ShowDate)
                    {
                        Text(CellBox(0, y, h), header.Date.ToString("d-MMM-yy", CultureInfo.InvariantCulture), Body(_d.SmallSize),
                            R47Align.Center, R47Align.Center);
                    }

                    GridText(1, GroupHeaderIndent, y, h, header.Text, _metrics.ParticularsCapacity, header.Lines);
                    break;

                case R47DetailRow detail:
                    GridText(1, DetailIndent, y, h, detail.Particulars, _metrics.ParticularsCapacity, detail.Lines);
                    GridText(2, 0, y, h, detail.InvoiceOrNo, _metrics.InvoiceCapacity, detail.Lines);
                    var b = detail.Buckets;
                    decimal[] amounts = [b.Meals, b.Transportation, b.Toll, b.Parking, b.Others, b.LocalTotal, b.Representation, b.Oth, b.Vat, b.GrandTotal];
                    for (var k = 0; k < amounts.Length; k++)
                    {
                        Amount(3 + k, y, h, amounts[k] == 0 ? string.Empty : Money(amounts[k]), bold: false);
                    }

                    break;
            }

            y += h;
        }

        // The rest of the detail area stays blank (column rules only) so the totals row is anchored.
        var spare = Math.Max(0d, _d.DetailHeight - data.UsedHeight);
        if (spare > 0)
        {
            foreach (var x in _columnX)
            {
                Line(x, y, x, y + spare);
            }

            y += spare;
        }

        double totalsHeight = _d.TotalsRowHeight;
        var t = data.PageTotals;
        RuleRow(y, totalsHeight);
        (decimal Value, bool DashZero)[] totals =
        [
            (t.Meals, true), (t.Transportation, true), (t.Toll, true), (t.Parking, true), (t.Others, true), (t.LocalTotal, true),
            (t.Representation, false), (t.Oth, false), (t.Vat, false), (t.GrandTotal, true),
        ];
        for (var k = 0; k < totals.Length; k++)
        {
            var (value, dashZero) = totals[k];
            Amount(3 + k, y, totalsHeight, value == 0 ? (dashZero ? "-" : string.Empty) : Money(value), bold: true);
        }

        return y + totalsHeight;
    }

    private void HeadingCell(double y, int column, int span, double height, string[] lines, string color, bool bold = true, double? size = null)
    {
        var x = _columnX[column];
        var width = _columnX[column + span] - x;
        Box(x, y, width, height);

        var style = Body(size ?? _d.BodySize, bold, color);
        var pitch = style.Size * 1.17;
        var top = y + (height - lines.Length * pitch) / 2;
        for (var i = 0; i < lines.Length; i++)
        {
            Text(new R47Box(x, top + i * pitch, width, pitch), lines[i], style, R47Align.Center, R47Align.Center);
        }
    }

    private void RuleRow(double y, double height)
    {
        for (var k = 0; k < _columns.Length; k++)
        {
            Box(_columnX[k], y, _columns[k], height);
        }
    }

    private R47Box CellBox(int column, double y, double height) =>
        new(_columnX[column] + CellPadding, y, _columns[column] - 2 * CellPadding, height);

    /// <summary>
    /// Grid text wrapped with the pagination metrics, at the grid line pitch, centred in its row. A row never
    /// holds more lines than pagination gave it; if the wrap ever differs, the pitch tightens instead of spilling.
    /// </summary>
    private void GridText(int column, double indent, double y, double height, string? text, int capacity, int rowLines)
    {
        var lines = R47TextMetrics.Wrap(text, capacity);
        var pitch = Math.Min(_metrics.LineHeight, (height - 2) / Math.Max(lines.Count, rowLines));
        var top = y + (height - lines.Count * pitch) / 2;
        var x = _columnX[column] + CellPadding + indent;
        var width = _columnX[column + 1] - CellPadding - x;
        var style = new R47TextStyle(R47FontRole.Grid, _d.GridTextSize, Black);
        for (var i = 0; i < lines.Count; i++)
        {
            Text(new R47Box(x, top + i * pitch, width, pitch), lines[i], style, R47Align.Start, R47Align.Center);
        }
    }

    private void Amount(int column, double y, double height, string text, bool bold)
    {
        if (text.Length > 0)
        {
            Text(CellBox(column, y, height), text, Grid(_d.AmountSize, bold), R47Align.End, R47Align.Center);
        }
    }

    // ---- Revolving fund + GRAND TOTAL row (same column grid for alignment) ----

    private double RevolvingFundRow(double y, R47Page data)
    {
        double h = _d.GrandTotalRowHeight;
        Text(new R47Box(_columnX[0] + 6, y, _columnX[3] - _columnX[0] - 6, h), _d.RevolvingFundText, Body(_d.SmallSize, color: NoteColor),
            R47Align.Start, R47Align.Center);

        Box(_columnX[3], y, _columns[3], h);
        Text(new R47Box(_columnX[3] + 2, y, _columns[3] - 4, h), _dto.RevolvingFundAmount == 0 ? "0" : Money(_dto.RevolvingFundAmount),
            Grid(_d.AmountSize), R47Align.End, R47Align.Center);

        Box(_columnX[9], y, _columnX[12] - _columnX[9], h);
        Text(new R47Box(_columnX[9] + 4, y, _columnX[12] - _columnX[9] - 4, h), "GRAND TOTAL", Body(_d.BodySize, bold: true), R47Align.Start, R47Align.Center);

        Box(_columnX[12], y, _columns[12], h);
        Text(new R47Box(_columnX[12] + 2, y, _columns[12] - 4, h), Money(data.PageTotals.GrandTotal, dashZero: true),
            Grid(_d.AmountSize, bold: true), R47Align.End, R47Align.Center);

        return y + h;
    }

    // ---- Cash advance received (left) + reconciliation (right) ----

    private double CashAdvanceBlock(double y, R47Page data)
    {
        double boxWidth = _d.CashAdvanceWidth;
        const double titleHeight = R47Design.CashAdvanceTitleHeight;
        const double headingHeight = R47Design.CashAdvanceHeadingHeight;
        double rowHeight = _d.CashAdvanceRowHeight;
        double[] widths = [65, boxWidth - 65 - 70 - 80, 70, 80];
        var x = new double[widths.Length + 1];
        x[0] = _left;
        for (var i = 0; i < widths.Length; i++)
        {
            x[i + 1] = x[i] + widths[i];
        }

        // At least one (blank) row so the block keeps its printed shape.
        var rows = _dto.CashAdvances.Count > 0 ? _dto.CashAdvances : [new R47CashAdvanceRow(null, null, null, 0m)];
        double height = _d.CashAdvanceHeight(rows.Count);

        Box(_left, y, boxWidth, height);
        Text(new R47Box(_left, y, boxWidth, titleHeight), "CASH ADVANCE RECEIVED", Body(_d.BodySize + 0.8, bold: true, color: LabelColor),
            R47Align.Center, R47Align.Center);
        Line(_left, y + titleHeight, _left + boxWidth, y + titleHeight);

        var headingTop = y + titleHeight;
        string[] headings = ["DATE", "REFERENCE DOCUMENT", "REF. NO.", "Amount"];
        for (var i = 0; i < headings.Length; i++)
        {
            Text(new R47Box(x[i], headingTop, widths[i], headingHeight), headings[i], Body(_d.BodySize + 0.9), R47Align.Center, R47Align.Center);
        }

        var rowsTop = headingTop + headingHeight;
        var totalTop = rowsTop + rows.Count * rowHeight;
        Line(_left, rowsTop, _left + boxWidth, rowsTop);
        for (var i = 1; i < widths.Length; i++)
        {
            Line(x[i], headingTop, x[i], totalTop);
        }

        var body = Body(_d.BodySize);
        for (var r = 0; r < rows.Count; r++)
        {
            var ca = rows[r];
            var top = rowsTop + r * rowHeight;
            Text(new R47Box(x[0], top, widths[0], rowHeight), ca.AdvanceDate?.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture) ?? string.Empty,
                body, R47Align.Center, R47Align.Center);
            Text(new R47Box(x[1] + 3, top, widths[1] - 3, rowHeight), ca.ReferenceDocument ?? string.Empty, body, R47Align.Start, R47Align.Center);
            Text(new R47Box(x[2] + 3, top, widths[2] - 3, rowHeight), ca.ReferenceNo ?? string.Empty, body, R47Align.Start, R47Align.Center);
            Text(new R47Box(x[3], top, widths[3] - 3, rowHeight), ca.Amount.ToString("#,##0.00", CultureInfo.InvariantCulture),
                Grid(_d.AmountSize), R47Align.End, R47Align.Center);
        }

        Line(_left, totalTop, _left + boxWidth, totalTop);
        Text(new R47Box(x[1] + 20, totalTop, widths[1] - 20, rowHeight), "TOTAL", Body(_d.BodySize, bold: true), R47Align.Start, R47Align.Center);
        Text(new R47Box(x[3], totalTop, widths[3] - 3, rowHeight), _dto.CashAdvanceTotal.ToString("#,##0.00", CultureInfo.InvariantCulture),
            Grid(_d.AmountSize, bold: true), R47Align.End, R47Align.Center);

        // Reconciliation, each amount boxed under the GRAND TOTAL column.
        double line = _d.ReconLineHeight;
        var rec = data.Reconciliation;
        var reconX = _left + boxWidth + 4;
        ReconLine(reconX, y, "CASH ADVANCE, if any", rec.CarriedBalance);
        ReconLine(reconX, y + line, "LESS EXPENSE (THIS EXPENSE REPORT)", rec.LessExpense);

        var balTop = y + 2 * line;
        var bold = Body(_d.BodySize, bold: true);
        // The legacy report never stored this; it follows the sign of the remainder.
        var dueTo = BalanceDueRules.DueTo(rec.BalanceDue, _opt.CompanyShortName);
        var note = _d.BalanceNote(_opt.CompanyShortName);
        Runs(new R47Box(reconX, balTop, Right - _columns[^1] - reconX - 2, line), R47Align.Center,
            new R47Run("BAL DUE TO:   ", bold),
            new R47Run(dueTo, bold),
            new R47Run(note.Length == 0 ? string.Empty : "      " + note, Body(_d.SmallSize - 0.5, color: LabelColor)));
        AmountBox(balTop, Money(rec.BalanceDue, dashZero: true));

        return y + height;
    }

    private void ReconLine(double x, double y, string label, decimal value)
    {
        Text(new R47Box(x, y, Right - _columns[^1] - x - 2, _d.ReconLineHeight), label, Body(_d.BodySize, bold: true, color: LabelColor),
            R47Align.Start, R47Align.Center);
        AmountBox(y, Money(value, dashZero: true));
    }

    private void AmountBox(double y, string text)
    {
        var width = _columns[^1];
        var x = Right - width;
        Box(x, y, width, _d.ReconLineHeight);
        Text(new R47Box(x + 2, y, width - 4, _d.ReconLineHeight), text, Grid(_d.AmountSize, bold: true), R47Align.End, R47Align.Center);
    }

    // ---- Footer: promissory + employee signature | signatories ----

    private double Footer(double y)
    {
        double leftWidth = _d.FooterLeftWidth;
        double height = _d.FooterHeight;
        Box(_left, y, _width, height);
        Line(_left + leftWidth, y, _left + leftWidth, y + height);

        var promise = _d.PromiseLines();
        var body = Body(_d.BodySize);
        double pitch = _d.PromisePitch();
        var promiseAlign = _d.PromiseAlign == R47DesignRules.AlignCenter ? R47Align.Center : R47Align.Start;
        for (var i = 0; i < promise.Count; i++)
        {
            Text(new R47Box(_left + 6, y + 5 + i * pitch, leftWidth - 12, pitch), promise[i], body with { Color = NoteColor }, promiseAlign, R47Align.Center);
        }

        // Signature over printed name: signature first, so the name is drawn on top of an opaque
        // image; the electronic stamp sits at the top of the same box, so the footer keeps its height.
        var sig = new R47Box(_left + (leftWidth - 160) / 2, y + 5 + promise.Count * pitch, 160, 20);
        if (_dto.EmployeeSignature is { Length: > 0 } employeeSignature)
        {
            _shapes.Add(new R47ImageShape(sig, employeeSignature, R47Align.Center, R47Align.Center));
            Watermark(sig);
        }

        if (EmployeeStamp() is { } employeeStamp)
        {
            Text(sig, employeeStamp, Stamp(), R47Align.Center, R47Align.Start);
        }

        Text(sig, _dto.EmployeeFullName, body, R47Align.Center, R47Align.End);
        _shapes.Add(new R47LineShape(sig.X, sig.Bottom, sig.Right, sig.Bottom, 0.8));
        Text(new R47Box(_left, sig.Bottom + 1, leftWidth, 10), "EMPLOYEE'S SIGNATURE", Body(_d.BodySize + 0.8, bold: true), R47Align.Center, R47Align.Start);

        // Signatories.
        var s = _dto.Signatories;
        var x = _left + leftWidth;
        Text(new R47Box(x + 20, y + 3, 200, 9), "(Signature Over Printed Name)", body with { Color = LabelColor }, R47Align.Start, R47Align.Center);

        var top = y + 12;
        top = SignedSignatoryLine(x, top, "ENDORSED BY :", s.EndorsedBy, s.EndorserSignature, SignatoryStamp(s.EndorserSignature, s.EndorsedAt), topBorder: false);

        Line(x, top, Right, top);
        Text(new R47Box(x + 20, top, 70, 18), "REVIEWED BY :", Body(_d.BodySize, bold: true), R47Align.Start, R47Align.Center);
        Text(new R47Box(x + 90, top, Right - x - 90, 18), s.ReviewedBy ?? string.Empty, body, R47Align.Start, R47Align.Center);
        top += 18;

        SignedSignatoryLine(x, top, "APPROVED BY :", s.ApprovedBy, s.ApproverSignature, SignatoryStamp(s.ApproverSignature, s.ApprovedAt), topBorder: true);

        return y + height;
    }

    /// <summary>
    /// A signatory row whose signature sits in its own 18 pt band above the printed name, so the
    /// signature never covers the name; the band is kept when unsigned so the form keeps its height.
    /// A printed signature carries the ELECTRONIC SIGNATURE watermark, and the stamp sits beside it.
    /// </summary>
    private double SignedSignatoryLine(double x, double y, string label, string? name, byte[]? signature, string? stamp, bool topBorder)
    {
        const double band = 18;
        const double nameHeight = 9;
        if (topBorder)
        {
            Line(x, y, Right, y);
        }

        Text(new R47Box(x + 20, y, 70, band + nameHeight), label, Body(_d.BodySize, bold: true), R47Align.Start, R47Align.End);

        var bandLeft = x + 90;
        var signed = signature is { Length: > 0 };
        if (signed)
        {
            var box = new R47Box(bandLeft + 10, y, 90, band);
            _shapes.Add(new R47ImageShape(box, signature!, R47Align.Start, R47Align.End));
            Watermark(box);
        }

        if (stamp is not null)
        {
            var stampLeft = signed ? bandLeft + 104 : bandLeft + 10;
            Text(new R47Box(stampLeft, y, Right - stampLeft, band), stamp, Stamp(), R47Align.Start, R47Align.End);
        }

        Text(new R47Box(bandLeft, y + band, Right - bandLeft, nameHeight), name ?? string.Empty, Body(_d.BodySize), R47Align.Start, R47Align.Start);

        return y + band + nameHeight + 3;
    }

    private void Watermark(R47Box box) =>
        Text(box, R47Signatures.Watermark, Body(WatermarkSize, bold: true, color: WatermarkGrey), R47Align.Center, R47Align.Center);

    /// <summary>The R47 signature rule: signed image → signed stamp; approved without one → approved stamp; null before approval.</summary>
    private string? SignatoryStamp(byte[]? signature, DateTimeOffset? approvedAt) => approvedAt switch
    {
        null => null,
        { } at when signature is { Length: > 0 } => R47Signatures.SignedStamp(at, _opt.UtcOffsetHours, _dto.ReferenceNo),
        { } at => R47Signatures.ApprovedStamp(at, _opt.UtcOffsetHours),
    };

    private string? EmployeeStamp() => _dto.FiledAt switch
    {
        null => null,
        { } at when _dto.EmployeeSignature is { Length: > 0 } => R47Signatures.SignedStamp(at, _opt.UtcOffsetHours, _dto.ReferenceNo),
        { } at => R47Signatures.FiledStamp(at, _opt.UtcOffsetHours),
    };

    // ---- Shapes and styles ----

    private R47TextStyle Body(double size, bool bold = false, string? color = null) => new(R47FontRole.Body, size, color ?? Black, bold);

    private static R47TextStyle Grid(double size, bool bold = false) => new(R47FontRole.Grid, size, Black, bold);

    private R47TextStyle Stamp() => new(R47FontRole.Body, StampSize, NoteColor, Italic: true);

    private void Box(double x, double y, double width, double height) => _shapes.Add(new R47RectShape(new R47Box(x, y, width, height), Border));

    private void Line(double x1, double y1, double x2, double y2) => _shapes.Add(new R47LineShape(x1, y1, x2, y2, Border));

    private void Text(R47Box box, string text, R47TextStyle style, R47Align horizontal, R47Align vertical)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _shapes.Add(new R47TextShape(box, text, style, horizontal, vertical));
        }
    }

    private void Runs(R47Box box, R47Align vertical, params R47Run[] runs) =>
        _shapes.Add(new R47RunsShape(box, runs.Where(r => r.Text.Length > 0).ToArray(), vertical));

    /// <summary>"#,##0.00" with negatives in parentheses; zero → "-" when dashZero, else "0.00".</summary>
    internal static string Money(decimal value, bool dashZero = false)
    {
        if (value == 0)
        {
            return dashZero ? "-" : "0.00";
        }

        var text = Math.Abs(value).ToString("#,##0.00", CultureInfo.InvariantCulture);
        return value < 0 ? $"({text})" : text;
    }
}
