using System.Globalization;
using System.Text.RegularExpressions;

namespace ERFWeb.Domain.Reporting;

/// <summary>
/// The layout of the R47 form, set in the R47 designer (Rules page). The form is a stack of bands drawn
/// top to bottom on a US Letter landscape page (792×612 pt): header, DATE stamp, employee block, grid
/// headings, detail area, page totals, revolving fund / GRAND TOTAL row, cash advance and reconciliation,
/// footer, ER reference line. The designer changes sizes, widths, type sizes, colours and fixed texts; the
/// band order and what each band prints are part of the controlled form and do not change.
/// Every value defaults to the standard form measured from the legacy Crystal printout, so a design saved
/// by an older designer (missing properties) keeps the standard value for them. All sizes are points.
/// </summary>
public sealed record R47Design
{
    public const float PageWidth = 792f;
    public const float PageHeight = 612f;

    /// <summary>The standard form, as printed before the designer existed.</summary>
    public static R47Design Standard { get; } = new();

    // Page
    public float MarginLeft { get; init; } = 25f;
    public float MarginRight { get; init; } = 19f;
    public float MarginTop { get; init; } = 8f;

    // Header: logo | company name over title | Document Code / Effectivity | Page / Revision
    public float HeaderHeight { get; init; } = 88f;
    public float LogoWidth { get; init; } = 118f;
    public float DocumentCodeWidth { get; init; } = 104f;
    public float PageNoWidth { get; init; } = 102f;
    public float CompanyNameSize { get; init; } = 14.8f;
    public float TitleSize { get; init; } = 19.6f;
    public float InfoLabelSize { get; init; } = 8.2f;
    public float InfoValueSize { get; init; } = 13.1f;

    // DATE stamp and employee block
    public float DateStampHeight { get; init; } = 10.5f;
    public float EmployeeLineHeight { get; init; } = 12.3f;
    public float EmployeeLabelWidth { get; init; } = 100f;

    // Grid: DATE, PARTICULARS, Invoice/OR, MEALS, TRANSP., TOLL, PARKING, OTHERS, TOTAL,
    // Representation, Other Exp., VAT, GRAND TOTAL. They fill the width between the margins.
    public IReadOnlyList<float> GridColumns { get; init; } = [45f, 148f, 51f, 50f, 53f, 53f, 50f, 60f, 57.5f, 54f, 36f, 37f, 53.5f];
    public float GridHeadingHeight { get; init; } = 11f;
    public float GridSubHeadingHeight { get; init; } = 10.5f;

    /// <summary>Height of a grid row whose texts fit on one line; wrapped rows grow by the grid line pitch.</summary>
    public float SingleRowHeight { get; init; } = R47Layout.SingleRowHeight;

    /// <summary>Height of the detail area per page (how many rows a page holds).</summary>
    public float DetailHeight { get; init; } = R47Layout.DefaultDetailHeight;

    public float TotalsRowHeight { get; init; } = 15.2f;
    public float GrandTotalRowHeight { get; init; } = 13.5f;

    // Cash advance received (left) and reconciliation (right)
    public float CashAdvanceWidth { get; init; } = 379f;
    public float CashAdvanceRowHeight { get; init; } = 11.5f;
    public float ReconLineHeight { get; init; } = 12.3f;

    // Footer: promise and employee signature | signatories
    public float FooterHeight { get; init; } = 90f;
    public float FooterLeftWidth { get; init; } = 300f;

    // Type sizes
    public float BodySize { get; init; } = 7.1f;
    public float SmallSize { get; init; } = 6.2f;
    public float GridTextSize { get; init; } = 5.8f;
    public float AmountSize { get; init; } = 6.6f;

    // Colours (#RRGGBB): headings and labels, and notes (promise, revolving fund line, stamps)
    public string LabelColor { get; init; } = "#C00000";
    public string NoteColor { get; init; } = "#1F1FBF";

    // Fixed texts
    public string PromiseText { get; init; } =
        "I PROMISE TO RETURN ANY AMOUNT DUE TO THE COMPANY,\n" +
        "TOGETHER WITH THIS EXPENSE REPORT LIQUIDATING ANY CASH\n" +
        "ADVANCES RECEIVED. THIS ALSO AUTHORIZES THE COMPANY\n" +
        "TO DEDUCT FROM MY SALARY, ANY UNLIQUIDATED PORTION\n" +
        "PURSUANT TO THE POLICY ON CASH ADVANCES.";

    /// <summary>
    /// How the promise lines sit in their box: <see cref="R47DesignRules.AlignLeft"/> (the Crystal form) or
    /// <see cref="R47DesignRules.AlignCenter"/>, centred over the employee's signature.
    /// </summary>
    public string PromiseAlign { get; init; } = R47DesignRules.AlignLeft;

    public string RevolvingFundText { get; init; } = "IF WITH REVOLVING FUND. PLEASE INDICATE THE AMOUNT HERE";

    /// <summary>The note after BAL DUE TO; <c>{ShortName}</c> prints the company short name.</summary>
    public string BalanceNoteText { get; init; } = "(If  due to {ShortName}, Cashier to issue OR; indicate Ref. and OR date.)";

    // ---- Derived geometry, shared by both PDF engines and the designer rules ----

    /// <summary>Width between the left and right margins; the grid columns add up to it.</summary>
    public float ContentWidth() => PageWidth - MarginLeft - MarginRight;

    /// <summary>The DATE stamp sits over the last four grid columns so its lines continue the grid's.</summary>
    public float DateBoxWidth() => GridColumns.Skip(GridColumns.Count - 4).Sum();

    /// <summary>The DATE label spans Representation + Other Exp., so its divider meets the VAT column line.</summary>
    public float DateLabelWidth() => GridColumns[^4] + GridColumns[^3];

    public float TitleWidth() => ContentWidth() - LogoWidth - DocumentCodeWidth - PageNoWidth;

    public const float CashAdvanceTitleHeight = 12f;
    public const float CashAdvanceHeadingHeight = 12.5f;

    /// <summary>Height of the cash advance block for <paramref name="rows"/> rows (at least one blank row prints).</summary>
    public float CashAdvanceHeight(int rows) =>
        Math.Max(CashAdvanceTitleHeight + CashAdvanceHeadingHeight + (Math.Max(rows, 1) + 1) * CashAdvanceRowHeight, 3 * ReconLineHeight);

    public IReadOnlyList<string> PromiseLines() =>
        PromiseText.Split('\n', StringSplitOptions.TrimEntries).Where(l => l.Length > 0).ToArray();

    /// <summary>Line pitch of the promise paragraph.</summary>
    public float PromisePitch() => BodySize * 1.45f;

    /// <summary>Gap between the cash advance block and the band above it, and room for the ER reference line.</summary>
    public const float CashAdvanceGap = 1f;
    public const float ReferenceLineHeight = 11f;

    /// <summary>The signatory column needs this much height: caption, two signed lines and REVIEWED BY.</summary>
    public const float SignatoriesHeight = 90f;

    /// <summary>Height of the whole form with <paramref name="cashAdvanceRows"/> cash advance rows.</summary>
    public float FormHeight(int cashAdvanceRows = 1) =>
        MarginTop + HeaderHeight + DateStampHeight + 2 * EmployeeLineHeight + GridHeadingHeight + GridSubHeadingHeight
        + DetailHeight + TotalsRowHeight + GrandTotalRowHeight + CashAdvanceGap + CashAdvanceHeight(cashAdvanceRows)
        + FooterHeight + ReferenceLineHeight;

    /// <summary>Height the promise and the employee signature need in the footer's left box.</summary>
    public float PromiseBlockHeight() => 5f + PromiseLines().Count * PromisePitch() + 20f + 11f;

    /// <summary>The note after BAL DUE TO with the company short name filled in.</summary>
    public string BalanceNote(string companyShortName) =>
        BalanceNoteText.Replace("{ShortName}", companyShortName, StringComparison.Ordinal);

    /// <summary>Wrap capacities and row heights the grid prints with, so pagination and both engines agree.</summary>
    public R47GridMetrics GridMetrics() => R47GridMetrics.For(GridColumns[1], GridColumns[2], GridTextSize, SingleRowHeight);
}

/// <summary>
/// How the grid measures text: the wrap capacities of PARTICULARS and Invoice/OR (thousandths of an em of
/// the Tahoma grid font) and the row heights. The standard design gives exactly the measured constants of
/// <see cref="R47TextMetrics"/> and <see cref="R47Layout"/>; other widths and sizes scale them.
/// </summary>
public sealed record R47GridMetrics(int ParticularsCapacity, int InvoiceCapacity, float SingleRowHeight, float LineHeight)
{
    /// <summary>Part of the PARTICULARS column that is not text: cell padding, indent and Crystal's slack.</summary>
    public const float ParticularsSlack = 29f;

    /// <summary>Part of the Invoice/OR column that is not text: cell padding.</summary>
    public const float InvoiceSlack = 3.4f;

    public const float StandardGridTextSize = 5.8f;
    private const float StandardParticularsWidth = 148f;
    private const float StandardInvoiceWidth = 51f;
    private const float RowPadding = 2f;

    public static R47GridMetrics Standard { get; } =
        new(R47TextMetrics.ParticularsCapacity, R47TextMetrics.InvoiceCapacity, R47Layout.SingleRowHeight, R47Layout.WrappedLineHeight);

    /// <summary>Printed height of a row whose tallest text wraps to <paramref name="lines"/> lines.</summary>
    public float RowHeight(int lines) => Math.Max(SingleRowHeight, lines * LineHeight + RowPadding);

    public static R47GridMetrics For(float particularsWidth, float invoiceWidth, float gridTextSize, float singleRowHeight)
    {
        var size = gridTextSize / StandardGridTextSize;
        return new R47GridMetrics(
            Scale(R47TextMetrics.ParticularsCapacity, (particularsWidth - ParticularsSlack) / (StandardParticularsWidth - ParticularsSlack), size),
            Scale(R47TextMetrics.InvoiceCapacity, (invoiceWidth - InvoiceSlack) / (StandardInvoiceWidth - InvoiceSlack), size),
            singleRowHeight,
            Math.Abs(size - 1f) < 0.0001f ? R47Layout.WrappedLineHeight : R47Layout.WrappedLineHeight * size);
    }

    private static int Scale(int standard, float width, float size) =>
        Math.Max(1, (int)Math.Round(standard * width / size, MidpointRounding.AwayFromZero));
}

/// <summary>Limits of the R47 designer: every size within a range, and the form must fit on the page.</summary>
public static partial class R47DesignRules
{
    public const int PromiseMaxLines = 6;
    public const int PromiseLineMaxLength = 70;
    public const int RevolvingFundTextMaxLength = 80;
    public const int BalanceNoteTextMaxLength = 100;
    public const int GridColumnCount = 13;
    public const float MinGridColumnWidth = 20f;

    /// <summary>Values of <see cref="R47Design.PromiseAlign"/>.</summary>
    public const string AlignLeft = "Left";
    public const string AlignCenter = "Center";

    /// <summary>Allowed range of each size, keyed by its camelCase property name.</summary>
    public static readonly IReadOnlyDictionary<string, (float Min, float Max)> Ranges = new Dictionary<string, (float, float)>
    {
        ["marginLeft"] = (0f, 60f),
        ["marginRight"] = (0f, 60f),
        ["marginTop"] = (0f, 40f),
        ["headerHeight"] = (50f, 150f),
        ["logoWidth"] = (40f, 250f),
        ["documentCodeWidth"] = (60f, 200f),
        ["pageNoWidth"] = (60f, 200f),
        ["companyNameSize"] = (8f, 24f),
        ["titleSize"] = (10f, 28f),
        ["infoLabelSize"] = (6f, 12f),
        ["infoValueSize"] = (8f, 18f),
        ["dateStampHeight"] = (8f, 20f),
        ["employeeLineHeight"] = (9f, 24f),
        ["employeeLabelWidth"] = (60f, 200f),
        ["gridHeadingHeight"] = (8f, 24f),
        ["gridSubHeadingHeight"] = (8f, 24f),
        ["singleRowHeight"] = (10f, 24f),
        ["detailHeight"] = (60f, 400f),
        ["totalsRowHeight"] = (10f, 30f),
        ["grandTotalRowHeight"] = (10f, 30f),
        ["cashAdvanceRowHeight"] = (9f, 20f),
        ["reconLineHeight"] = (9f, 24f),
        ["footerHeight"] = (R47Design.SignatoriesHeight, 150f),
        ["bodySize"] = (5f, 10f),
        ["smallSize"] = (4.5f, 9f),
        ["gridTextSize"] = (4.5f, 8f),
        ["amountSize"] = (5f, 9f),
        ["cashAdvanceWidth"] = (250f, 450f),
        ["footerLeftWidth"] = (200f, 450f),
    };

    /// <summary>Errors keyed <c>r47Design.&lt;field&gt;</c>; empty when the design can be saved.</summary>
    public static IReadOnlyDictionary<string, string> Validate(R47Design design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        void Error(string field, string message) => errors.TryAdd($"r47Design.{field}", message);

        foreach (var (field, (min, max)) in Ranges)
        {
            var value = Value(design, field);
            if (!float.IsFinite(value) || value < min || value > max)
            {
                Error(field, $"Enter {Number(min)} to {Number(max)} pt.");
            }
        }

        if (design.GridColumns is null || design.GridColumns.Count != GridColumnCount)
        {
            Error("gridColumns", $"The grid needs {GridColumnCount} column widths.");
        }
        else if (design.GridColumns.Any(w => !float.IsFinite(w) || w < MinGridColumnWidth))
        {
            Error("gridColumns", $"Each grid column needs at least {Number(MinGridColumnWidth)} pt.");
        }
        else if (design.GridColumns[1] < 80f || design.GridColumns[2] < 30f)
        {
            Error("gridColumns", "PARTICULARS needs at least 80 pt and Invoice/OR at least 30 pt.");
        }
        else if (Math.Abs(design.GridColumns.Sum() - design.ContentWidth()) > 0.5f)
        {
            Error("gridColumns", $"The grid columns add up to {Number(design.GridColumns.Sum())} pt; they must fill the {Number(design.ContentWidth())} pt between the margins.");
        }

        if (!errors.ContainsKey("r47Design.gridColumns") && !errors.Keys.Any(k => k.Contains("Width", StringComparison.Ordinal) || k.Contains("margin", StringComparison.Ordinal)))
        {
            if (design.TitleWidth() < 150f)
            {
                Error("logoWidth", "The company name and title need at least 150 pt; narrow the logo, Document Code or Page No. boxes.");
            }

            if (design.EmployeeLabelWidth > design.ContentWidth() - design.DateBoxWidth() - 50f)
            {
                Error("employeeLabelWidth", "The label leaves no room for the employee's name.");
            }

            if (design.CashAdvanceWidth > design.ContentWidth() - 330f)
            {
                Error("cashAdvanceWidth", "The reconciliation beside the cash advance box needs at least 330 pt.");
            }

            if (design.FooterLeftWidth > design.ContentWidth() - 300f)
            {
                Error("footerLeftWidth", "The signatories beside the promise need at least 300 pt.");
            }
        }

        foreach (var (field, color) in new[] { ("labelColor", design.LabelColor), ("noteColor", design.NoteColor) })
        {
            if (color is null || !HexColor().IsMatch(color))
            {
                Error(field, "Enter a colour as #RRGGBB.");
            }
        }

        var promise = design.PromiseText is null ? [] : design.PromiseLines();
        if (promise.Count == 0 || promise.Count > PromiseMaxLines || promise.Any(l => l.Length > PromiseLineMaxLength))
        {
            Error("promiseText", $"Enter 1 to {PromiseMaxLines} lines of up to {PromiseLineMaxLength} characters.");
        }

        if (design.PromiseAlign is not (AlignLeft or AlignCenter))
        {
            Error("promiseAlign", "Choose Left or Center.");
        }

        if (string.IsNullOrWhiteSpace(design.RevolvingFundText) || design.RevolvingFundText.Trim().Length > RevolvingFundTextMaxLength)
        {
            Error("revolvingFundText", $"Enter the revolving fund line, up to {RevolvingFundTextMaxLength} characters.");
        }

        if (design.BalanceNoteText is null || design.BalanceNoteText.Trim().Length > BalanceNoteTextMaxLength)
        {
            Error("balanceNoteText", $"Enter up to {BalanceNoteTextMaxLength} characters (it may be blank).");
        }

        // The fit checks only mean something once each size is in range.
        if (errors.Count == 0)
        {
            if (design.PromiseBlockHeight() > design.FooterHeight)
            {
                Error("footerHeight", $"The promise and the employee's signature need {Number(design.PromiseBlockHeight())} pt; make the footer taller, the text shorter or the body type smaller.");
            }

            if (design.FormHeight() > R47Design.PageHeight)
            {
                Error("detailHeight", $"The form is {Number(design.FormHeight())} pt tall and the page {Number(R47Design.PageHeight)} pt; make the detail area or another band shorter.");
            }
        }

        return errors;
    }

    /// <summary>Trims the texts, keeps one line break between promise lines and writes colours in capitals.</summary>
    public static R47Design Normalize(R47Design design)
    {
        ArgumentNullException.ThrowIfNull(design);
        return design with
        {
            PromiseText = string.Join('\n', (design.PromiseText ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.TrimEntries).Where(l => l.Length > 0)),
            RevolvingFundText = design.RevolvingFundText?.Trim() ?? string.Empty,
            BalanceNoteText = design.BalanceNoteText?.Trim() ?? string.Empty,
            PromiseAlign = string.Equals(design.PromiseAlign?.Trim(), AlignCenter, StringComparison.OrdinalIgnoreCase) ? AlignCenter
                : string.Equals(design.PromiseAlign?.Trim(), AlignLeft, StringComparison.OrdinalIgnoreCase) ? AlignLeft
                : design.PromiseAlign ?? string.Empty,
            LabelColor = design.LabelColor?.Trim().ToUpperInvariant() ?? string.Empty,
            NoteColor = design.NoteColor?.Trim().ToUpperInvariant() ?? string.Empty,
            GridColumns = design.GridColumns?.ToArray() ?? [],
        };
    }

    private static float Value(R47Design d, string field) => field switch
    {
        "marginLeft" => d.MarginLeft,
        "marginRight" => d.MarginRight,
        "marginTop" => d.MarginTop,
        "headerHeight" => d.HeaderHeight,
        "logoWidth" => d.LogoWidth,
        "documentCodeWidth" => d.DocumentCodeWidth,
        "pageNoWidth" => d.PageNoWidth,
        "companyNameSize" => d.CompanyNameSize,
        "titleSize" => d.TitleSize,
        "infoLabelSize" => d.InfoLabelSize,
        "infoValueSize" => d.InfoValueSize,
        "dateStampHeight" => d.DateStampHeight,
        "employeeLineHeight" => d.EmployeeLineHeight,
        "employeeLabelWidth" => d.EmployeeLabelWidth,
        "gridHeadingHeight" => d.GridHeadingHeight,
        "gridSubHeadingHeight" => d.GridSubHeadingHeight,
        "singleRowHeight" => d.SingleRowHeight,
        "detailHeight" => d.DetailHeight,
        "totalsRowHeight" => d.TotalsRowHeight,
        "grandTotalRowHeight" => d.GrandTotalRowHeight,
        "cashAdvanceRowHeight" => d.CashAdvanceRowHeight,
        "reconLineHeight" => d.ReconLineHeight,
        "footerHeight" => d.FooterHeight,
        "bodySize" => d.BodySize,
        "smallSize" => d.SmallSize,
        "gridTextSize" => d.GridTextSize,
        "amountSize" => d.AmountSize,
        "cashAdvanceWidth" => d.CashAdvanceWidth,
        "footerLeftWidth" => d.FooterLeftWidth,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown R47 design size."),
    };

    private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();
}
