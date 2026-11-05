namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// The R47 as positioned shapes, in points from the top-left of a US Letter landscape page. Built once by
/// <see cref="R47Composer"/> from the R47 design, then drawn by either engine (<see cref="R47Document"/>
/// with QuestPDF, <see cref="R47PdfSharpDocument"/> with PDFsharp), so both print the same form.
/// Every text is a single line; the composer wraps grid text itself.
/// </summary>
internal sealed record R47Drawing(string Title, string Author, DateTimeOffset CreatedAt, IReadOnlyList<IReadOnlyList<R47Shape>> Pages);

internal readonly record struct R47Box(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
}

internal enum R47Align
{
    Start,
    Center,
    End,
}

/// <summary>Which configured font family a text uses (<c>R47Report</c> HeaderFont, BodyFont, GridFont).</summary>
internal enum R47FontRole
{
    Header,
    Body,
    Grid,
}

internal sealed record R47TextStyle(R47FontRole Font, double Size, string Color, bool Bold = false, bool Italic = false);

internal abstract record R47Shape;

/// <summary>An outlined rectangle.</summary>
internal sealed record R47RectShape(R47Box Box, double Thickness) : R47Shape;

/// <summary>A horizontal or vertical rule.</summary>
internal sealed record R47LineShape(double X1, double Y1, double X2, double Y2, double Thickness) : R47Shape;

/// <summary>One line of text aligned inside <see cref="Box"/>; it never wraps.</summary>
internal sealed record R47TextShape(R47Box Box, string Text, R47TextStyle Style, R47Align Horizontal, R47Align Vertical) : R47Shape;

internal sealed record R47Run(string Text, R47TextStyle Style);

/// <summary>Texts of different styles on one line, starting at the left of <see cref="Box"/>.</summary>
internal sealed record R47RunsShape(R47Box Box, IReadOnlyList<R47Run> Runs, R47Align Vertical) : R47Shape;

/// <summary>An image scaled to fit <see cref="Box"/>, keeping its aspect ratio, aligned inside it.</summary>
internal sealed record R47ImageShape(R47Box Box, byte[] Image, R47Align Horizontal, R47Align Vertical) : R47Shape;
