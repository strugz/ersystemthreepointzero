using System.Reflection;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>Embedded binary assets used by the R47 renderer (company logo).</summary>
public static class R47Assets
{
    private const string LogoResource = "ERFWeb.Infrastructure.Reporting.Assets.mdmpi-logo.png";

    private static readonly Lazy<byte[]> LogoBytes = new(() => Load(LogoResource));

    public static byte[] Logo => LogoBytes.Value;

    private static byte[] Load(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' was not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
