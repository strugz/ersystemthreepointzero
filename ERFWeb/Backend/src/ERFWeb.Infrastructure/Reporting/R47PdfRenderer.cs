using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Reporting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Renders the R47 document: builds grid rows and fixed-size pages from the Domain layout
/// rules, then hands them to the engine of the R47 format (Rules page, else <c>R47Report:Engine</c>):
/// <see cref="R47Document"/> (QuestPDF, default) or <see cref="R47PdfSharpDocument"/> (PDFsharp).
/// The format's header details replace the server's; fonts, sizes and the time offset stay the server's.
/// </summary>
public sealed class R47PdfRenderer : IExpenseReportPdfRenderer
{
    private readonly R47ReportOptions _options;

    public R47PdfRenderer(IOptions<R47ReportOptions> options)
    {
        _options = options.Value;
        QuestPdfSetup.Apply(_options);
    }

    public byte[] Render(ExpenseReportDocumentDto document, DateTimeOffset printedAt)
    {
        ArgumentNullException.ThrowIfNull(document);

        var options = document.Format is { } format ? _options.WithFormat(format) : _options;
        var rows = R47Layout.BuildRows(document.Lines);
        var pages = R47Layout.Paginate(rows, document.CashAdvanceTotal, options.DetailHeight);
        return options.Engine switch
        {
            R47PdfEngine.PdfSharp => new R47PdfSharpDocument(document, pages, printedAt, options, R47Assets.Logo).GeneratePdf(),
            _ => new R47Document(document, pages, printedAt, options, R47Assets.Logo).GeneratePdf(),
        };
    }
}
