namespace ERFWeb.Domain.Scope;

/// <summary>Outcome of an authority check for a scope or rate change.</summary>
public sealed record AuthorityDecision(bool IsAllowed, string? Reason)
{
    public static AuthorityDecision Allowed() => new(true, null);

    public static AuthorityDecision Refused(string reason) => new(false, reason);
}

/// <summary>What a rate assignment or rate-line change applies to, for the self-rate check.</summary>
public sealed record RateTarget(RateTargetType Type, long? UserId, string? JobGrade, long? DepartmentId);

/// <summary>The person making a scope or rate change.</summary>
public sealed record EntitlementActor(long UserId, IReadOnlySet<UserRole> Roles, string? JobGrade, long? DepartmentId)
{
    public bool Has(UserRole role) => Roles.Contains(role);
}

/// <summary>
/// Who may change scope and rates. Both are administration settings kept by the System
/// Administrator, who holds no signatory role and so never sets scope they hold. The one
/// self-interest guard left is 9.12: an administrator never names themselves in a rate
/// assignment. Grade, department and company targets stay open, because the only
/// administrator may share a department or grade with the people being rated.
/// </summary>
public static class EntitlementAuthority
{
    public static AuthorityDecision ForScope(EntitlementActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.Has(UserRole.SystemAdministrator)
            ? AuthorityDecision.Allowed()
            : AuthorityDecision.Refused("Only a System Administrator can set scope.");
    }

    public static AuthorityDecision ForRates(EntitlementActor actor, RateTarget target)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(target);

        if (!actor.Has(UserRole.SystemAdministrator))
        {
            return AuthorityDecision.Refused("Only a System Administrator can set rates.");
        }

        return target.Type == RateTargetType.Individual && target.UserId == actor.UserId
            ? AuthorityDecision.Refused("You cannot set a rate that applies to you by name.")
            : AuthorityDecision.Allowed();
    }
}
