namespace ERFWeb.Domain.Scope;

/// <summary>Outcome of an authority check for a scope or rate change.</summary>
public sealed record AuthorityDecision(bool IsAllowed, bool IsBootstrap, string? Reason)
{
    public static AuthorityDecision Allowed(bool bootstrap = false) => new(true, bootstrap, null);

    public static AuthorityDecision Refused(string reason) => new(false, false, reason);
}

/// <summary>What a rate assignment or rate-line change applies to, for the self-rate check.</summary>
public sealed record RateTarget(RateTargetType Type, long? UserId, string? JobGrade, long? DepartmentId);

/// <summary>The person making a scope or rate change.</summary>
public sealed record EntitlementActor(long UserId, IReadOnlySet<UserRole> Roles, string? JobGrade, long? DepartmentId)
{
    public bool Has(UserRole role) => Roles.Contains(role);
}

/// <summary>
/// Who may change scope and rates (RBAC v0.6 sections 2.4, 9.12, 9.13, 9.15).
/// <list type="bullet">
/// <item>Level 1 scope is set by a Level 2 Signatory or the Business Owner.</item>
/// <item>Level 2 scope is set by the Business Owner. Before a Business Owner exists, another Level 2
/// may set it, and a System Administrator may create it only while no Level 2 covers the area; both
/// are flagged as bootstrap.</item>
/// <item>Nobody changes scope they hold themselves (9.13).</item>
/// <item>Rates are set by a Level 2 Signatory or the Business Owner (9.15), never for a target that
/// includes the actor (9.12). The company-wide default includes everyone, so only the Business Owner
/// sets it; before a Business Owner exists, a Level 2 may, flagged as bootstrap.</item>
/// </list>
/// </summary>
public static class EntitlementAuthority
{
    public static AuthorityDecision ForScope(
        EntitlementActor actor,
        long holderUserId,
        int level,
        bool businessOwnerExists,
        bool areaHasLevel2Coverage)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (actor.UserId == holderUserId)
        {
            return AuthorityDecision.Refused("You cannot change a scope you hold yourself. The Business Owner does that.");
        }

        if (actor.Has(UserRole.BusinessOwner))
        {
            return AuthorityDecision.Allowed();
        }

        if (level == 1)
        {
            return actor.Has(UserRole.SignatoryLevel2)
                ? AuthorityDecision.Allowed()
                : AuthorityDecision.Refused("Only a Level 2 Signatory or the Business Owner can set Level 1 scope.");
        }

        if (!businessOwnerExists && actor.Has(UserRole.SignatoryLevel2))
        {
            return AuthorityDecision.Allowed(bootstrap: true);
        }

        if (actor.Has(UserRole.SystemAdministrator) && !areaHasLevel2Coverage)
        {
            return AuthorityDecision.Allowed(bootstrap: true);
        }

        return AuthorityDecision.Refused(actor.Has(UserRole.SystemAdministrator)
            ? "A System Administrator can create Level 2 scope only while no Level 2 covers that area."
            : "Only the Business Owner can set Level 2 scope.");
    }

    public static AuthorityDecision ForRates(EntitlementActor actor, RateTarget target, bool businessOwnerExists)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(target);

        var isOwner = actor.Has(UserRole.BusinessOwner);
        if (!isOwner && !actor.Has(UserRole.SignatoryLevel2))
        {
            return AuthorityDecision.Refused("Only a Level 2 Signatory or the Business Owner can set rates.");
        }

        if (IncludesActor(actor, target))
        {
            return AuthorityDecision.Refused("You cannot set a rate that applies to yourself.");
        }

        if (target.Type == RateTargetType.Company && !isOwner)
        {
            return businessOwnerExists
                ? AuthorityDecision.Refused("Only the Business Owner can set the company-wide default rate.")
                : AuthorityDecision.Allowed(bootstrap: true);
        }

        return AuthorityDecision.Allowed();
    }

    /// <summary>Whether a target covers the actor. The company default covers everyone but is handled separately.</summary>
    public static bool IncludesActor(EntitlementActor actor, RateTarget target)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(target);
        return target.Type switch
        {
            RateTargetType.Individual => target.UserId == actor.UserId,
            RateTargetType.Grade => actor.JobGrade is not null
                && string.Equals(target.JobGrade?.Trim(), actor.JobGrade.Trim(), StringComparison.OrdinalIgnoreCase),
            RateTargetType.Department => actor.DepartmentId is not null && target.DepartmentId == actor.DepartmentId,
            _ => false,
        };
    }
}
