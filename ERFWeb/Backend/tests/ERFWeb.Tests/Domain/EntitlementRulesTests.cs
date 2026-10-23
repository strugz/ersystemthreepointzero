using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Rates;
using ERFWeb.Domain.Scope;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class ScopeRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 13);
    private const long Sales = 10;
    private const long Maria = 1;   // Level 1 for Sales
    private const long Jose = 2;    // Level 1 by name
    private const long Ramon = 3;   // Level 2
    private const long Ana = 100;   // employee in Sales
    private const long Ben = 101;   // employee in Sales

    private static ScopeCandidate Dept(long id, long holder, int level, long dept, decimal? limit = null) =>
        new(id, holder, level, ScopeAssignmentType.Department, dept, new HashSet<long>(), Today.AddDays(-30), null, true, limit);

    private static ScopeCandidate Named(long id, long holder, int level, params long[] members) =>
        new(id, holder, level, ScopeAssignmentType.Individual, null, members.ToHashSet(), Today.AddDays(-30), null, true, null);

    private static ScopeCandidate Excluding(long id, long holder, int level, long dept, params long[] members) =>
        new(id, holder, level, ScopeAssignmentType.Exclusion, dept, members.ToHashSet(), Today.AddDays(-30), null, true, null);

    [Fact]
    public void Department_assignment_covers_its_people()
    {
        var result = ScopeRules.Resolve([Dept(1, Maria, 1, Sales)], new ScopeSubject(Ana, Sales), 1, Today);
        Assert.Equal(Maria, result.HolderUserId);
    }

    [Fact]
    public void Named_assignment_beats_department()
    {
        var result = ScopeRules.Resolve([Dept(1, Maria, 1, Sales), Named(2, Jose, 1, Ana)], new ScopeSubject(Ana, Sales), 1, Today);
        Assert.Equal(Jose, result.HolderUserId);
    }

    [Fact]
    public void Exclusion_leaves_out_the_named_people()
    {
        var assignments = new[] { Excluding(1, Maria, 1, Sales, Ben) };
        Assert.Equal(Maria, ScopeRules.Resolve(assignments, new ScopeSubject(Ana, Sales), 1, Today).HolderUserId);
        Assert.False(ScopeRules.Resolve(assignments, new ScopeSubject(Ben, Sales), 1, Today).IsResolved);
    }

    [Fact]
    public void Two_department_signatories_are_ambiguous()
    {
        var result = ScopeRules.Resolve([Dept(1, Maria, 1, Sales), Dept(2, Jose, 1, Sales)], new ScopeSubject(Ana, Sales), 1, Today);
        Assert.False(result.IsResolved);
        Assert.True(result.IsAmbiguous);
    }

    [Fact]
    public void Ended_or_inactive_assignments_do_not_count()
    {
        var ended = Dept(1, Maria, 1, Sales) with { EffectiveTo = Today.AddDays(-1) };
        var inactive = Dept(2, Jose, 1, Sales) with { IsActive = false };
        var future = Dept(3, Ramon, 1, Sales) with { EffectiveFrom = Today.AddDays(1) };
        var result = ScopeRules.Resolve([ended, inactive, future], new ScopeSubject(Ana, Sales), 1, Today);
        Assert.False(result.IsResolved);
        Assert.False(result.IsAmbiguous);
    }

    [Fact]
    public void No_department_and_no_named_assignment_is_unresolved() =>
        Assert.False(ScopeRules.Resolve([Dept(1, Maria, 1, Sales)], new ScopeSubject(Ana, null), 1, Today).IsResolved);

    [Theory]
    [InlineData(ScopeAssignmentType.Individual, true, false, "departmentId")]
    [InlineData(ScopeAssignmentType.Individual, false, false, "memberUserIds")]
    [InlineData(ScopeAssignmentType.Department, false, false, "departmentId")]
    [InlineData(ScopeAssignmentType.Department, true, true, "memberUserIds")]
    [InlineData(ScopeAssignmentType.Exclusion, true, false, "memberUserIds")]
    public void Shape_errors_name_the_wrong_field(ScopeAssignmentType type, bool withDepartment, bool withMembers, string field)
    {
        var errors = ScopeRules.ShapeErrors(1, type, withDepartment ? Sales : null, withMembers ? [Ana] : [], Maria, Today, null, null);
        Assert.Contains(field, errors.Keys);
    }

    [Fact]
    public void A_signatory_cannot_be_named_in_their_own_scope()
    {
        var errors = ScopeRules.ShapeErrors(1, ScopeAssignmentType.Individual, null, [Maria], Maria, Today, null, null);
        Assert.Contains("memberUserIds", errors.Keys);
    }

    [Fact]
    public void Routing_adds_level2_when_policy_requires_it()
    {
        var l1 = ScopeRules.Resolve([Dept(1, Maria, 1, Sales)], new ScopeSubject(Ana, Sales), 1, Today);
        var l2 = ScopeRules.Resolve([Dept(2, Ramon, 2, Sales)], new ScopeSubject(Ana, Sales), 2, Today);

        var always = ApprovalRouting.Build(Ana, false, 100m, l1, l2, new RoutingPolicy(true, 0m));
        Assert.Equal([Maria, Ramon], always.Steps.Select(s => s.ApproverUserId));

        var below = ApprovalRouting.Build(Ana, false, 100m, l1, l2, new RoutingPolicy(false, 5000m));
        Assert.Equal([Maria], below.Steps.Select(s => s.ApproverUserId));

        var above = ApprovalRouting.Build(Ana, false, 5000m, l1, l2, new RoutingPolicy(false, 5000m));
        Assert.Equal([Maria, Ramon], above.Steps.Select(s => s.ApproverUserId));
    }

    [Fact]
    public void Routing_escalates_above_the_level1_limit()
    {
        var l1 = ScopeRules.Resolve([Dept(1, Maria, 1, Sales, limit: 1000m)], new ScopeSubject(Ana, Sales), 1, Today);
        var l2 = ScopeRules.Resolve([Dept(2, Ramon, 2, Sales)], new ScopeSubject(Ana, Sales), 2, Today);
        var plan = ApprovalRouting.Build(Ana, false, 1500m, l1, l2, new RoutingPolicy(false, 0m));
        Assert.Equal([1, 2], plan.Steps.Select(s => s.Level));
    }

    [Fact]
    public void Signatory_filing_their_own_report_goes_to_level2_alone()
    {
        var l1 = ScopeRules.Resolve([Dept(1, Maria, 1, Sales)], new ScopeSubject(Maria, Sales), 1, Today);
        var l2 = ScopeRules.Resolve([Dept(2, Ramon, 2, Sales)], new ScopeSubject(Maria, Sales), 2, Today);
        var plan = ApprovalRouting.Build(Maria, true, 100m, l1, l2, new RoutingPolicy(false, 0m));
        var step = Assert.Single(plan.Steps);
        Assert.Equal(Ramon, step.ApproverUserId);
        Assert.Equal(1, step.StepOrder);
    }

    [Fact]
    public void Nobody_approves_their_own_report()
    {
        var l1 = ScopeRules.Resolve([Dept(1, Maria, 1, Sales)], new ScopeSubject(Ramon, Sales), 1, Today);
        var l2 = ScopeRules.Resolve([Dept(2, Ramon, 2, Sales)], new ScopeSubject(Ramon, Sales), 2, Today);
        var plan = ApprovalRouting.Build(Ramon, true, 100m, l1, l2, new RoutingPolicy(false, 0m));
        Assert.False(plan.IsRouted);
        Assert.DoesNotContain(plan.Steps, s => s.ApproverUserId == Ramon);
    }

    [Fact]
    public void Missing_level1_blocks_routing()
    {
        var none = new ScopeResolution(1, null, "No Level 1 signatory covers this person's department.");
        var plan = ApprovalRouting.Build(Ana, false, 0m, none, none with { Level = 2 }, new RoutingPolicy(false, 0m));
        Assert.False(plan.IsRouted);
        Assert.Contains("Level 1", plan.Problem);
    }
}

public class RateRulesTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly RateSubject Ana = new(100, "G3", 10);

    private static RateAssignmentCandidate Assign(long id, long profile, RateTargetType type, long? user = null, string? grade = null, long? dept = null, DateOnly? from = null, DateOnly? to = null) =>
        new(id, profile, type, user, grade, dept, from ?? Jan1, to);

    [Fact]
    public void Most_specific_assignment_wins()
    {
        var assignments = new[]
        {
            Assign(1, 1, RateTargetType.Company),
            Assign(2, 2, RateTargetType.Department, dept: 10),
            Assign(3, 3, RateTargetType.Grade, grade: "g3"),
        };
        Assert.Equal(3, RateRules.ResolveAssignment(assignments, Ana, Jan1.AddDays(10))!.ProfileId);
        Assert.Equal(4, RateRules.ResolveAssignment(assignments.Append(Assign(4, 4, RateTargetType.Individual, user: 100)), Ana, Jan1.AddDays(10))!.ProfileId);
    }

    [Fact]
    public void Assignment_is_resolved_on_the_expense_date()
    {
        var assignments = new[]
        {
            Assign(1, 1, RateTargetType.Company),
            Assign(2, 2, RateTargetType.Individual, user: 100, from: new DateOnly(2026, 6, 1)),
        };
        Assert.Equal(1, RateRules.ResolveAssignment(assignments, Ana, new DateOnly(2026, 5, 31))!.ProfileId);
        Assert.Equal(2, RateRules.ResolveAssignment(assignments, Ana, new DateOnly(2026, 6, 1))!.ProfileId);
    }

    [Fact]
    public void Line_in_force_on_the_date_is_used()
    {
        var lines = new[]
        {
            new RateLineCandidate(1, 1, ExpenseCategory.Meals, "Lunch", RateType.FixedPerDay, 150m, false, Jan1, new DateOnly(2026, 6, 30)),
            new RateLineCandidate(2, 1, ExpenseCategory.Meals, "Lunch", RateType.FixedPerDay, 180m, false, new DateOnly(2026, 7, 1), null),
        };
        Assert.Equal(150m, RateRules.ResolveLine(lines, 1, ExpenseCategory.Meals, "lunch", new DateOnly(2026, 6, 30))!.Amount);
        Assert.Equal(180m, RateRules.ResolveLine(lines, 1, ExpenseCategory.Meals, "Lunch", new DateOnly(2026, 7, 1))!.Amount);
        Assert.Null(RateRules.ResolveLine(lines, 1, ExpenseCategory.Meals, "Dinner", new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void New_version_closes_the_open_one_the_day_before()
    {
        var open = new RateLineCandidate(7, 1, ExpenseCategory.Transportation, null, RateType.FixedPerDay, 100m, true, Jan1, null);
        var plan = RateRules.PlanVersion([open], new DateOnly(2026, 11, 1), null);
        Assert.True(plan.IsValid);
        Assert.Equal(7, plan.CloseLineId);
        Assert.Equal(new DateOnly(2026, 10, 31), plan.CloseOn);
    }

    [Fact]
    public void New_version_cannot_start_on_or_before_the_latest()
    {
        var latest = new RateLineCandidate(7, 1, ExpenseCategory.Transportation, null, RateType.FixedPerDay, 100m, true, new DateOnly(2026, 11, 1), null);
        Assert.False(RateRules.PlanVersion([latest], new DateOnly(2026, 11, 1), null).IsValid);
        Assert.False(RateRules.PlanVersion([latest], new DateOnly(2026, 10, 1), null).IsValid);
    }

    [Theory]
    [InlineData(0, false, null, true)]
    [InlineData(-1, false, null, false)]
    [InlineData(-1, true, null, false)]
    [InlineData(-1, true, "Board resolution 12", true)]
    public void Backdating_needs_override_and_reason(int days, bool overrideFlag, string? reason, bool ok)
    {
        var today = new DateOnly(2026, 10, 13);
        Assert.Equal(ok, RateRules.BackdateProblem(today.AddDays(days), today, overrideFlag, reason) is null);
    }
}

public class EntitlementAuthorityTests
{
    private static EntitlementActor Actor(long id, string? grade, long? dept, params UserRole[] roles) =>
        new(id, roles.Append(UserRole.Employee).ToHashSet(), grade, dept);

    [Fact]
    public void Only_a_system_administrator_sets_scope()
    {
        Assert.True(EntitlementAuthority.ForScope(Actor(9, null, null, UserRole.SystemAdministrator)).IsAllowed);
        Assert.False(EntitlementAuthority.ForScope(Actor(3, null, 10, UserRole.SignatoryLevel2)).IsAllowed);
        Assert.False(EntitlementAuthority.ForScope(Actor(1, null, 10, UserRole.SignatoryLevel1)).IsAllowed);
        Assert.False(EntitlementAuthority.ForScope(Actor(2, null, 10, UserRole.Finance)).IsAllowed);
    }

    [Theory]
    [InlineData(RateTargetType.Individual, 9L, null, null, false)]
    [InlineData(RateTargetType.Individual, 4L, null, null, true)]
    [InlineData(RateTargetType.Grade, null, "G5", null, true)]
    [InlineData(RateTargetType.Department, null, null, 10L, true)]
    [InlineData(RateTargetType.Company, null, null, null, true)]
    public void Administrator_sets_rates_except_one_naming_themselves(RateTargetType type, long? user, string? grade, long? dept, bool allowed)
    {
        var admin = Actor(9, "g5", 10, UserRole.SystemAdministrator);
        Assert.Equal(allowed, EntitlementAuthority.ForRates(admin, new RateTarget(type, user, grade, dept)).IsAllowed);
    }

    [Fact]
    public void Only_a_system_administrator_sets_rates()
    {
        var target = new RateTarget(RateTargetType.Individual, 100, null, null);
        Assert.False(EntitlementAuthority.ForRates(Actor(1, null, null, UserRole.SignatoryLevel1), target).IsAllowed);
        Assert.False(EntitlementAuthority.ForRates(Actor(3, null, null, UserRole.SignatoryLevel2), target).IsAllowed);
        Assert.False(EntitlementAuthority.ForRates(Actor(2, null, null, UserRole.Finance), target).IsAllowed);
    }
}
