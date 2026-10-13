namespace ERFWeb.Domain.Administration;

/// <summary>
/// Pure role-grant rules from the RBAC specification v0.6: which roles may stack on one
/// account (the three stacking rules), which grants need a countersign, who may countersign,
/// and the no-self-elevation guard. Everything here is UI- and persistence-independent.
/// </summary>
public static class RoleRules
{
    /// <summary>Roles that act on expense reports. A System Administrator may hold none of them.</summary>
    public static readonly IReadOnlySet<UserRole> TransactingRoles = new HashSet<UserRole>
    {
        UserRole.SignatoryLevel1,
        UserRole.SignatoryLevel2,
        UserRole.Finance,
        UserRole.ExpenseVerifier,
        UserRole.BusinessOwner,
    };

    /// <summary>Roles that are held in Pending Activation until countersigned (spec 9.8).</summary>
    public static readonly IReadOnlySet<UserRole> PrivilegedRoles = new HashSet<UserRole>
    {
        UserRole.SignatoryLevel1,
        UserRole.SignatoryLevel2,
        UserRole.Finance,
        UserRole.ExpenseVerifier,
        UserRole.SystemAdministrator,
        UserRole.BusinessOwner,
    };

    public static bool IsPrivileged(UserRole role) => PrivilegedRoles.Contains(role);

    public static bool IsTransacting(UserRole role) => TransactingRoles.Contains(role);

    /// <summary>
    /// The three stacking rules (spec section 3). Given the roles an account already holds
    /// (Pending or Active) and the role being added, returns the reason the save must be
    /// refused, or null when the combination is allowed. Employee always stacks.
    /// </summary>
    public static string? StackingViolation(IEnumerable<UserRole> held, UserRole adding)
    {
        ArgumentNullException.ThrowIfNull(held);
        var current = new HashSet<UserRole>(held) { adding };

        if (current.Contains(UserRole.SystemAdministrator) && current.Overlaps(TransactingRoles))
        {
            return "A System Administrator cannot hold a transacting role (Signatory, Finance, Expense Verifier, or Business Owner).";
        }

        if (current.Contains(UserRole.Finance)
            && (current.Contains(UserRole.SignatoryLevel1) || current.Contains(UserRole.SignatoryLevel2)))
        {
            return "Finance cannot also be a Signatory.";
        }

        if (current.Contains(UserRole.Finance) && current.Contains(UserRole.ExpenseVerifier))
        {
            return "Finance cannot also be an Expense Verifier.";
        }

        return null;
    }

    /// <summary>No self-elevation (spec 9.7): an administrator never changes their own account.</summary>
    public static bool IsSelfChange(long actorUserId, long targetUserId) => actorUserId == targetUserId;

    /// <summary>The base role can never be granted explicitly or revoked.</summary>
    public static bool IsBaseRole(UserRole role) => role == UserRole.Employee;

    /// <summary>
    /// Whether <paramref name="countersignerRoles"/> may countersign a grant made by
    /// <paramref name="granterUserId"/> to <paramref name="targetUserId"/>. The designated
    /// Business Owner countersigns (spec 9.8). While no Business Owner exists yet, a second
    /// System Administrator may countersign instead; that is the bootstrap exception the plan
    /// records as open item 12.3a and the caller must flag the grant as bootstrap.
    /// </summary>
    public static CountersignDecision Countersign(
        long countersignerUserId,
        IEnumerable<UserRole> countersignerRoles,
        long granterUserId,
        long targetUserId,
        bool businessOwnerExists)
    {
        ArgumentNullException.ThrowIfNull(countersignerRoles);
        var roles = new HashSet<UserRole>(countersignerRoles);

        if (countersignerUserId == targetUserId)
        {
            return CountersignDecision.Refused("You cannot countersign a role on your own account.");
        }

        if (countersignerUserId == granterUserId)
        {
            return CountersignDecision.Refused("The person who granted the role cannot also countersign it. Dual approval needs two people.");
        }

        if (roles.Contains(UserRole.BusinessOwner))
        {
            return CountersignDecision.Allowed(bootstrap: false);
        }

        if (!businessOwnerExists && roles.Contains(UserRole.SystemAdministrator))
        {
            return CountersignDecision.Allowed(bootstrap: true);
        }

        return CountersignDecision.Refused(businessOwnerExists
            ? "Only the designated Business Owner can countersign role grants."
            : "Only a System Administrator can countersign while no Business Owner has been designated.");
    }
}

/// <summary>Outcome of a countersign check.</summary>
public sealed record CountersignDecision(bool IsAllowed, bool IsBootstrap, string? Reason)
{
    public static CountersignDecision Allowed(bool bootstrap) => new(true, bootstrap, null);

    public static CountersignDecision Refused(string reason) => new(false, false, reason);
}
