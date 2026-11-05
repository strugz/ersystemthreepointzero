namespace ERFWeb.Domain.Reporting;

/// <summary>The PDF library that draws the R47 (Rules page, <c>Reports.R47Engine</c>).</summary>
public enum R47PdfEngine
{
    /// <summary>QuestPDF layout (the original R47 renderer).</summary>
    QuestPdf,

    /// <summary>PDFsharp drawing at fixed positions; prints the same form.</summary>
    PdfSharp,
}

/// <summary>
/// The R47 format the System Administrator sets on the Rules page: which PDF library draws it and the
/// controlled-form details printed in its header (company, title, document code, effectivity date,
/// revision). A value never saved falls back to the server's <c>R47Report</c> settings.
/// </summary>
public sealed record R47Format(
    R47PdfEngine Engine,
    string CompanyName,
    string CompanyShortName,
    string Title,
    string DocumentCode,
    string EffectivityDate,
    string RevisionNo);

/// <summary>Limits that keep each value inside its box on the R47 header.</summary>
public static class R47FormatRules
{
    // Measured against the header boxes: company name 424 pt at 14.8 pt bold, title 424 pt at 19.6 pt bold,
    // document code / effectivity date / revision about 96 pt at 13.1 pt bold.
    public const int CompanyNameMaxLength = 50;
    public const int CompanyShortNameMaxLength = 20;
    public const int TitleMaxLength = 30;
    public const int DocumentCodeMaxLength = 12;
    public const int EffectivityDateMaxLength = 18;
    public const int RevisionNoMaxLength = 10;

    /// <summary>A stored engine name, or null when it is missing or not a known engine.</summary>
    public static R47PdfEngine? ParseEngine(string? value) =>
        Enum.TryParse<R47PdfEngine>(value, ignoreCase: true, out var engine) && Enum.IsDefined(engine) && !int.TryParse(value, out _)
            ? engine
            : null;

    /// <summary>Field (camelCase, as the API reports it) → message, for every value that does not fit.</summary>
    public static IReadOnlyDictionary<string, string> Validate(R47Format format)
    {
        ArgumentNullException.ThrowIfNull(format);
        var errors = new Dictionary<string, string>();

        if (!Enum.IsDefined(format.Engine))
        {
            errors["r47Format.engine"] = "Choose QuestPDF or PDFsharp.";
        }

        Check(errors, "companyName", format.CompanyName, CompanyNameMaxLength, "the company name");
        Check(errors, "companyShortName", format.CompanyShortName, CompanyShortNameMaxLength, "the company short name");
        Check(errors, "title", format.Title, TitleMaxLength, "the form title");
        Check(errors, "documentCode", format.DocumentCode, DocumentCodeMaxLength, "the document code");
        Check(errors, "effectivityDate", format.EffectivityDate, EffectivityDateMaxLength, "the effectivity date");
        Check(errors, "revisionNo", format.RevisionNo, RevisionNoMaxLength, "the revision number");
        return errors;
    }

    /// <summary>The same format with every text trimmed.</summary>
    public static R47Format Normalize(R47Format format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return format with
        {
            CompanyName = format.CompanyName?.Trim() ?? string.Empty,
            CompanyShortName = format.CompanyShortName?.Trim() ?? string.Empty,
            Title = format.Title?.Trim() ?? string.Empty,
            DocumentCode = format.DocumentCode?.Trim() ?? string.Empty,
            EffectivityDate = format.EffectivityDate?.Trim() ?? string.Empty,
            RevisionNo = format.RevisionNo?.Trim() ?? string.Empty,
        };
    }

    private static void Check(Dictionary<string, string> errors, string field, string? value, int max, string what)
    {
        var v = value?.Trim() ?? string.Empty;
        if (v.Length == 0 || v.Length > max)
        {
            errors[$"r47Format.{field}"] = $"Enter {what}, up to {max} characters.";
        }
    }
}
