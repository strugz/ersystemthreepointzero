using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Reporting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Renders the R47 document: builds grid rows and fixed-size pages from the Domain layout rules with the
/// R47 design (the designer's saved layout, else the standard form), lays the pages out once with
/// <see cref="R47Composer"/>, then hands the shapes to the engine of the R47 format (Rules page, else
/// <c>R47Report:Engine</c>): <see cref="R47Document"/> (QuestPDF, default) or <see cref="R47PdfSharpDocument"/> (PDFsharp).
/// The format's header details replace the server's; fonts and the time offset stay the server's.
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
        var design = document.Design ?? _options.DefaultDesign();
        var rows = R47Layout.BuildRows(document.Lines, design.GridMetrics());
        var pages = R47Layout.Paginate(rows, document.CashAdvanceTotal, design.DetailHeight);
        var drawing = new R47Composer(document, printedAt, options, design, R47Assets.Logo).Compose(pages);
        return options.Engine switch
        {
            R47PdfEngine.PdfSharp => new R47PdfSharpDocument(drawing, options).GeneratePdf(),
            _ => new R47Document(drawing, options).GeneratePdf(),
        };
    }
}
