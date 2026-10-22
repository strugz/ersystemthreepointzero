namespace ERFWeb.Domain.Administration;

/// <summary>
/// Pure role-grant rules from the RBAC specification v0.6: which roles may stack on one
/// account (the three stacking rules) and the no-self-elevation guard. A grant made by a
/// System Administrator applies at once; there is no countersign step. Everything here is
/// UI- and persistence-independent.
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
    };

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
            return "A System Administrator cannot hold a transacting role (Signatory, Finance, or Expense Verifier).";
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
}

