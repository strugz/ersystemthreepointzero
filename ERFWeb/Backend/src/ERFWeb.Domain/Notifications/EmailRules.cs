using System.Net.Mail;

namespace ERFWeb.Domain.Notifications;

/// <summary>
/// Rules for a report email. As with SMS, the recipients are always the addresses on file
/// (<c>users.email</c>); the caller never supplies an address.
/// </summary>
public static class EmailRules
{
    public const int MaxIdempotencyKeyLength = SmsRules.MaxIdempotencyKeyLength;

    /// <summary>
    /// The usable address in <c>users.email</c>, trimmed, or null when the field is empty or not a
    /// single plain mailbox (a display name, a list, or a malformed value is skipped, not guessed at).
    /// </summary>
    public static string? RecipientAddress(string? email)
    {
        var value = email?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 254)
        {
            return null;
        }

        return MailAddress.TryCreate(value, out var address) && address.Address == value && string.IsNullOrEmpty(address.DisplayName)
            ? value
            : null;
    }

    /// <summary>The addresses in order with case-insensitive duplicates removed (a shared mailbox gets one copy).</summary>
    public static IReadOnlyList<string> DistinctAddresses(IEnumerable<string?> emails)
    {
        var result = new List<string>();
        foreach (var email in emails)
        {
            if (RecipientAddress(email) is { } address && !result.Contains(address, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(address);
            }
        }

        return result;
    }

    /// <summary>Null when the key is absent or usable; otherwise why not. Same alphabet as SMS keys.</summary>
    public static string? IdempotencyKeyError(string? key) => SmsRules.IdempotencyKeyError(key);
}
