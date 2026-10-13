using ERFWeb.Domain.Scope;

namespace ERFWeb.Domain.Approval;

/// <summary>One planned approval step.</summary>
public sealed record RoutedStep(int StepOrder, int Level, long ApproverUserId);

/// <summary>The approval route for a report at filing, or why it cannot be built.</summary>
public sealed record RoutePlan(IReadOnlyList<RoutedStep> Steps, string? Problem)
{
    public bool IsRouted => Problem is null && Steps.Count > 0;
}

/// <summary>Inputs that decide whether a Level 2 step is added.</summary>
public sealed record RoutingPolicy(bool Level2AlwaysRequired, decimal Level2ThresholdAmount);

/// <summary>
/// Builds the approval steps snapshotted at filing (plan section 13 "Routing at submission").
/// Level 1 comes from scope. A Level 2 step is added when the policy always requires it, when the
/// total reaches the threshold, when the total exceeds the Level 1's approval limit, or when the
/// claimant is themselves a signatory. If the claimant is their own resolved Level 1, their Level 2
/// acts alone as the first step. Nobody approves their own report (9.1).
/// </summary>
public static class ApprovalRouting
{
    public static RoutePlan Build(
        long claimantUserId,
        bool claimantIsSignatory,
        decimal reportTotal,
        ScopeResolution level1,
        ScopeResolution level2,
        RoutingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(level1);
        ArgumentNullException.ThrowIfNull(level2);
        ArgumentNullException.ThrowIfNull(policy);

        if (!level1.IsResolved)
        {
            return Fail(level1.Problem ?? "No Level 1 signatory covers your account.");
        }

        var claimantIsOwnLevel1 = level1.HolderUserId == claimantUserId;
        var limit = level1.Assignment!.ApprovalLimit;
        var needsLevel2 = claimantIsOwnLevel1
            || claimantIsSignatory
            || policy.Level2AlwaysRequired
            || (policy.Level2ThresholdAmount > 0 && reportTotal >= policy.Level2ThresholdAmount)
            || (limit is { } l && reportTotal > l);

        if (needsLevel2)
        {
            if (!level2.IsResolved)
            {
                return Fail(level2.Problem ?? "No Level 2 signatory covers your account.");
            }

            if (level2.HolderUserId == claimantUserId)
            {
                return Fail("You are your own Level 2 signatory. Ask the Business Owner to assign another Level 2 for your account.");
            }
        }

        var steps = new List<RoutedStep>();
        if (!claimantIsOwnLevel1)
        {
            steps.Add(new RoutedStep(1, 1, level1.HolderUserId!.Value));
        }

        if (needsLevel2 && (steps.Count == 0 || steps[0].ApproverUserId != level2.HolderUserId))
        {
            steps.Add(new RoutedStep(steps.Count + 1, 2, level2.HolderUserId!.Value));
        }

        return steps.Count == 0
            ? Fail("No approver other than you covers your account.")
            : new RoutePlan(steps, null);
    }

    private static RoutePlan Fail(string problem) => new([], problem);
}
