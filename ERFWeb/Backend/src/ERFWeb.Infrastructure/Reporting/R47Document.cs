using ERFWeb.Domain.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Draws the R47 laid out by <see cref="R47Composer"/> with QuestPDF (R47 format engine <c>QuestPdf</c>,
/// the default). Each shape is placed absolutely in its own layer, so this prints the same form as the
/// PDFsharp <see cref="R47PdfSharpDocument"/> rather than letting QuestPDF's flow layout size the boxes.
/// </summary>
internal sealed class R47Document(R47Drawing drawing, R47ReportOptions opt) : IDocument
{
    // A composed text is one line that never wraps. QuestPDF wraps text wider than its box and refuses
    // text taller than it, so each text box is widened away from its anchor and given room for the line;
    // the anchor (left, centre or right; top, middle or bottom) stays where the composer put it.
    private const double Slack = 400;
    private const double LineRoom = 1.5;

    public DocumentMetadata GetMetadata() => new()
    {
        Title = drawing.Title,
        Author = drawing.Author,
        CreationDate = drawing.CreatedAt,
    };

    public void Compose(IDocumentContainer container)
    {
        foreach (var shapes in drawing.Pages)
        {
            container.Page(page =>
            {
                page.Size(R47Design.PageWidth, R47Design.PageHeight);
                page.Margin(0);
                page.Content().Layers(layers =>
                {
                    layers.PrimaryLayer();
                    foreach (var shape in shapes)
                    {
                        Draw(layers, shape);
                    }
                });
            });
        }
    }

    private void Draw(LayersDescriptor layers, R47Shape shape)
    {
        switch (shape)
        {
            case R47RectShape r:
                Rule(layers, r.Box.X, r.Box.Y, r.Box.Right, r.Box.Y, r.Thickness);
                Rule(layers, r.Box.X, r.Box.Bottom, r.Box.Right, r.Box.Bottom, r.Thickness);
                Rule(layers, r.Box.X, r.Box.Y, r.Box.X, r.Box.Bottom, r.Thickness);
                Rule(layers, r.Box.Right, r.Box.Y, r.Box.Right, r.Box.Bottom, r.Thickness);
                break;

            case R47LineShape l:
                Rule(layers, l.X1, l.Y1, l.X2, l.Y2, l.Thickness);
                break;

            case R47TextShape t:
                Aligned(layers, TextBox(t.Box, t.Style.Size, t.Horizontal, t.Vertical), t.Horizontal, t.Vertical)
                    .ScaleToFit().Text(t.Text).Style(Style(t.Style));
                break;

            case R47RunsShape runs:
                var size = runs.Runs.Count == 0 ? 0 : runs.Runs.Max(r => r.Style.Size);
                Aligned(layers, TextBox(runs.Box, size, R47Align.Start, runs.Vertical), R47Align.Start, runs.Vertical)
                    .ScaleToFit().Text(text =>
                    {
                        foreach (var run in runs.Runs)
                        {
                            text.Span(run.Text).Style(Style(run.Style));
                        }
                    });
                break;

            case R47ImageShape i:
                Aligned(layers, i.Box, i.Horizontal, i.Vertical).Image(i.Image).FitArea();
                break;
        }
    }

    /// <summary>A horizontal or vertical rule centred on its line, as PDFsharp strokes it.</summary>
    private static void Rule(LayersDescriptor layers, double x1, double y1, double x2, double y2, double thickness)
    {
        var half = thickness / 2;
        var box = x1 == x2
            ? new R47Box(x1 - half, Math.Min(y1, y2) - half, thickness, Math.Abs(y2 - y1) + thickness)
            : new R47Box(Math.Min(x1, x2) - half, y1 - half, Math.Abs(x2 - x1) + thickness, thickness);
        Place(layers, box).Background(Colors.Black);
    }

    private static IContainer Place(LayersDescriptor layers, R47Box box) =>
        layers.Layer().Unconstrained().TranslateX((float)box.X).TranslateY((float)box.Y).Width((float)box.Width).Height((float)box.Height);

    private static IContainer Aligned(LayersDescriptor layers, R47Box box, R47Align horizontal, R47Align vertical)
    {
        var c = Place(layers, box);
        c = horizontal switch
        {
            R47Align.Center => c.AlignCenter(),
            R47Align.End => c.AlignRight(),
            _ => c.AlignLeft(),
        };
        return vertical switch
        {
            R47Align.Center => c.AlignMiddle(),
            R47Align.End => c.AlignBottom(),
            _ => c.AlignTop(),
        };
    }

    private static R47Box TextBox(R47Box box, double size, R47Align horizontal, R47Align vertical)
    {
        var (x, width) = horizontal switch
        {
            R47Align.Center => (box.X - Slack, box.Width + 2 * Slack),
            R47Align.End => (box.X - Slack, box.Width + Slack),
            _ => (box.X, box.Width + Slack),
        };

        var room = Math.Max(box.Height, size * LineRoom);
        var y = vertical switch
        {
            R47Align.Center => box.Y - (room - box.Height) / 2,
            R47Align.End => box.Bottom - room,
            _ => box.Y,
        };

        return new R47Box(x, y, width, room);
    }

    private TextStyle Style(R47TextStyle style)
    {
        var family = style.Font switch
        {
            R47FontRole.Header => opt.HeaderFont,
            R47FontRole.Grid => opt.GridFont,
            _ => opt.BodyFont,
        };

        var text = TextStyle.Default.FontFamily(family).FontSize((float)style.Size).FontColor(Color.FromHex(style.Color)).LineHeight(1);
        if (style.Bold)
        {
            text = text.Bold();
        }

        return style.Italic ? text.Italic() : text;
    }
}
