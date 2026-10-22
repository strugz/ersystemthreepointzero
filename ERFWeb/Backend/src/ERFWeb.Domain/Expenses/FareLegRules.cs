namespace ERFWeb.Domain.Expenses;

/// <summary>The parts of a fare mode the leg rules need.</summary>
public sealed record FareModeRule(long Id, bool IsActive, bool RequiresDetail);

/// <summary>One fare leg as submitted on a Transportation line.</summary>
public sealed record FareLegInput(long? FareModeId, string? FareModeDetail, decimal FareAmount);

/// <summary>
/// Rules for the fare legs of a Transportation line. Every leg saved names how it was travelled
/// (legacy dbo.tbFare had one fare type per line; ERFWeb records it per leg). Legs saved before fare
/// modes existed have none and stay valid until the line is saved again.
/// </summary>
public static class FareLegRules
{
    public const int MaxDetailLength = 60;

    /// <summary>
    /// Validates the legs against the known fare modes. An inactive mode is accepted only when the
    /// line already used it (<paramref name="modesAlreadyOnLine"/>), so deactivating a mode never
    /// blocks editing an old line. Returns field → message pairs keyed by leg index; empty when valid.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> Validate(
        IReadOnlyList<FareLegInput> legs,
        IReadOnlyDictionary<long, FareModeRule> modes,
        IReadOnlySet<long> modesAlreadyOnLine)
    {
        ArgumentNullException.ThrowIfNull(legs);
        ArgumentNullException.ThrowIfNull(modes);
        ArgumentNullException.ThrowIfNull(modesAlreadyOnLine);

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (legs.Any(l => l.FareAmount < 0))
        {
            errors["transports"] = ["Fare amounts cannot be negative."];
        }

        for (var i = 0; i < legs.Count; i++)
        {
            var leg = legs[i];
            var modeKey = $"transports[{i}].fareModeId";
            if (leg.FareModeId is not { } modeId)
            {
                errors[modeKey] = ["Choose how this leg was travelled."];
                continue;
            }

            if (!modes.TryGetValue(modeId, out var mode) || (!mode.IsActive && !modesAlreadyOnLine.Contains(modeId)))
            {
                errors[modeKey] = ["That fare mode is not available."];
                continue;
            }

            var detail = leg.FareModeDetail?.Trim();
            if (mode.RequiresDetail && string.IsNullOrEmpty(detail))
            {
                errors[$"transports[{i}].fareModeDetail"] = ["Type how this leg was travelled."];
            }
            else if (detail is { Length: > MaxDetailLength })
            {
                errors[$"transports[{i}].fareModeDetail"] = [$"Keep it to {MaxDetailLength} characters."];
            }
        }

        return errors;
    }

    /// <summary>The typed mode is kept only for modes that ask for it ("Others").</summary>
    public static string? NormalizeDetail(FareModeRule mode, string? detail)
    {
        ArgumentNullException.ThrowIfNull(mode);
        return mode.RequiresDetail && !string.IsNullOrWhiteSpace(detail) ? detail.Trim() : null;
    }
}
