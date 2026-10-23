namespace ERFWeb.Domain.Reporting;

/// <summary>
/// Estimates how many printed lines a grid text occupies, so pagination can reproduce the
/// legacy report's page breaks (Crystal fills the detail area by height, not by row count).
/// Uses Tahoma advance widths (the grid text font) in thousandths of an em with greedy word
/// wrapping. The capacities are the text widths the document prints into, so the estimate
/// and the rendered wrap agree; they reproduce the wraps on the reference printout
/// (MARER08OCT2026: 119 pt of 5.8 pt text in PARTICULARS).
/// </summary>
public static class R47TextMetrics
{
    /// <summary>Usable width of the PARTICULARS column, in thousandths of an em of the grid font.</summary>
    public const int ParticularsCapacity = 20_500;

    /// <summary>Usable width of the Invoice/OR column, in thousandths of an em.</summary>
    public const int InvoiceCapacity = 8_200;

    private const int DefaultWidth = 546;

    private static readonly Dictionary<char, int> Widths = new()
    {
        [' '] = 312,
        ['-'] = 363,
        ['('] = 383,
        [')'] = 383,
        ['/'] = 382,
        ['.'] = 303,
        [','] = 303,
        [':'] = 354,
        [';'] = 354,
        ['&'] = 674,
        ['\''] = 211,
        ['"'] = 401,
        ['+'] = 728,
        ['#'] = 728,
        ['*'] = 546,
        ['_'] = 546,
        ['A'] = 600,
        ['B'] = 589,
        ['C'] = 601,
        ['D'] = 678,
        ['E'] = 561,
        ['F'] = 521,
        ['G'] = 667,
        ['H'] = 675,
        ['I'] = 373,
        ['J'] = 417,
        ['K'] = 588,
        ['L'] = 498,
        ['M'] = 771,
        ['N'] = 667,
        ['O'] = 708,
        ['P'] = 551,
        ['Q'] = 708,
        ['R'] = 621,
        ['S'] = 557,
        ['T'] = 584,
        ['U'] = 656,
        ['V'] = 597,
        ['W'] = 902,
        ['X'] = 581,
        ['Y'] = 576,
        ['Z'] = 559,
        ['a'] = 525,
        ['b'] = 553,
        ['c'] = 461,
        ['d'] = 553,
        ['e'] = 526,
        ['f'] = 318,
        ['g'] = 553,
        ['h'] = 558,
        ['i'] = 229,
        ['j'] = 282,
        ['k'] = 498,
        ['l'] = 229,
        ['m'] = 840,
        ['n'] = 558,
        ['o'] = 543,
        ['p'] = 553,
        ['q'] = 553,
        ['r'] = 360,
        ['s'] = 446,
        ['t'] = 334,
        ['u'] = 558,
        ['v'] = 498,
        ['w'] = 742,
        ['x'] = 495,
        ['y'] = 498,
        ['z'] = 444,
    };

    /// <summary>Advance width of <paramref name="text"/> in thousandths of an em (digits and unknown glyphs = 546).</summary>
    public static int Width(string text)
    {
        var total = 0;
        foreach (var ch in text)
        {
            total += Widths.TryGetValue(ch, out var w) ? w : DefaultWidth;
        }

        return total;
    }

    /// <summary>
    /// Lines needed to print <paramref name="text"/> in a column of <paramref name="capacity"/>:
    /// greedy word wrap on spaces; a single word wider than the column breaks mid-word.
    /// Empty text still occupies one line.
    /// </summary>
    public static int LineCount(string? text, int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return 1;
        }

        var space = Width(" ");
        var lines = 1;
        var current = 0;

        foreach (var word in text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var w = Width(word);
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
