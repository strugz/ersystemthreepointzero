namespace ERFWeb.Domain.Approval;

/// <summary>One delegation as the rules see it.</summary>
public sealed record DelegationWindow(long Id, long SignatoryUserId, long DelegateUserId, DateOnly StartsOn, DateOnly EndsOn, bool IsCancelled)
{
    public bool InForceOn(DateOnly date) => !IsCancelled && StartsOn <= date && EndsOn >= date;

    /// <summary>Not cancelled and not yet over: it still blocks an overlapping delegation.</summary>
    public bool IsLive(DateOnly today) => !IsCancelled && EndsOn >= today;

    public bool Overlaps(DateOnly startsOn, DateOnly endsOn) => StartsOn <= endsOn && EndsOn >= startsOn;
}

/// <summary>Where a delegation stands on a given day.</summary>
public enum DelegationState
{
    Scheduled,
    Active,
    Ended,
    Cancelled,
}

/// <summary>A proposed delegation and the facts needed to judge it.</summary>
/// <param name="SignatoryLevel">The signatory's highest Active level; null when they are not a signatory.</param>
/// <param name="DelegateLevel">The delegate's highest Active level; null when they are not a signatory.</param>
/// <param name="ActorIsLevel2Over">The actor is the Level 2 over the signatory (needed to assign for someone else).</param>
public sealed record DelegationProposal(
    long ActorUserId,
    long SignatoryUserId,
    int? SignatoryLevel,
    long DelegateUserId,
    int? DelegateLevel,
    DateOnly StartsOn,
    DateOnly EndsOn,
    bool ActorIsLevel2Over);

/// <summary>The verdict on a proposal; <see cref="IsForced"/> is true when a Level 2 assigns for a Level 1.</summary>
public sealed record DelegationVerdict(WorkflowRefusal Refusal, string? Reason, bool IsForced)
{
    public bool IsAllowed => Refusal == WorkflowRefusal.None;
}

/// <summary>
/// Pure delegation rules (plan Phase 7, Flow 7). A signatory names a delegate of the same level or
/// higher for a date range; while it is in force the delegate acts on the signatory's approval
/// steps, stamped "by [Delegate] on behalf of [Signatory]". If a Level 1 left no delegate, their
/// Level 2 may assign one. A signatory has at most one live delegation for any day. Only approving
/// is delegated: rate and scope authority stays with the original Level 2.
/// </summary>
public static class DelegationRules
{
    public const int MaxReasonLength = 1000;

    /// <summary>The longest range one delegation may cover; a longer absence calls for a scope change.</summary>
    public const int MaxDays = 90;

    public static DelegationVerdict Check(DelegationProposal proposal, IEnumerable<DelegationWindow> existing, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(existing);
        var p = proposal;
        var forced = p.ActorUserId != p.SignatoryUserId;

        if (p.SignatoryLevel is not { } level)
        {
            return Refuse(WorkflowRefusal.Invalid, forced ? "That person is not a signatory." : "Only a signatory can delegate approvals.");
        }

        if (forced && (level != 1 || !p.ActorIsLevel2Over))
        {
            return Refuse(WorkflowRefusal.Forbidden, "You can assign a delegate only for a Level 1 signatory you are the Level 2 over.");
        }

        if (p.DelegateUserId == p.SignatoryUserId)
        {
            return Refuse(WorkflowRefusal.Invalid, "A signatory cannot delegate to themselves.");
        }

        if (forced && p.DelegateUserId == p.ActorUserId)
        {
            return Refuse(WorkflowRefusal.Invalid, "Choose someone else. To act on a report yourself, override the pending Level 1.");
        }

        if (p.DelegateLevel is not { } delegateLevel || delegateLevel < level)
        {
            return Refuse(WorkflowRefusal.Invalid, $"The delegate must be a Level {level} signatory or higher.");
        }

        if (p.EndsOn < p.StartsOn)
        {
            return Refuse(WorkflowRefusal.Invalid, "The end date cannot be before the start date.");
        }

        if (p.EndsOn < today)
        {
            return Refuse(WorkflowRefusal.Invalid, "The date range has already ended.");
        }

        if (p.EndsOn.DayNumber - p.StartsOn.DayNumber + 1 > MaxDays)
        {
            return Refuse(WorkflowRefusal.Invalid, $"A delegation can cover at most {MaxDays} days.");
        }

        var clash = existing.FirstOrDefault(d => d.SignatoryUserId == p.SignatoryUserId && d.IsLive(today) && d.Overlaps(p.StartsOn, p.EndsOn));
        if (clash is not null)
        {
            return Refuse(WorkflowRefusal.Conflict,
                $"A delegation from {clash.StartsOn:yyyy-MM-dd} to {clash.EndsOn:yyyy-MM-dd} already covers part of that range. Cancel it first.");
        }

        return new DelegationVerdict(WorkflowRefusal.None, null, forced);
    }

    /// <summary>The signatory may cancel their own delegation; whoever created it (the Level 2, if forced) may too.</summary>
    public static string? CancelError(DelegationWindow delegation, long actorUserId, long createdByUserId, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(delegation);
        if (actorUserId != delegation.SignatoryUserId && actorUserId != createdByUserId)
        {
            return "Only the signatory, or whoever assigned the delegation, can cancel it.";
        }

        if (delegation.IsCancelled)
        {
            return "The delegation is already cancelled.";
        }

        return delegation.EndsOn < today ? "The delegation has already ended." : null;
    }

    public static DelegationState StateOn(DelegationWindow delegation, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(delegation);
        if (delegation.IsCancelled)
        {
            return DelegationState.Cancelled;
        }

        if (delegation.EndsOn < date)
        {
            return DelegationState.Ended;
        }

        return delegation.StartsOn > date ? DelegationState.Scheduled : DelegationState.Active;
    }

    /// <summary>
    /// The signatories the user acts for on <paramref name="date"/>, each with the delegation that allows it.
    /// Delegation is not transitive: a delegate's own delegate does not inherit the first signatory.
    /// </summary>
    public static IReadOnlyDictionary<long, long> ActingFor(IEnumerable<DelegationWindow> delegations, long userId, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(delegations);
        return delegations
            .Where(d => d.DelegateUserId == userId && d.InForceOn(date))
            .GroupBy(d => d.SignatoryUserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Id).First().Id);
    }

    private static DelegationVerdict Refuse(WorkflowRefusal refusal, string reason) => new(refusal, reason, false);
}
