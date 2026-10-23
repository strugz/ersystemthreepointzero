using ERFWeb.Domain.Reporting;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Controlled-form constants and rendering knobs for the R47 "EXPENSE REPORT".
/// Bound from the <c>R47Report</c> configuration section; defaults match the current form.
/// </summary>
public sealed class R47ReportOptions
{
    public const string SectionName = "R47Report";

    public string CompanyName { get; set; } = "Marsman Drysdale Medical Products, Inc.";
    public string CompanyShortName { get; set; } = "MDMPI";
    public string Title { get; set; } = "EXPENSE REPORT";
    public string DocumentCode { get; set; } = "R47";
    public string EffectivityDate { get; set; } = "May 15, 2023";
    public string RevisionNo { get; set; } = "0";

    /// <summary>
    /// Height of the detail grid per page, in points (a single-line row is 13.3 pt, each wrapped
    /// line adds 8.2 pt). Replaces the old line-count <c>RowsPerPage</c> setting, which is ignored.
    /// </summary>
    public float DetailHeight { get; set; } = R47Layout.DefaultDetailHeight;

    /// <summary>Offset applied to the UTC print timestamp (Philippines = +8).</summary>
    public int UtcOffsetHours { get; set; } = 8;

    public string HeaderFont { get; set; } = "Calibri";
    public string BodyFont { get; set; } = "Times New Roman";

    /// <summary>Font of the grid's particulars, invoice numbers and amounts (line estimates assume Tahoma).</summary>
    public string GridFont { get; set; } = "Tahoma";

    /// <summary>QuestPDF license tier: Community, Professional, or Enterprise.</summary>
    public string QuestPdfLicense { get; set; } = "Community";
}
