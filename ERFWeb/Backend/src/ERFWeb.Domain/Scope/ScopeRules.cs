namespace ERFWeb.Domain.Scope;

/// <summary>One scope assignment as the resolver sees it.</summary>
public sealed record ScopeCandidate(
    long Id,
    long HolderUserId,
    int Level,
    ScopeAssignmentType Type,
    long? DepartmentId,
    IReadOnlySet<long> MemberUserIds,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    decimal? ApprovalLimit)
{
    public bool InForceOn(DateOnly date) =>
        IsActive && EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);
}

/// <summary>The employee being routed.</summary>
public sealed record ScopeSubject(long UserId, long? DepartmentId);

/// <summary>Result of resolving one signatory level for one employee.</summary>
public sealed record ScopeResolution(int Level, ScopeCandidate? Assignment, string? Problem, bool IsAmbiguous = false)
{
    public long? HolderUserId => Assignment?.HolderUserId;
    public bool IsResolved => Assignment is not null;
}

/// <summary>
/// Pure scope rules from the RBAC specification v0.6 section 4: who signs for an employee at a
/// level on a date. A by-name (Individual) assignment beats a by-department one; within the
/// winning tier there must be exactly one holder, otherwise the employee is ambiguous and filing
/// is blocked. Exclusion assignments cover a department minus their named members.
/// </summary>
public static class ScopeRules
{
    public static ScopeResolution Resolve(IEnumerable<ScopeCandidate> assignments, ScopeSubject subject, int level, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(subject);
        var inForce = assignments.Where(a => a.Level == level && a.InForceOn(date)).ToList();

        var byName = inForce
            .Where(a => a.Type == ScopeAssignmentType.Individual && a.MemberUserIds.Contains(subject.UserId))
            .ToList();
        if (byName.Count > 0)
        {
            return Pick(byName, level, "named");
        }

        if (subject.DepartmentId is not { } departmentId)
        {
            return new ScopeResolution(level, null, NoneMessage(level, hasDepartment: false));
        }

        var byDepartment = inForce
            .Where(a => a.DepartmentId == departmentId
                && (a.Type == ScopeAssignmentType.Department
                    || (a.Type == ScopeAssignmentType.Exclusion && !a.MemberUserIds.Contains(subject.UserId))))
            .ToList();

        return byDepartment.Count > 0
            ? Pick(byDepartment, level, "department")
            : new ScopeResolution(level, null, NoneMessage(level, hasDepartment: true));
    }

    /// <summary>Whether an assignment, if in force, would cover the subject (used to list affected people).</summary>
    public static bool Covers(ScopeCandidate assignment, ScopeSubject subject)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(subject);
        return assignment.Type switch
        {
            ScopeAssignmentType.Individual => assignment.MemberUserIds.Contains(subject.UserId),
            ScopeAssignmentType.Department => subject.DepartmentId is not null && assignment.DepartmentId == subject.DepartmentId,
            ScopeAssignmentType.Exclusion => subject.DepartmentId is not null && assignment.DepartmentId == subject.DepartmentId
                && !assignment.MemberUserIds.Contains(subject.UserId),
            _ => false,
        };
    }

    /// <summary>Shape checks for a new or edited assignment. Returns per-field errors, empty when valid.</summary>
    public static IReadOnlyDictionary<string, string> ShapeErrors(
        int level,
        ScopeAssignmentType type,
        long? departmentId,
        IReadOnlyCollection<long> memberUserIds,
        long holderUserId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        decimal? approvalLimit)
    {
        ArgumentNullException.ThrowIfNull(memberUserIds);
        var errors = new Dictionary<string, string>();

        if (level is not (1 or 2))
        {
            errors["level"] = "The signatory level must be 1 or 2.";
        }

        switch (type)
        {
            case ScopeAssignmentType.Individual:
                if (departmentId is not null)
                {
                    errors["departmentId"] = "A by-name assignment does not take a department.";
                }

                if (memberUserIds.Count == 0)
                {
                    errors["memberUserIds"] = "Name at least one person this signatory covers.";
                }

                break;
            case ScopeAssignmentType.Department:
                if (departmentId is null)
                {
                    errors["departmentId"] = "Choose the department this signatory covers.";
                }

                if (memberUserIds.Count > 0)
                {
                    errors["memberUserIds"] = "A department assignment covers everyone; use an exclusion to leave people out.";
                }

                break;
            case ScopeAssignmentType.Exclusion:
                if (departmentId is null)
                {
                    errors["departmentId"] = "Choose the department this signatory covers.";
                }

                if (memberUserIds.Count == 0)
                {
                    errors["memberUserIds"] = "Name at least one person to leave out of the department.";
                }

                break;
        }

        if (type == ScopeAssignmentType.Individual && memberUserIds.Contains(holderUserId))
        {
            errors["memberUserIds"] = "A signatory cannot be named in their own scope; their reports go to their Level 2.";
        }

        if (effectiveTo is { } to && to < effectiveFrom)
        {
            errors["effectiveTo"] = "The end date must be on or after the start date.";
        }

        if (approvalLimit is < 0)
        {
            errors["approvalLimit"] = "The approval limit cannot be negative.";
        }

        return errors;
    }

    private static ScopeResolution Pick(List<ScopeCandidate> tier, int level, string tierName)
    {
        var holders = tier.Select(a => a.HolderUserId).Distinct().ToList();
        if (holders.Count == 1)
        {
            // Several rows for the same holder are harmless; take the most recent.
            return new ScopeResolution(level, tier.OrderByDescending(a => a.EffectiveFrom).ThenByDescending(a => a.Id).First(), null);
        }

        return new ScopeResolution(level, null,
            $"More than one Level {level} signatory covers this person by {tierName} assignment. Each person must resolve to exactly one.",
            IsAmbiguous: true);
    }

    private static string NoneMessage(int level, bool hasDepartment) => hasDepartment
        ? $"No Level {level} signatory covers this person's department."
        : $"No Level {level} signatory covers this person, and they have no department.";
}
