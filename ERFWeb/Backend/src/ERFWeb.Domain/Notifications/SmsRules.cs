using System.Text.RegularExpressions;

namespace ERFWeb.Domain.Notifications;

/// <summary>
/// Rules for a notification SMS sent through the Lasius gateway. The recipients are always the
/// numbers on file for the user (their "text group"); the caller never supplies a number.
/// </summary>
public static partial class SmsRules
{
    /// <summary>Two concatenated SMS segments. Notifications are meant to be short.</summary>
    public const int MaxMessageLength = 320;

    public const int MaxIdempotencyKeyLength = 100;

    /// <summary>
    /// The user's text group: every Philippine mobile number in <c>users.mobile_no</c>. The field may
    /// hold more than one number separated by a comma, semicolon, slash, or line break. Spaces,
    /// dashes, dots, and brackets are dropped; <c>639…</c> becomes <c>+639…</c>; anything that is not
    /// a mobile number (a landline, an extension) is skipped. Lasius accepts both <c>09…</c> and <c>+639…</c>.
    /// </summary>
    public static IReadOnlyList<string> RecipientNumbers(string? mobileNo)
    {
        if (string.IsNullOrWhiteSpace(mobileNo))
        {
            return [];
        }

        var numbers = new List<string>();
        foreach (var part in mobileNo.Split([',', ';', '/', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            var compact = Noise().Replace(part, string.Empty);
            if (compact.StartsWith("639", StringComparison.Ordinal))
            {
                compact = "+" + compact;
            }

            if (MobileNumber().IsMatch(compact) && !numbers.Contains(compact))
            {
                numbers.Add(compact);
            }
        }

        return numbers;
    }

    /// <summary>Null when the message can be sent; otherwise why not.</summary>
    public static string? MessageError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Enter the message to send.";
        }

        return message.Trim().Length > MaxMessageLength
            ? $"The message must be at most {MaxMessageLength} characters."
            : null;
    }

    /// <summary>Null when the key is absent or usable; otherwise why not.</summary>
    public static string? IdempotencyKeyError(string? key)
    {
        if (key is null)
        {
            return null;
        }

        if (key.Length > MaxIdempotencyKeyLength)
        {
            return $"The idempotency key must be at most {MaxIdempotencyKeyLength} characters.";
        }

        return IdempotencyKey().IsMatch(key)
            ? null
            : "The idempotency key may only contain letters, digits, '-', '_', ':' and '.'.";
    }

    [GeneratedRegex(@"[\s\-\.\(\)]")]
    private static partial Regex Noise();

    [GeneratedRegex(@"^(?:\+639|09)\d{9}$")]
    private static partial Regex MobileNumber();

    [GeneratedRegex(@"^[A-Za-z0-9\-_:\.]+$")]
    private static partial Regex IdempotencyKey();
}
