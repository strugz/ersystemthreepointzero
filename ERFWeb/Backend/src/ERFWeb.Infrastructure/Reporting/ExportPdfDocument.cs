using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>A column of an export table; widths are relative. Numeric columns align right.</summary>
internal sealed record PdfColumn(string Header, float Width, bool Numeric = false);

/// <summary>One titled table of an export. The total row, when present, is printed bold under a rule.</summary>
internal sealed record PdfTable(string Heading, IReadOnlyList<PdfColumn> Columns, IReadOnlyList<string[]> Rows, string[]? TotalRow = null);

/// <summary>
/// The shared layout of the summary and aging PDFs: company and title header, the filters that
/// produced the export, one or more tables whose header repeats on every page, and a footer with
/// who generated it, when, and the page count. US Letter landscape, like the R47.
/// </summary>
internal sealed class ExportPdfDocument(
    string title,
    IReadOnlyList<string> subtitle,
    IReadOnlyList<PdfTable> tables,
    string generatedAt,
    string generatedBy,
    R47ReportOptions opt) : IDocument
{
    private static readonly Color Rule = Color.FromHex("#9E9E9E");
    private static readonly Color HeaderFill = Color.FromHex("#ECEFF1");

    public DocumentMetadata GetMetadata() => new() { Title = title, Author = opt.CompanyShortName };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.Letter.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(s => s.FontFamily(opt.HeaderFont).FontSize(8));

            page.Header().PaddingBottom(8).Column(col =>
            {
                col.Item().Text(opt.CompanyName).FontSize(11).Bold();
                col.Item().Text(title).FontSize(15).Bold();
                foreach (var line in subtitle)
                {
                    col.Item().Text(line).FontSize(9);
                }
            });

            page.Content().Column(col =>
            {
                foreach (var table in tables)
                {
                    col.Item().PaddingTop(6).PaddingBottom(3).Text(table.Heading).FontSize(10).Bold();
                    col.Item().Element(c => ComposeTable(c, table));
                }
            });

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"Generated {generatedAt} by {generatedBy}").FontSize(7);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(s => s.FontSize(7));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });
    }

    private static void ComposeTable(IContainer container, PdfTable table)
    {
        if (table.Rows.Count == 0)
        {
            container.PaddingVertical(4).Text("No records.").Italic();
            return;
        }

        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                foreach (var column in table.Columns)
                {
                    cols.RelativeColumn(column.Width);
                }
            });

            t.Header(h =>
            {
                foreach (var column in table.Columns)
                {
                    Align(h.Cell().Background(HeaderFill).BorderBottom(0.8f).BorderColor(Rule).Padding(3), column)
                        .Text(column.Header).Bold();
                }
            });

            foreach (var row in table.Rows)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    Align(t.Cell().BorderBottom(0.3f).BorderColor(Rule).PaddingVertical(2).PaddingHorizontal(3), table.Columns[i])
                        .Text(i < row.Length ? row[i] : string.Empty);
                }
            }

            if (table.TotalRow is { } total)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    Align(t.Cell().BorderTop(0.8f).BorderColor(Rule).PaddingVertical(3).PaddingHorizontal(3), table.Columns[i])
                        .Text(i < total.Length ? total[i] : string.Empty).Bold();
                }
            }
        });
    }

    private static IContainer Align(IContainer cell, PdfColumn column) => column.Numeric ? cell.AlignRight() : cell.AlignLeft();
}
