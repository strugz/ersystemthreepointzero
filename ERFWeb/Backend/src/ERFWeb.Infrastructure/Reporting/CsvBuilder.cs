using System.Globalization;
using System.Text;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Minimal RFC 4180 CSV writer for exports. Output is UTF-8 with a byte-order mark and CRLF line
/// ends so Excel opens it with the right encoding. Text cells that a spreadsheet would read as a
/// formula (=, +, -, @, tab, CR) are prefixed with an apostrophe: claimants type particulars and
/// purposes freely, and an export must never run one of them as a formula (CSV injection).
/// </summary>
internal sealed class CsvBuilder
{
    private readonly StringBuilder _sb = new();

    public CsvBuilder Row(params object?[] cells)
    {
        for (var i = 0; i < cells.Length; i++)
        {
            if (i > 0)
            {
                _sb.Append(',');
            }

            _sb.Append(Escape(Format(cells[i])));
        }

        _sb.Append("\r\n");
        return this;
    }

    public byte[] ToBytes() => Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(_sb.ToString())).ToArray();

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        // Plain invariant amounts so the column stays numeric in Excel whatever the locale.
        decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
        int n => n.ToString(CultureInfo.InvariantCulture),
        long n => n.ToString(CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool b => b ? "Yes" : "No",
        string s => Neutralize(s),
        _ => Neutralize(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty),
    };

    private static string Neutralize(string text) =>
        text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + text : text;

    private static string Escape(string text) =>
        text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"" : text;
}
