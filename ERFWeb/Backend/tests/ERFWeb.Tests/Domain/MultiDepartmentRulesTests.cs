using ERFWeb.Domain;
using ERFWeb.Domain.Scope;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class MultiDepartmentRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);
    private const long Sales = 10;      // main department
    private const long Service = 20;    // other department
    private const long Marketing = 30;  // not one of the employee's
    private const long SalesL1 = 1;
    private const long ServiceL1 = 2;
    private const long NamedL1 = 3;
    private const long Ana = 100;
    private static readonly long[] Others = [Service];
    private static readonly MultiDepartmentSettings DepartmentWins = MultiDepartmentSettings.Default;
    private static readonly MultiDepartmentSettings ByNameWins = DepartmentWins with { ByName = MultiDepartmentByName.ByNameWins };

    private static ScopeCandidate Dept(long id, long holder, long dept) =>
        new(id, holder, 1, ScopeAssignmentType.Department, dept, new HashSet<long>(), Today.AddDays(-30), null, true, null);

    private static ScopeCandidate Named(long id, long holder, long member) =>
        new(id, holder, 1, ScopeAssignmentType.Individual, null, new HashSet<long> { member }, Today.AddDays(-30), null, true, null);

    [Fact]
    public void Allowed_IsTheMainDepartmentOnly_WhileSwitchedOff()
    {
        Assert.Equal([Sales], MultiDepartmentRules.Allowed(Sales, enabled: false, Others));
        Assert.Equal([Sales, Service], MultiDepartmentRules.Allowed(Sales, enabled: true, Others));
    }

    [Fact]
    public void NormalizeOthers_DropsTheMainDepartmentAndDuplicates() =>
        Assert.Equal([Service, Marketing], MultiDepartmentRules.NormalizeOthers([Marketing, Sales, Service, Marketing], Sales));

    [Fact]
    public void StoredChoice_IsNullForTheMainDepartment()
    {
        Assert.Null(MultiDepartmentRules.StoredChoice(Sales, Sales));
        Assert.Null(MultiDepartmentRules.StoredChoice(null, Sales));
        Assert.Equal(Service, MultiDepartmentRules.StoredChoice(Service, Sales));
    }

    [Fact]
    public void FilingProblem_NoneForTheMainDepartmentOrAnAllowedOne()
    {
        Assert.Null(MultiDepartmentRules.FilingProblem(null, Sales, false, Others, null, "Sales"));
        Assert.Null(MultiDepartmentRules.FilingProblem(Service, Sales, true, Others, "Service", "Sales"));
    }

    [Fact]
    public void FilingProblem_AfterTheSwitchIsTurnedOff_AsksToChangeToTheMainDepartment()
    {
        var problem = MultiDepartmentRules.FilingProblem(Service, Sales, false, Others, "Service", "Sales");
        Assert.NotNull(problem);
        Assert.Contains("Sales", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void FilingProblem_ForADepartmentNoLongerListed()
    {
        var problem = MultiDepartmentRules.FilingProblem(Marketing, Sales, true, Others, "Marketing", "Sales");
        Assert.NotNull(problem);
        Assert.Contains("Marketing", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void Report_ChargedToTheOtherDepartment_RoutesToItsSignatory()
    {
        var assignments = new[] { Dept(1, SalesL1, Sales), Dept(2, ServiceL1, Service) };
        var subject = new ScopeSubject(Ana, MultiDepartmentRules.Effective(Service, Sales), MultiDepartmentRules.ByNameApplies(DepartmentWins, Service, Sales));
        Assert.Equal(ServiceL1, ScopeRules.Resolve(assignments, subject, 1, Today).HolderUserId);
    }

    [Fact]
    public void ByName_DepartmentWins_AppliesOnlyToMainDepartmentReports()
    {
        var assignments = new[] { Dept(1, SalesL1, Sales), Dept(2, ServiceL1, Service), Named(3, NamedL1, Ana) };

        var other = new ScopeSubject(Ana, Service, MultiDepartmentRules.ByNameApplies(DepartmentWins, Service, Sales));
        var main = new ScopeSubject(Ana, Sales, MultiDepartmentRules.ByNameApplies(DepartmentWins, null, Sales));

        Assert.Equal(ServiceL1, ScopeRules.Resolve(assignments, other, 1, Today).HolderUserId);
        Assert.Equal(NamedL1, ScopeRules.Resolve(assignments, main, 1, Today).HolderUserId);
    }

    [Fact]
    public void ByName_ByNameWins_SignsEveryReport()
    {
        var assignments = new[] { Dept(1, SalesL1, Sales), Dept(2, ServiceL1, Service), Named(3, NamedL1, Ana) };
        var other = new ScopeSubject(Ana, Service, MultiDepartmentRules.ByNameApplies(ByNameWins, Service, Sales));
        Assert.Equal(NamedL1, ScopeRules.Resolve(assignments, other, 1, Today).HolderUserId);
    }

    [Fact]
    public void RateDepartment_FollowsTheSetting()
    {
        Assert.Equal(Sales, MultiDepartmentRules.RateDepartment(DepartmentWins, Service, Sales));
        Assert.Equal(Service, MultiDepartmentRules.RateDepartment(DepartmentWins with { Rates = MultiDepartmentRates.ReportDepartment }, Service, Sales));
        Assert.Equal(Sales, MultiDepartmentRules.RateDepartment(DepartmentWins with { Rates = MultiDepartmentRates.ReportDepartment }, null, Sales));
    }
}
