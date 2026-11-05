using System.Collections.Concurrent;
using PdfSharp.Fonts;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Font resolver for the PDFsharp R47 engine. The PDFsharp package is its cross-platform (Core) build,
/// which has no fonts of its own, so this maps the R47 font families (the <c>R47Report</c> font settings)
/// to the TrueType files in the Windows fonts folder. A family without a bold or italic file is
/// simulated by PDFsharp. Registered once, before the first PDFsharp font is created.
/// </summary>
internal sealed class PdfSharpFonts : IFontResolver
{
    // Family → regular, bold, italic, bold-italic file names (without .ttf); null = simulate from regular/bold.
    private static readonly Dictionary<string, string?[]> Families = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Calibri"] = ["calibri", "calibrib", "calibrii", "calibriz"],
        ["Tahoma"] = ["tahoma", "tahomabd", null, null],
        ["Times New Roman"] = ["times", "timesbd", "timesi", "timesbi"],
        ["Arial"] = ["arial", "arialbd", "ariali", "arialbi"],
        ["Segoe UI"] = ["segoeui", "segoeuib", "segoeuii", "segoeuiz"],
        ["Verdana"] = ["verdana", "verdanab", "verdanai", "verdanaz"],
    };

    private static readonly Lock SetupLock = new();
    private static bool _registered;

    private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers this resolver with PDFsharp once per process.</summary>
    public static void EnsureRegistered()
    {
        lock (SetupLock)
        {
            if (_registered)
            {
                return;
            }

            GlobalFontSettings.FontResolver = new PdfSharpFonts();
            _registered = true;
        }
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (!Families.TryGetValue(familyName, out var files))
        {
            throw new InvalidOperationException(
                $"The PDFsharp R47 engine has no font file mapping for '{familyName}'. " +
                $"Use one of: {string.Join(", ", Families.Keys)}, or set R47Report:Engine to QuestPdf.");
        }

        var index = (isBold ? 1 : 0) + (isItalic ? 2 : 0);
        if (files[index] is { } exact)
        {
            return new FontResolverInfo(exact);
        }

        // No italic file (Tahoma): use the upright face of the same weight and slant it.
        var upright = files[isBold ? 1 : 0] ?? files[0]!;
        var simulateBold = isBold && files[1] is null;
        return new FontResolverInfo(upright, simulateBold, mustSimulateItalic: isItalic);
    }

    public byte[]? GetFont(string faceName) => _files.GetOrAdd(faceName, name =>
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), name + ".ttf");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Font file '{name}.ttf' is not installed in the Windows fonts folder; the PDFsharp R47 engine cannot draw it.");
        }

        return File.ReadAllBytes(path);
    });
}
