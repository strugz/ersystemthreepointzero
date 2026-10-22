namespace ERFWeb.Domain.Approval;

/// <summary>What an approver does to a report.</summary>
public enum ApprovalAction
{
    Approve,
    Return,
    Reject,

    /// <summary>A Level 2 approves past a pending Level 1, with a reason.</summary>
    Override,
}

/// <summary>One approval step of the current cycle, as the workflow sees it.</summary>
public sealed record WorkflowStep(long Id, int StepOrder, int? Level, long ApproverUserId, ApprovalStatus Status);

/// <summary>Why an action was refused: the caller maps Forbidden to 403 and Conflict to 409.</summary>
public enum WorkflowRefusal
{
    None,
    Forbidden,
    Conflict,
    Invalid,
}

/// <summary>The outcome of an approval decision: the step changes and the resulting report status.</summary>
public sealed record WorkflowDecision(
    WorkflowRefusal Refusal,
    string? Reason,
    IReadOnlyDictionary<long, ApprovalStatus> StepChanges,
    ReportStatus? NewReportStatus,
    long? ActingStepId)
{
    public bool IsAllowed => Refusal == WorkflowRefusal.None;

    /// <summary>True when this action completed the last step and the report is now approved.</summary>
    public bool CompletesApproval => NewReportStatus == ReportStatus.Approved;

    public static WorkflowDecision Refused(WorkflowRefusal refusal, string reason) =>
        new(refusal, reason, new Dictionary<long, ApprovalStatus>(), null, null);
}

/// <summary>What the viewer may do on a report, for the review screen.</summary>
public sealed record ReviewPermissions(bool CanApprove, bool CanReturn, bool CanReject, bool CanOverride, bool CanAdjustLines);

/// <summary>
/// Pure approval rules for Phase 5 (plan section 13, RBAC v0.6 flow 5). Steps run in order; only
/// the approver of the lowest pending step acts, except that a Level 2 may override a pending
/// Level 1 with a reason. Return and reject need a reason. Nobody acts on their own report (9.1).
/// Amounts may be lowered only by the current Level 1, before they approve; after that they are
/// locked (9.6).
/// A delegate acts on the steps of the signatories they stand in for (<c>actingFor</c>), unless
/// they are themselves an approver on the report: one person never fills two levels of the same
/// report through a delegation (a Level 2 who needs to move a stuck Level 1 overrides instead).
/// </summary>
public static class ApprovalWorkflow
{
    public const int MaxReasonLength = 1000;

    public static WorkflowDecision Decide(
        ReportStatus reportStatus,
        IReadOnlyList<WorkflowStep> steps,
        long claimantUserId,
        long actorUserId,
        ApprovalAction action,
        string? reason,
        IReadOnlySet<long>? actingFor = null)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (actorUserId == claimantUserId)
        {
            return WorkflowDecision.Refused(WorkflowRefusal.Forbidden, "You cannot act on your own report.");
        }

        var mine = ActorSteps(steps, actorUserId, actingFor);
        if (mine.Count == 0)
        {
            return WorkflowDecision.Refused(WorkflowRefusal.Forbidden, "You are not an approver on this report.");
        }

        if (reportStatus != ReportStatus.ForApproval)
        {
            return WorkflowDecision.Refused(WorkflowRefusal.Conflict, $"The report is {reportStatus} and no longer awaits approval.");
        }

        var myPending = mine.Where(s => s.Status == ApprovalStatus.Pending).OrderBy(s => s.StepOrder).FirstOrDefault();
        if (myPending is null)
        {
            return WorkflowDecision.Refused(WorkflowRefusal.Conflict, "You have already acted on this report.");
        }

        if (ReasonError(action, reason) is { } reasonError)
        {
            return WorkflowDecision.Refused(WorkflowRefusal.Invalid, reasonError);
        }

        var pending = steps.Where(s => s.Status == ApprovalStatus.Pending).OrderBy(s => s.StepOrder).ToList();
        var current = pending[0];
        var changes = new Dictionary<long, ApprovalStatus>();

        if (action == ApprovalAction.Override)
        {
            if (myPending.Level != 2)
            {
                return WorkflowDecision.Refused(WorkflowRefusal.Forbidden, "Only the Level 2 signatory on this report can override.");
            }

            if (current.Id == myPending.Id)
            {
                return WorkflowDecision.Refused(WorkflowRefusal.Conflict, "Nothing to override: it is already your turn. Approve instead.");
            }

            foreach (var earlier in pending.Where(s => s.StepOrder < myPending.StepOrder))
            {
                changes[earlier.Id] = ApprovalStatus.Overridden;
            }

            changes[myPending.Id] = ApprovalStatus.Approved;
            return Finish(steps, changes, myPending.Id);
        }

        if (current.Id != myPending.Id)
        {
            return WorkflowDecision.Refused(WorkflowRefusal.Conflict, $"Step {current.StepOrder} has not been completed yet. Wait for it, or override if you are the Level 2.");
        }

        switch (action)
        {
            case ApprovalAction.Approve:
                changes[current.Id] = ApprovalStatus.Approved;
                return Finish(steps, changes, current.Id);

            case ApprovalAction.Return:
            case ApprovalAction.Reject:
                changes[current.Id] = action == ApprovalAction.Return ? ApprovalStatus.Returned : ApprovalStatus.Rejected;
                foreach (var later in pending.Where(s => s.Id != current.Id))
                {
                    changes[later.Id] = ApprovalStatus.Superseded;
                }

                return new WorkflowDecision(WorkflowRefusal.None, null, changes,
                    action == ApprovalAction.Return ? ReportStatus.Returned : ReportStatus.Rejected, current.Id);

            default:
                return WorkflowDecision.Refused(WorkflowRefusal.Invalid, "Unknown action.");
        }
    }

    /// <summary>What the viewer may do now. Computed with the same rules the actions enforce.</summary>
    public static ReviewPermissions Permissions(
        ReportStatus reportStatus, IReadOnlyList<WorkflowStep> steps, long claimantUserId, long viewerUserId, IReadOnlySet<long>? actingFor = null)
    {
        ArgumentNullException.ThrowIfNull(steps);
        bool Allowed(ApprovalAction a) => Decide(reportStatus, steps, claimantUserId, viewerUserId, a, "x", actingFor).IsAllowed;
        return new ReviewPermissions(
            Allowed(ApprovalAction.Approve),
            Allowed(ApprovalAction.Return),
            Allowed(ApprovalAction.Reject),
            Allowed(ApprovalAction.Override),
            AdjustmentError(reportStatus, steps, claimantUserId, viewerUserId, actingFor) is null);
    }

    /// <summary>Whether the viewer may lower a line amount now; null when allowed.</summary>
    public static string? AdjustmentError(
        ReportStatus reportStatus, IReadOnlyList<WorkflowStep> steps, long claimantUserId, long actorUserId, IReadOnlySet<long>? actingFor = null)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if (actorUserId == claimantUserId)
        {
            return "You cannot adjust your own report.";
        }

        if (reportStatus != ReportStatus.ForApproval)
        {
            return "Amounts can be adjusted only while the report awaits approval.";
        }

        var current = steps.Where(s => s.Status == ApprovalStatus.Pending).OrderBy(s => s.StepOrder).FirstOrDefault();
        if (current is null || !ActorSteps(steps, actorUserId, actingFor).Any(s => s.Id == current.Id))
        {
            return "Only the approver whose turn it is can adjust amounts.";
        }

        // Amounts are locked once any step has been approved (9.6), so only the first acting step may adjust.
        return steps.Any(s => s.Status is ApprovalStatus.Approved or ApprovalStatus.Overridden)
            ? "Amounts are locked after the first approval."
            : null;
    }

    /// <summary>An adjustment may only lower an amount, and needs a reason.</summary>
    public static string? AdjustedAmountError(decimal currentAmount, decimal newAmount, string? reason)
    {
        if (newAmount < 0)
        {
            return "The amount cannot be negative.";
        }

        if (newAmount >= currentAmount)
        {
            return "An approver can only lower an amount. Return the report if it needs to go up.";
        }

        return string.IsNullOrWhiteSpace(reason) ? "Give a reason for the adjustment." : null;
    }

    public static string? ReasonError(ApprovalAction action, string? reason)
    {
        var needsReason = action is ApprovalAction.Return or ApprovalAction.Reject or ApprovalAction.Override;
        if (needsReason && string.IsNullOrWhiteSpace(reason))
        {
            return action switch
            {
                ApprovalAction.Return => "Say what the employee must correct.",
                ApprovalAction.Reject => "Give the reason for rejecting the report.",
                _ => "Give the reason for overriding the earlier step.",
            };
        }

        return reason is { Length: > MaxReasonLength } ? $"The reason must be at most {MaxReasonLength} characters." : null;
    }

    /// <summary>
    /// The steps the actor may act on: their own, or, when they have none on this report, the steps
    /// of the signatories they are delegated for.
    /// </summary>
    public static List<WorkflowStep> ActorSteps(IReadOnlyList<WorkflowStep> steps, long actorUserId, IReadOnlySet<long>? actingFor)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var own = steps.Where(s => s.ApproverUserId == actorUserId).ToList();
        if (own.Count > 0 || actingFor is not { Count: > 0 })
        {
            return own;
        }

        return steps.Where(s => actingFor.Contains(s.ApproverUserId)).ToList();
    }

    private static WorkflowDecision Finish(IReadOnlyList<WorkflowStep> steps, Dictionary<long, ApprovalStatus> changes, long actingStepId)
    {
        var done = steps.All(s =>
        {
            var status = changes.TryGetValue(s.Id, out var changed) ? changed : s.Status;
            return status is ApprovalStatus.Approved or ApprovalStatus.Overridden;
        });

        return new WorkflowDecision(WorkflowRefusal.None, null, changes, done ? ReportStatus.Approved : null, actingStepId);
    }
}
