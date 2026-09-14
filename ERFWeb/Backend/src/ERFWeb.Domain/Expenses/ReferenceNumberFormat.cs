namespace ERFWeb.Domain.Expenses;

/// <summary>
/// Formats the ERF reference number as a yearly series: <c>ERF-2026-000142</c>.
/// The series resets each year; the sequence is zero-padded to a fixed width so
/// numbers sort correctly as text. Pure formatting; the counter lives in persistence.
/// </summary>
public static class ReferenceNumberFormat
{
    public const int MinDigits = 3;
    public const int MaxDigits = 9;

    /// <summary>Key of the counter row a report filed in <paramref name="year"/> draws from.</summary>
    public static string SeriesKey(string prefix, int year) => $"{Normalize(prefix)}-{year}";

    public static string Format(string prefix, int year, int sequence, int digits)
    {
        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence numbers start at 1.");
        }

        if (digits is < MinDigits or > MaxDigits)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), $"Digits must be between {MinDigits} and {MaxDigits}.");
        }

        var padded = sequence.ToString().PadLeft(digits, '0');
        return $"{Normalize(prefix)}-{year}-{padded}";
    }

    private static string Normalize(string prefix)
    {
        var trimmed = (prefix ?? string.Empty).Trim().ToUpperInvariant();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Prefix is required.", nameof(prefix));
        }

        return trimmed;
    }
}
