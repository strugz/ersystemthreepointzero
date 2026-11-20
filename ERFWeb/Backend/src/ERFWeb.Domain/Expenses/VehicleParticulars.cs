namespace ERFWeb.Domain.Expenses;

/// <summary>
/// Gas and Vehicle emergency purchase lines are for the issued vehicle, so their Particulars read
/// "reason - vehicle model - plate no." (e.g. "Client visit - Toyota Vios - ABC 1234"). The reason may itself contain
/// " - "; the last two parts are the model and the plate number.
/// </summary>
public static class VehicleParticulars
{
    public const string Separator = " - ";
    public const int MaxModelLength = 60;
    public const int MaxPlateLength = 20;

    public static bool Applies(ExpenseCategory category) =>
        category is ExpenseCategory.Gas or ExpenseCategory.VehicleEmergency;

    /// <summary>Why the Particulars do not fit the category, or null when they do (or the category has no format).</summary>
    public static string? Problem(ExpenseCategory category, string? particulars)
    {
        if (!Applies(category) || string.IsNullOrWhiteSpace(particulars))
        {
            return null;
        }

        var parts = particulars.Split(Separator);
        return parts.Length >= 3 && parts.All(p => p.Trim().Length > 0)
            ? null
            : "Particulars must read: reason - vehicle model - plate no., e.g. Client visit - Toyota Vios - ABC 1234.";
    }

    /// <summary>The reason part of the Particulars (everything before the model and plate), trimmed; empty when there is none.</summary>
    public static string Reason(string? particulars)
    {
        var parts = (particulars ?? string.Empty).Split(Separator);
        return (parts.Length < 3 ? parts[0] : string.Join(Separator, parts[..^2])).Trim();
    }

    /// <summary>A plate number as saved: trimmed, upper-case, single spaces ("abc  1234" → "ABC 1234").</summary>
    public static string? NormalizePlate(string? plate) =>
        string.IsNullOrWhiteSpace(plate) ? null : string.Join(' ', plate.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Errors for an employee's saved vehicle, keyed <c>model</c> / <c>plateNo</c>: both or neither (neither clears
    /// it), within length, and without " - " (it would break the Particulars). Empty when it can be saved.
    /// </summary>
    public static IReadOnlyDictionary<string, string> VehicleProblems(string? model, string? plate)
    {
        var errors = new Dictionary<string, string>();
        var m = model?.Trim() ?? string.Empty;
        var p = NormalizePlate(plate) ?? string.Empty;
        if (m.Length == 0 && p.Length == 0)
        {
            return errors;
        }

        if (m.Length == 0)
        {
            errors["model"] = "Enter the vehicle model.";
        }
        else if (m.Length > MaxModelLength)
        {
            errors["model"] = $"The vehicle model must be {MaxModelLength} characters or fewer.";
        }
        else if (m.Contains(Separator, StringComparison.Ordinal))
        {
            errors["model"] = "The vehicle model cannot contain \" - \".";
        }

        if (p.Length == 0)
        {
            errors["plateNo"] = "Enter the plate number.";
        }
        else if (p.Length > MaxPlateLength)
        {
            errors["plateNo"] = $"The plate number must be {MaxPlateLength} characters or fewer.";
        }
        else if (p.Contains(Separator, StringComparison.Ordinal))
        {
            errors["plateNo"] = "The plate number cannot contain \" - \".";
        }

        return errors;
    }
}
