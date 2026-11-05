using System.Text.Json;
using ERFWeb.Domain.Reporting;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// The R47 designer's layout as stored in <c>app_settings</c> (<c>Reports.R47Design</c>): camelCase JSON of
/// <see cref="R47Design"/>. A property missing from a stored layout keeps its standard value.
/// </summary>
internal static class R47DesignJson
{
    public const string SettingKey = "Reports.R47Design";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(R47Design design) => JsonSerializer.Serialize(design, Options);

    /// <summary>The stored layout, or null when none is stored or it cannot be read or no longer passes the rules (the standard form prints).</summary>
    public static R47Design? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var design = JsonSerializer.Deserialize<R47Design>(json, Options);
            return design is not null && R47DesignRules.Validate(design).Count == 0 ? design : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
