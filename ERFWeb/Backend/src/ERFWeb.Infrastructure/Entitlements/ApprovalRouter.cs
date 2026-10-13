using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Entitlements;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Scope;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Entitlements;

/// <summary>
/// Builds a claimant's approval route from scope and the workflow rules (plan section 13). Replaces
/// the fixed <c>approval_routes</c> lookup: the filing service snapshots the result into
/// <c>approval_steps</c>, so later scope changes never re-route an in-flight report (9.3).
/// </summary>
public sealed class ApprovalRouter(ErfDbContext db, EntitlementContext context, ISystemSettingsService settings) : IApprovalRouter
{
    public async Task<RoutingResultDto> RouteAsync(long claimantUserId, decimal reportTotal, DateOnly date, CancellationToken ct = default)
    {
        var claimant = await context.SubjectAsync(claimantUserId, ct);
        if (claimant is null)
        {
            return new RoutingResultDto([], "Your account was not found.");
        }

        var candidates = await context.ScopeCandidatesAsync(validHoldersOnly: true, ct);
        var level1 = ScopeRules.Resolve(candidates, claimant.Scope, 1, date);
        var level2 = ScopeRules.Resolve(candidates, claimant.Scope, 2, date);

        var rules = await settings.GetAsync(ct);
        var plan = ApprovalRouting.Build(
            claimantUserId,
            await context.IsSignatoryAsync(claimantUserId, ct),
            reportTotal,
            level1,
            level2,
            new RoutingPolicy(rules.Level2AlwaysRequired, rules.Level2ThresholdAmount));

        if (!plan.IsRouted)
        {
            return new RoutingResultDto([], plan.Problem);
        }

        var ids = plan.Steps.Select(s => s.ApproverUserId).ToList();
        var people = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email, u.MobileNo })
            .ToDictionaryAsync(u => u.Id, ct);

        var approvers = plan.Steps
            .Select(s => new RoutedApproverDto(s.StepOrder, s.Level, s.ApproverUserId,
                people[s.ApproverUserId].FullName, people[s.ApproverUserId].Email, people[s.ApproverUserId].MobileNo))
            .ToList();
        return new RoutingResultDto(approvers, null);
    }
}
