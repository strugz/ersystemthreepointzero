using System.Globalization;

namespace ERFWeb.Domain.Reporting;

/// <summary>What the R47 prints in the signature places: approved images, or status lines only.</summary>
public enum R47SignatureMode
{
    /// <summary>Approved signature images print, each with the ELECTRONIC SIGNATURE watermark.</summary>
    ESignature,

    /// <summary>No images: each signed place prints "Electronically approved" (or "filed") with the date and time.</summary>
    StatusOnly,
}

/// <summary>
/// The R47 electronic signature rule (Rules page, <c>Reports.R47SignatureMode</c>). Filing and approving
/// are the signature either way; the mode only decides whether an approved image is printed with them.
/// An image prints only after Finance approves it, and never without the watermark.
/// </summary>
public static class R47Signatures
{
    /// <summary>Printed across every signature image on the R47.</summary>
    public const string Watermark = "ELECTRONIC SIGNATURE";

    /// <summary>Whether a person's signature image prints: E-signature mode and an image Finance approved.</summary>
    public static bool PrintsImage(R47SignatureMode mode, bool hasImage, bool approved) =>
        mode == R47SignatureMode.ESignature && hasImage && approved;

    /// <summary>Beside a printed signature image: "Electronically signed in ERFWeb 10/29/2026 2:05pm · ERF-2026-000142".</summary>
    public static string SignedStamp(DateTimeOffset at, int utcOffsetHours, string? referenceNo) =>
        string.IsNullOrWhiteSpace(referenceNo)
            ? $"Electronically signed in ERFWeb {LocalTime(at, utcOffsetHours)}"
            : $"Electronically signed in ERFWeb {LocalTime(at, utcOffsetHours)} · {referenceNo}";

    /// <summary>An approved step without a printed image: "Electronically approved 10/29/2026 2:05pm".</summary>
    public static string ApprovedStamp(DateTimeOffset at, int utcOffsetHours) =>
        $"Electronically approved {LocalTime(at, utcOffsetHours)}";

    /// <summary>A filed report without a printed employee image: "Electronically filed 10/29/2026 2:05pm".</summary>
    public static string FiledStamp(DateTimeOffset at, int utcOffsetHours) =>
        $"Electronically filed {LocalTime(at, utcOffsetHours)}";

    /// <summary>Parses the stored setting; anything unknown keeps the images (the behaviour before the rule existed).</summary>
    public static R47SignatureMode ParseMode(string? value) =>
        Enum.TryParse<R47SignatureMode>(value, ignoreCase: true, out var mode) && Enum.IsDefined(mode)
            ? mode
            : R47SignatureMode.ESignature;

    private static string LocalTime(DateTimeOffset at, int utcOffsetHours)
    {
        var local = at.ToOffset(TimeSpan.FromHours(utcOffsetHours));
        var time = local.ToString("h:mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();
        return string.Create(CultureInfo.InvariantCulture, $"{local:MM/dd/yyyy} {time}");
    }
}
