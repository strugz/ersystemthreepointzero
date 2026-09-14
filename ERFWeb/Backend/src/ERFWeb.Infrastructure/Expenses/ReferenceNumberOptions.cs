namespace ERFWeb.Infrastructure.Expenses;

/// <summary>ERF reference number series settings. Bound from the <c>ReferenceNumber</c> section of appsettings.json.</summary>
public sealed class ReferenceNumberOptions
{
    public const string SectionName = "ReferenceNumber";

    /// <summary>Series prefix, e.g. <c>ERF</c> → <c>ERF-2026-000142</c>.</summary>
    public string Prefix { get; set; } = "ERF";

    /// <summary>Zero-padded width of the sequence part.</summary>
    public int Digits { get; set; } = 6;

    /// <summary>Offset applied to the UTC filing time to decide which year the number belongs to (Philippines = +8).</summary>
    public int UtcOffsetHours { get; set; } = 8;
}
