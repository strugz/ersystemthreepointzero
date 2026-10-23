using System.Text.RegularExpressions;

namespace ERFWeb.Domain.Administration;

/// <summary>
/// The company email domains offered on the account form. The first domain is the default; the
/// list only drives the picker, so an account may still carry an address at any other domain.
/// </summary>
public static partial class EmailDomainRules
{
    public const string DefaultDomain = "marsmandrysdale.com";
    public const int MaxDomains = 20;
    public const int MaxDomainLength = 100;

    /// <summary>
    /// Trims, drops a leading '@', lower-cases and de-duplicates the domains, keeping their order.
    /// Returns the clean list and the first problem found, if any.
    /// </summary>
    public static (IReadOnlyList<string> Domains, string? Error) Normalize(IEnumerable<string?>? domains)
    {
        var clean = new List<string>();
        foreach (var raw in domains ?? [])
        {
            var domain = (raw ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();
            if (domain.Length == 0 || clean.Contains(domain))
            {
                continue;
            }

            if (domain.Length > MaxDomainLength || !DomainPattern().IsMatch(domain))
            {
                return (clean, $"\"{domain}\" is not a valid email domain, e.g. {DefaultDomain}.");
            }

            clean.Add(domain);
        }

        if (clean.Count == 0)
        {
            return (clean, "Add at least one email domain.");
        }

        return clean.Count > MaxDomains
            ? (clean, $"Keep at most {MaxDomains} email domains.")
            : (clean, null);
    }

    /// <summary>Reads the stored comma-separated list; an empty or unreadable value falls back to the default.</summary>
    public static IReadOnlyList<string> Parse(string? stored)
    {
        var (domains, error) = Normalize((stored ?? string.Empty).Split(','));
        return error is null ? domains : [DefaultDomain];
    }

    public static string Format(IReadOnlyList<string> domains) => string.Join(",", domains);

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)+$")]
    private static partial Regex DomainPattern();
}
