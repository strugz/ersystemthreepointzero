using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Reporting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Renders the R47 document: builds grid rows and fixed-size pages from the Domain layout
/// rules, then hands them to <see cref="R47Document"/> for QuestPDF generation.
/// </summary>
public sealed class R47PdfRenderer : IExpenseReportPdfRenderer
{
    private readonly R47ReportOptions _options;

    public R47PdfRenderer(IOptions<R47ReportOptions> options)
    {
        _options = options.Value;
        QuestPDF.Settings.License = Enum.TryParse<LicenseType>(_options.QuestPdfLicense, ignoreCase: true, out var license)
            ? license
            : LicenseType.Community;
    }

    public byte[] Render(ExpenseReportDocumentDto document, DateTimeOffset printedAt)
    {
        ArgumentNullException.ThrowIfNull(document);

        var rows = R47Layout.BuildRows(document.Lines);
        var pages = R47Layout.Paginate(rows, document.CashAdvanceTotal, _options.RowsPerPage);
        var pdf = new R47Document(document, pages, printedAt, _options, R47Assets.Logo);
        return pdf.GeneratePdf();
    }
}
