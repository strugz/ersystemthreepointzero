namespace ERFWeb.Domain.Reporting;

/// <summary>
/// Estimates how many printed lines a grid text occupies, so pagination can reproduce the
/// legacy report's page breaks (Crystal fills the detail area by height, not by row count).
/// Uses Times Roman advance widths in thousandths of an em with greedy word wrapping;
/// capacities were calibrated against the reference printouts (which particulars wrapped
/// to 2 or 3 lines, and which did not).
/// </summary>
public static class R47TextMetrics
{
    /// <summary>Usable width of the PARTICULARS column, in thousandths of an em of the body font.</summary>
    public const int ParticularsCapacity = 18_400;

    /// <summary>Usable width of the Invoice/OR column, in thousandths of an em.</summary>
    public const int InvoiceCapacity = 7_300;

    /// <summary>Group headers print bold, which is ~6% wider than regular Times.</summary>
    public const double BoldFactor = 1.06;

    private const int DefaultWidth = 500;

    private static readonly Dictionary<char, int> Widths = new()
    {
        [' '] = 250,
        ['-'] = 333,
        ['('] = 333,
        [')'] = 333,
        ['/'] = 278,
        ['.'] = 250,
        [','] = 250,
        [':'] = 278,
        [';'] = 278,
        ['&'] = 778,
        ['\''] = 333,
        ['"'] = 408,
        ['+'] = 564,
        ['#'] = 500,
        ['*'] = 500,
        ['_'] = 500,
        ['A'] = 722,
        ['B'] = 667,
        ['C'] = 667,
        ['D'] = 722,
        ['E'] = 611,
        ['F'] = 556,
        ['G'] = 722,
        ['H'] = 722,
        ['I'] = 333,
        ['J'] = 389,
        ['K'] = 722,
        ['L'] = 611,
        ['M'] = 889,
        ['N'] = 722,
        ['O'] = 722,
        ['P'] = 556,
        ['Q'] = 722,
        ['R'] = 667,
        ['S'] = 556,
        ['T'] = 611,
        ['U'] = 722,
        ['V'] = 722,
        ['W'] = 944,
        ['X'] = 722,
        ['Y'] = 722,
        ['Z'] = 611,
        ['a'] = 444,
        ['b'] = 500,
        ['c'] = 444,
        ['d'] = 500,
        ['e'] = 444,
        ['f'] = 333,
        ['g'] = 500,
        ['h'] = 500,
        ['i'] = 278,
        ['j'] = 278,
        ['k'] = 500,
        ['l'] = 278,
        ['m'] = 778,
        ['n'] = 500,
        ['o'] = 500,
        ['p'] = 500,
        ['q'] = 500,
        ['r'] = 333,
        ['s'] = 389,
        ['t'] = 278,
        ['u'] = 500,
        ['v'] = 500,
        ['w'] = 722,
        ['x'] = 500,
        ['y'] = 500,
        ['z'] = 444,
    };

    /// <summary>Advance width of <paramref name="text"/> in thousandths of an em (digits and unknown glyphs = 500).</summary>
    public static int Width(string text, bool bold = false)
    {
        var total = 0;
        foreach (var ch in text)
        {
            total += Widths.TryGetValue(ch, out var w) ? w : DefaultWidth;
        }

        return bold ? (int)Math.Round(total * BoldFactor) : total;
    }

    /// <summary>
    /// Lines needed to print <paramref name="text"/> in a column of <paramref name="capacity"/>:
    /// greedy word wrap on spaces; a single word wider than the column breaks mid-word.
    /// Empty text still occupies one line.
    /// </summary>
    public static int LineCount(string? text, int capacity, bool bold = false)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return 1;
        }

        var space = Width(" ", bold);
        var lines = 1;
        var current = 0;

        foreach (var word in text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var w = Width(word, bold);
            if (w > capacity)
            {
                // Over-long token (invoice numbers): finish the current line, then break by width.
                if (current > 0)
                {
                    lines++;
                }

                var pieces = (int)Math.Ceiling(w / (double)capacity);
                lines += pieces - 1;
                current = w - (pieces - 1) * capacity;
                continue;
            }

            var needed = current == 0 ? w : current + space + w;
            if (needed > capacity)
            {
                lines++;
                current = w;
            }
            else
            {
                current = needed;
            }
        }

        return lines;
    }
}
