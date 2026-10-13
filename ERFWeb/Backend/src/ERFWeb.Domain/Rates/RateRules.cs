namespace ERFWeb.Domain.Rates;

/// <summary>Meal sub-categories that carry their own rate line.</summary>
public static class MealSubCategories
{
    public const string Breakfast = "Breakfast";
    public const string Lunch = "Lunch";
    public const string Dinner = "Dinner";
    public const string OtMeal = "OTMeal";

    public static readonly IReadOnlyList<string> All = [Breakfast, Lunch, Dinner, OtMeal];
}

/// <summary>One rate profile assignment as the resolver sees it.</summary>
public sealed record RateAssignmentCandidate(
    long Id,
    long ProfileId,
    RateTargetType TargetType,
    long? UserId,
    string? JobGrade,
    long? DepartmentId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo)
{
    public bool InForceOn(DateOnly date) => EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);
}

/// <summary>One versioned rate line as the resolver sees it.</summary>
public sealed record RateLineCandidate(
    long Id,
    long ProfileId,
    ExpenseCategory Category,
    string? SubCategory,
    RateType RateType,
    decimal Amount,
    bool RequiresReceipt,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo)
{
    public bool InForceOn(DateOnly date) => EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);
}

/// <summary>The employee whose rates are resolved.</summary>
public sealed record RateSubject(long UserId, string? JobGrade, long? DepartmentId);

/// <summary>The outcome of planning a new rate-line version.</summary>
public sealed record RateVersionPlan(long? CloseLineId, DateOnly? CloseOn, string? Problem)
{
    public bool IsValid => Problem is null;
}

/// <summary>
/// Pure rate-profile rules (RBAC v0.6 section 5). A profile reaches a person through the most
/// specific assignment in force on the expense date: Individual, then Grade, then Department,
/// then Company. Lines are versioned: a change never edits a line in place, it closes the open
/// version the day before the new one starts, so an ERF always uses the rate in force on its
/// expense date and old ERFs never change.
/// </summary>
public static class RateRules
{
    private static readonly RateTargetType[] Precedence =
        [RateTargetType.Individual, RateTargetType.Grade, RateTargetType.Department, RateTargetType.Company];

    /// <summary>The assignment that gives the subject their profile on the date, or null.</summary>
    public static RateAssignmentCandidate? ResolveAssignment(IEnumerable<RateAssignmentCandidate> assignments, RateSubject subject, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(subject);
        var inForce = assignments.Where(a => a.InForceOn(date) && Matches(a, subject)).ToList();

        foreach (var tier in Precedence)
        {
            var match = inForce
                .Where(a => a.TargetType == tier)
                .OrderByDescending(a => a.EffectiveFrom)
                .ThenByDescending(a => a.Id)
                .FirstOrDefault();
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>The line of a profile in force for a category (and meal sub-category) on the date.</summary>
    public static RateLineCandidate? ResolveLine(IEnumerable<RateLineCandidate> lines, long profileId, ExpenseCategory category, string? subCategory, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return lines
            .Where(l => l.ProfileId == profileId && l.Category == category && SameSub(l.SubCategory, subCategory) && l.InForceOn(date))
            .OrderByDescending(l => l.EffectiveFrom)
            .ThenByDescending(l => l.Id)
            .FirstOrDefault();
    }

    /// <summary>Whether an assignment targets the subject, ignoring dates.</summary>
    public static bool Matches(RateAssignmentCandidate assignment, RateSubject subject)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(subject);
        return assignment.TargetType switch
        {
            RateTargetType.Individual => assignment.UserId == subject.UserId,
            RateTargetType.Grade => subject.JobGrade is not null
                && string.Equals(assignment.JobGrade?.Trim(), subject.JobGrade.Trim(), StringComparison.OrdinalIgnoreCase),
            RateTargetType.Department => subject.DepartmentId is not null && assignment.DepartmentId == subject.DepartmentId,
            RateTargetType.Company => true,
            _ => false,
        };
    }

    /// <summary>
    /// Plans a new version for one category/sub-category key. The new version must start after
    /// every existing version of the key; the version still open on that day is closed the day before.
    /// </summary>
    public static RateVersionPlan PlanVersion(IEnumerable<RateLineCandidate> existingForKey, DateOnly newFrom, DateOnly? newTo)
    {
        ArgumentNullException.ThrowIfNull(existingForKey);
        if (newTo is { } to && to < newFrom)
        {
            return new RateVersionPlan(null, null, "The end date must be on or after the start date.");
        }

        var versions = existingForKey.OrderBy(l => l.EffectiveFrom).ToList();
        if (versions.Count > 0 && versions[^1].EffectiveFrom >= newFrom)
        {
            return new RateVersionPlan(null, null,
                $"A version already starts on {versions[^1].EffectiveFrom:yyyy-MM-dd}. A new version must start after it.");
        }

        var open = versions.LastOrDefault(l => l.EffectiveTo is null || l.EffectiveTo >= newFrom);
        return open is null
            ? new RateVersionPlan(null, null, null)
            : new RateVersionPlan(open.Id, newFrom.AddDays(-1), null);
    }

    /// <summary>
    /// A change that starts before today rewrites a period that may already hold filed ERFs, so it
    /// needs an explicit override and a reason (spec 2.4 lock 4).
    /// </summary>
    public static string? BackdateProblem(DateOnly effectiveFrom, DateOnly today, bool backdateOverride, string? reason)
    {
        if (effectiveFrom >= today)
        {
            return null;
        }

        return backdateOverride && !string.IsNullOrWhiteSpace(reason)
            ? null
            : "This change starts in the past. Tick the backdate override and give a reason.";
    }

    /// <summary>Two assignments for the same target overlap in time.</summary>
    public static bool Overlaps(DateOnly fromA, DateOnly? toA, DateOnly fromB, DateOnly? toB) =>
        fromA <= (toB ?? DateOnly.MaxValue) && fromB <= (toA ?? DateOnly.MaxValue);

    private static bool SameSub(string? a, string? b) =>
        string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
}
