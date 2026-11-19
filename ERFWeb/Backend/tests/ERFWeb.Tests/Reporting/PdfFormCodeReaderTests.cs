using ERFWeb.Infrastructure.Expenses;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Xunit;

namespace ERFWeb.Tests.Reporting;

public class PdfFormCodeReaderTests
{
    /// <summary>A one-page PDF laid out like the R94 header: label and code in separate cells.</summary>
    private static byte[] Sheet(string code)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(doc => doc.Page(page => page.Content().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });
            table.Cell().Text("Document Code:");
            table.Cell().Text(code);
            table.Cell().Text("Page No. 1");
            table.Cell().ColumnSpan(3).Text("ATTENDANCE SHEET");
        }))).GeneratePdf();
    }

    [Fact]
    public void FindsTheCodeInThePdfText() => Assert.True(PdfFormCodeReader.Contains(Sheet("R94"), "R94"));

    [Fact]
    public void OtherFormCode_IsNotFound() => Assert.False(PdfFormCodeReader.Contains(Sheet("R47"), "R94"));

    [Fact]
    public void NotAPdf_IsNotFound() => Assert.False(PdfFormCodeReader.Contains([0xFF, 0xD8, 0xFF, 0xE0], "R94"));
}
