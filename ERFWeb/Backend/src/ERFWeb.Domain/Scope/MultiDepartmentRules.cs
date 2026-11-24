namespace ERFWeb.Domain.Scope;

/// <summary>Who signs a report charged to another department when the employee also has a by-name signatory.</summary>
public enum MultiDepartmentByName
{
    /// <summary>The charged department's signatories; the by-name assignment counts only for main-department reports.</summary>
    DepartmentWins,

    /// <summary>The by-name signatory signs every report of the employee, whatever department it is charged to.</summary>
    ByNameWins,
}

/// <summary>Which department's rate profiles apply to a report charged to another department.</summary>
public enum MultiDepartmentRates
{
    MainDepartment,
    ReportDepartment,
}

/// <summary>The Rules page settings for reports charged to another department.</summary>
public sealed record MultiDepartmentSettings(MultiDepartmentByName ByName, MultiDepartmentRates Rates)
{
    public static readonly MultiDepartmentSettings Default = new(MultiDepartmentByName.DepartmentWins, MultiDepartmentRates.MainDepartment);
}

/// <summary>
/// An employee whose account allows it (<c>users.multi_department_enabled</c>) may charge a report to one of their
/// other departments instead of the main one; that department then picks the approvers. A report's choice is
/// null for the main department, so a report of an employee who never uses this behaves as before. Reports
/// already filed keep their snapshotted route; only filing checks the choice again.
/// </summary>
public static class MultiDepartmentRules
{
    /// <summary>The departments the employee may charge a report to: the main one first, then the others while allowed.</summary>
    public static IReadOnlyList<long> Allowed(long? mainDepartmentId, bool enabled, IEnumerable<long> otherDepartmentIds)
    {
        ArgumentNullException.ThrowIfNull(otherDepartmentIds);
        var allowed = new List<long>();
        if (mainDepartmentId is { } main)
        {
            allowed.Add(main);
        }

        if (enabled)
        {
            allowed.AddRange(otherDepartmentIds.Where(id => !allowed.Contains(id)).Distinct());
        }

        return allowed;
    }

    /// <summary>The other departments as stored: distinct, sorted, never the main department.</summary>
    public static List<long> NormalizeOthers(IEnumerable<long>? otherDepartmentIds, long? mainDepartmentId) =>
        (otherDepartmentIds ?? []).Where(id => id != mainDepartmentId).Distinct().Order().ToList();

    /// <summary>A charged department other than the main one; null and the main department mean "main".</summary>
    public static bool IsOtherDepartment(long? chargedDepartmentId, long? mainDepartmentId) =>
        chargedDepartmentId is { } charged && charged != mainDepartmentId;

    /// <summary>What the report is stored with: null for the main department, else the chosen one.</summary>
    public static long? StoredChoice(long? chargedDepartmentId, long? mainDepartmentId) =>
        IsOtherDepartment(chargedDepartmentId, mainDepartmentId) ? chargedDepartmentId : null;

    /// <summary>The department the report routes and counts under.</summary>
    public static long? Effective(long? chargedDepartmentId, long? mainDepartmentId) => chargedDepartmentId ?? mainDepartmentId;

    /// <summary>Whether the employee may charge (or still file) a report to <paramref name="chargedDepartmentId"/>.</summary>
    public static bool IsAllowed(long? chargedDepartmentId, long? mainDepartmentId, bool enabled, IEnumerable<long> otherDepartmentIds) =>
        !IsOtherDepartment(chargedDepartmentId, mainDepartmentId)
        || Allowed(mainDepartmentId, enabled, otherDepartmentIds).Contains(chargedDepartmentId!.Value);

    /// <summary>Why a report charged to another department cannot be filed now, or null when it can.</summary>
    public static string? FilingProblem(
        long? chargedDepartmentId,
        long? mainDepartmentId,
        bool enabled,
        IEnumerable<long> otherDepartmentIds,
        string? chargedDepartmentName,
        string? mainDepartmentName)
    {
        if (IsAllowed(chargedDepartmentId, mainDepartmentId, enabled, otherDepartmentIds))
        {
            return null;
        }

        var main = mainDepartmentName ?? "your main department";
        return enabled
            ? $"This report is charged to {chargedDepartmentName ?? "a department"}, which is no longer one of your departments. Change the department in Report details."
            : $"Filing for other departments is off for your account. Change the department to {main} in Report details.";
    }

    /// <summary>Whether a by-name scope assignment still applies to the report.</summary>
    public static bool ByNameApplies(MultiDepartmentSettings settings, long? chargedDepartmentId, long? mainDepartmentId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.ByName == MultiDepartmentByName.ByNameWins || !IsOtherDepartment(chargedDepartmentId, mainDepartmentId);
    }

    /// <summary>The department whose rate profiles apply to the report's lines.</summary>
    public static long? RateDepartment(MultiDepartmentSettings settings, long? chargedDepartmentId, long? mainDepartmentId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Rates == MultiDepartmentRates.ReportDepartment ? Effective(chargedDepartmentId, mainDepartmentId) : mainDepartmentId;
    }
}
