namespace ERFWeb.Domain.Approval;

/// <summary>
/// A single approval step, reduced to the fields the ordering rule needs.
/// </summary>
public readonly record struct ApprovalStepState(int StepOrder, ApprovalStatus Status);

/// <summary>
/// Pure rules that enforce ordered, multi-approver sequencing. A step is only
/// actionable when every step with a lower <see cref="ApprovalStepState.StepOrder"/>
/// in the same cycle is already <see cref="ApprovalStatus.Approved"/>.
/// </summary>
public static class ApprovalSequence
{
    /// <summary>
    /// Returns true if the step at <paramref name="stepOrder"/> may be approved now:
    /// it must itself be pending and all earlier steps must be approved.
    /// </summary>
    public static bool CanApprove(IReadOnlyCollection<ApprovalStepState> steps, int stepOrder)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var target = FindStep(steps, stepOrder);
        if (target is null || target.Value.Status != ApprovalStatus.Pending)
        {
            return false;
        }

        foreach (var step in steps)
        {
            if (step.StepOrder < stepOrder && step.Status != ApprovalStatus.Approved)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The lowest-ordered pending step, i.e. the one currently awaiting action.</summary>
    public static int? CurrentStepOrder(IReadOnlyCollection<ApprovalStepState> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        int? current = null;
        foreach (var step in steps)
        {
            if (step.Status == ApprovalStatus.Pending && (current is null || step.StepOrder < current))
            {
                current = step.StepOrder;
            }
        }

        return current;
    }

    /// <summary>True when every step is approved, i.e. the report is fully approved.</summary>
    public static bool IsFullyApproved(IReadOnlyCollection<ApprovalStepState> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return steps.Count > 0 && steps.All(s => s.Status == ApprovalStatus.Approved);
    }

    private static ApprovalStepState? FindStep(IEnumerable<ApprovalStepState> steps, int stepOrder)
    {
        foreach (var step in steps)
        {
            if (step.StepOrder == stepOrder)
            {
                return step;
            }
        }

        return null;
    }
}
