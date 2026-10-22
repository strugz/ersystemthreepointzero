namespace ERFWeb.Domain.Administration;

/// <summary>Account-level rules: the registration cooling period (spec 9.11) and credential shape.</summary>
public static class AccountRules
{
    public const int DefaultCoolingPeriodDays = 3;
    public const int MinPasswordLength = 8;
    public const int MaxUsernameLength = 64;

    /// <summary>
    /// A newly registered employee may file only after the cooling period has elapsed. A period
    /// of zero disables the rule.
    /// </summary>
    public static bool CoolingPeriodElapsed(DateTimeOffset createdAt, DateTimeOffset now, int coolingPeriodDays)
    {
        if (coolingPeriodDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(coolingPeriodDays), "The cooling period cannot be negative.");
        }

        return coolingPeriodDays == 0 || now >= createdAt.AddDays(coolingPeriodDays);
    }

    /// <summary>Usernames are trimmed, non-empty, at most 64 characters, with no whitespace inside.</summary>
    public static string? UsernameError(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return "Username is required.";
        }

        var value = username.Trim();
        if (value.Length > MaxUsernameLength)
        {
            return $"Username must be at most {MaxUsernameLength} characters.";
        }

        return value.Any(char.IsWhiteSpace) ? "Username cannot contain spaces." : null;
    }

    /// <summary>Initial and reset passwords must have a minimum length; complexity is a deployment policy.</summary>
    public static string? PasswordError(string? password) =>
        string.IsNullOrEmpty(password) || password.Length < MinPasswordLength
            ? $"Password must be at least {MinPasswordLength} characters."
            : null;

    /// <summary>
    /// A password the user chooses themselves: the minimum length, not the username, and not the password it
    /// replaces. Null when acceptable.
    /// </summary>
    public static string? NewPasswordError(string? newPassword, string username, string currentPassword)
    {
        if (PasswordError(newPassword) is { } lengthError)
        {
            return lengthError;
        }

        if (string.Equals(newPassword, username, StringComparison.OrdinalIgnoreCase))
        {
            return "Password cannot be the same as your username.";
        }

        return newPassword == currentPassword ? "Choose a password different from your current one." : null;
    }
}
