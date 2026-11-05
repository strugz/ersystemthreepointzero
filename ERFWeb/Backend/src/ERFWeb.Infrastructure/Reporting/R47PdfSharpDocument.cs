using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Draws the R47 laid out by <see cref="R47Composer"/> with PDFsharp (R47 format engine <c>PdfSharp</c>).
/// PDFsharp has no layout engine; every shape is drawn at its composed position, so this prints the
/// same form as the QuestPDF <see cref="R47Document"/>. Fonts come from <see cref="PdfSharpFonts"/>.
/// </summary>
internal sealed class R47PdfSharpDocument(R47Drawing drawing, R47ReportOptions opt)
{
    private readonly Dictionary<(string Family, double Size, XFontStyleEx Style), XFont> _fonts = [];
    private readonly Dictionary<byte[], XImage> _images = new(ReferenceEqualityComparer.Instance);

    public byte[] GeneratePdf()
    {
        PdfSharpFonts.EnsureRegistered();

        using var document = new PdfDocument();
        document.Info.Title = drawing.Title;
        document.Info.Author = drawing.Author;
        document.Info.CreationDate = drawing.CreatedAt.ToOffset(TimeSpan.FromHours(opt.UtcOffsetHours)).DateTime;

        try
        {
            foreach (var shapes in drawing.Pages)
            {
                var page = document.AddPage();
                page.Width = XUnit.FromPoint(Domain.Reporting.R47Design.PageWidth);
                page.Height = XUnit.FromPoint(Domain.Reporting.R47Design.PageHeight);

                using var gfx = XGraphics.FromPdfPage(page);
                foreach (var shape in shapes)
                {
                    Draw(gfx, shape);
                }
            }

            using var output = new MemoryStream();
            document.Save(output, false);
            return output.ToArray();
        }
        finally
        {
            foreach (var image in _images.Values)
            {
                image.Dispose();
            }
        }
    }

    private void Draw(XGraphics g, R47Shape shape)
    {
        switch (shape)
        {
            case R47RectShape r:
                g.DrawRectangle(new XPen(XColors.Black, r.Thickness), r.Box.X, r.Box.Y, r.Box.Width, r.Box.Height);
                break;

            case R47LineShape l:
                g.DrawLine(new XPen(XColors.Black, l.Thickness), l.X1, l.Y1, l.X2, l.Y2);
                break;

            case R47TextShape t:
                g.DrawString(t.Text, Font(t.Style), Brush(t.Style.Color), Rect(t.Box), Format(t.Horizontal, t.Vertical));
                break;

            case R47RunsShape runs:
                var x = runs.Box.X;
                foreach (var run in runs.Runs)
                {
                    var font = Font(run.Style);
                    g.DrawString(run.Text, font, Brush(run.Style.Color),
                        new XRect(x, runs.Box.Y, Math.Max(1, runs.Box.Right - x), runs.Box.Height), Format(R47Align.Start, runs.Vertical));
                    x += g.MeasureString(run.Text, font).Width;
                }

                break;

            case R47ImageShape i:
                DrawImageFit(g, Image(i.Image), i.Box, i.Horizontal, i.Vertical);
                break;
        }
    }

    private XImage Image(byte[] bytes)
    {
        if (!_images.TryGetValue(bytes, out var image))
        {
            image = XImage.FromStream(new MemoryStream(bytes, writable: false));
            _images[bytes] = image;
        }

        return image;
    }

    /// <summary>Draws an image scaled to fit <paramref name="area"/>, keeping its aspect ratio, aligned within it.</summary>
    private static void DrawImageFit(XGraphics g, XImage image, R47Box area, R47Align horizontal, R47Align vertical)
    {
        var scale = Math.Min(area.Width / image.PixelWidth, area.Height / image.PixelHeight);
        var w = image.PixelWidth * scale;
        var h = image.PixelHeight * scale;
        var x = horizontal switch
        {
            R47Align.Center => area.X + (area.Width - w) / 2,
            R47Align.End => area.Right - w,
            _ => area.X,
        };
        var y = vertical switch
        {
            R47Align.Center => area.Y + (area.Height - h) / 2,
            R47Align.End => area.Bottom - h,
            _ => area.Y,
        };
        g.DrawImage(image, x, y, w, h);
    }

    private static XRect Rect(R47Box box) => new(box.X, box.Y, box.Width, box.Height);

    private static XStringFormat Format(R47Align horizontal, R47Align vertical) => new()
    {
        Alignment = horizontal switch
        {
            R47Align.Center => XStringAlignment.Center,
            R47Align.End => XStringAlignment.Far,
            _ => XStringAlignment.Near,
        },
        LineAlignment = vertical switch
        {
            R47Align.Center => XLineAlignment.Center,
            R47Align.End => XLineAlignment.Far,
            _ => XLineAlignment.Near,
        },
    };

    private static XSolidBrush Brush(string hex) => new(XColor.FromArgb(
        Convert.ToInt32(hex.Substring(1, 2), 16), Convert.ToInt32(hex.Substring(3, 2), 16), Convert.ToInt32(hex.Substring(5, 2), 16)));

    private XFont Font(R47TextStyle style)
    {
        var family = style.Font switch
        {
            R47FontRole.Header => opt.HeaderFont,
            R47FontRole.Grid => opt.GridFont,
            _ => opt.BodyFont,
        };
        var fontStyle = (style.Bold, style.Italic) switch
        {
            (true, true) => XFontStyleEx.BoldItalic,
            (true, false) => XFontStyleEx.Bold,
            (false, true) => XFontStyleEx.Italic,
            _ => XFontStyleEx.Regular,
        };

        var key = (family, style.Size, fontStyle);
        if (!_fonts.TryGetValue(key, out var font))
        {
            font = new XFont(family, style.Size, fontStyle);
            _fonts[key] = font;
        }

        return font;
    }
}
