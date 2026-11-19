namespace ERFWeb.Domain.Administration;

/// <summary>
/// The company's job grades. Users and Grade rate assignments store the short code (the full
/// label is longer than the 32-character column); the screens show the label.
/// </summary>
public static class JobGrades
{
    public const string Staff = "Staff";
    public const string Specialist = "Specialist";
    public const string Supervisor = "Supervisor";

    public static readonly IReadOnlyList<string> All = [Staff, Specialist, Supervisor];

    /// <summary>The stored code for <paramref name="value"/> in any letter case, or null when it is not a job grade.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : All.FirstOrDefault(code => string.Equals(code, value.Trim(), StringComparison.OrdinalIgnoreCase));

    public const string Problem = "Pick a job grade: Staff, Specialists/Engineers or Team Leaders/Supervisors/Assistant Managers.";
}
