using System.Text.Json;
using System.Text.Json.Serialization;
using ERFWeb.Domain.Expenses;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>
/// The Finance rules as stored in <c>app_settings</c> (<c>Business.FinanceRules</c>): camelCase JSON of
/// <see cref="FinanceRuleSet"/>, enums by name. A property missing from the stored JSON keeps its default.
/// </summary>
internal static class FinanceRulesJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(FinanceRuleSet rules) => JsonSerializer.Serialize(rules, Options);

    /// <summary>The stored rules, or the defaults when none are stored or they cannot be read or no longer pass validation.</summary>
    public static FinanceRuleSet Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return FinanceRuleSet.Default;
        }

        try
        {
            var rules = JsonSerializer.Deserialize<FinanceRuleSet>(json, Options);
            return rules is not null && FinanceRuleSetRules.Validate(rules).Count == 0 ? rules : FinanceRuleSet.Default;
        }
        catch (JsonException)
        {
            return FinanceRuleSet.Default;
        }
    }
}
