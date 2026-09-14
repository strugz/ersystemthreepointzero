namespace ERFWeb.Infrastructure.Expenses;

/// <summary>Receipt attachment limits. Bound from the <c>Attachments</c> section of appsettings.json.</summary>
public sealed class AttachmentOptions
{
    public const string SectionName = "Attachments";

    /// <summary>Maximum size of one uploaded file in megabytes.</summary>
    public int MaxFileSizeMb { get; set; } = 10;

    /// <summary>Maximum number of attachments per report.</summary>
    public int MaxFilesPerReport { get; set; } = 20;

    /// <summary>Allowed MIME types (exact match, case-insensitive).</summary>
    public string[] AllowedContentTypes { get; set; } = ["image/jpeg", "image/png", "application/pdf"];

    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;
}
