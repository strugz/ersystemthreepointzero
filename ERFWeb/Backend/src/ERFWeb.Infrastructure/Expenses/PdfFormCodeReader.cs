using ERFWeb.Domain.Expenses;
using UglyToad.PdfPig;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Looks for a form code (e.g. R94) in the text of an uploaded PDF with PdfPig. Photos and scanned PDFs have no
/// text, so they never match; the employee confirms those instead (<see cref="CategoryDocumentRules"/>).
/// </summary>
public static class PdfFormCodeReader
{
    /// <summary>The form code sits in the header, so a few pages are enough.</summary>
    private const int MaxPages = 5;

    public static bool Contains(byte[] pdf, string code)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        try
        {
            using var document = PdfDocument.Open(pdf);
            // Words joined with spaces: page.Text runs table cells together ("R94Page No.").
            return document.GetPages().Take(MaxPages)
                .Any(page => CategoryDocumentRules.ContainsFormCode(string.Join(' ', page.GetWords().Select(w => w.Text)), code));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A damaged or password-protected PDF cannot be read: treated as "code not found", so the employee confirms it.
            return false;
        }
    }
}
