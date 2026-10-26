using System.Reflection;
using System.Text.RegularExpressions;
using ERFWeb.Api.Security;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace ERFWeb.Tests.Architecture;

/// <summary>
/// Guardrails checked on the code itself, without a database: who each endpoint lets in (the API, not the
/// SPA, enforces the rules) and that nothing in the server ever edits an audit row (9.9).
/// </summary>
public class GuardrailCodeTests
{
    private static readonly Assembly Api = typeof(Policies).Assembly;

    [Theory]
    [InlineData(Policies.RateManager)]
    [InlineData(Policies.ScopeManager)]
    public void G9_13_G9_15_Only_the_system_administrator_changes_scope_and_rates(string policy)
    {
        Assert.Equal([nameof(UserRole.SystemAdministrator)], RolesOf(policy));
    }

    [Theory]
    [InlineData("RatesController", Policies.RateManager)]
    [InlineData("ScopeController", Policies.ScopeManager)]
    [InlineData("AdminUsersController", Policies.SystemAdministrator)]
    public void Every_change_to_roles_scope_and_rates_needs_the_administrator_policy(string controller, string policy)
    {
        var type = Api.GetTypes().Single(t => t.Name == controller);
        var mutations = Actions(type).Where(m => !IsGet(m)).ToList();

        Assert.NotEmpty(mutations);
        Assert.All(mutations, m => Assert.Contains(policy, PoliciesOn(type, m)));
    }

    [Fact]
    public void G9_10_Finance_and_the_administrator_read_the_audit_trail()
    {
        Assert.Equal([nameof(UserRole.Finance), nameof(UserRole.SystemAdministrator)], RolesOf(Policies.AuditReader).Order());
    }

    [Fact]
    public void G9_04_The_finance_service_has_no_reject()
    {
        Assert.DoesNotContain(typeof(IFinanceService).GetMethods(), m => m.Name.Contains("Reject", StringComparison.Ordinal));
    }

    [Fact]
    public void G9_09_The_audit_endpoint_only_reads()
    {
        var audit = Api.GetTypes().Single(t => t.Name == "AdminAuditController");
        Assert.All(Actions(audit), m => Assert.True(IsGet(m), $"{m.Name} must be a GET"));
    }

    [Fact]
    public void G9_09_No_server_code_updates_or_deletes_audit_rows()
    {
        var src = FindSource();
        var offenders = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => Regex.IsMatch(File.ReadAllText(f),
                @"WorkflowAudits\b[^;]*?\.(Remove|RemoveRange|Update|UpdateRange|ExecuteDelete\w*|ExecuteUpdate\w*)\s*\(|(UPDATE|DELETE\s+FROM|TRUNCATE)\s+workflow_audit",
                RegexOptions.IgnoreCase | RegexOptions.Singleline))
            .Select(Path.GetFileName)
            .ToList();

        Assert.Empty(offenders);
    }

    private static string[] RolesOf(string policy)
    {
        var options = new AuthorizationOptions();
        Policies.Register(options);
        return options.GetPolicy(policy)!.Requirements.OfType<RolesAuthorizationRequirement>().Single().AllowedRoles.ToArray();
    }

    private static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

    private static bool IsGet(MethodInfo action) =>
        action.GetCustomAttributes<HttpMethodAttribute>().All(a => a.HttpMethods.All(h => h == "GET"));

    private static IEnumerable<string?> PoliciesOn(Type controller, MethodInfo action) =>
        action.GetCustomAttributes<AuthorizeAttribute>().Concat(controller.GetCustomAttributes<AuthorizeAttribute>()).Select(a => a.Policy);

    /// <summary>ERFWeb/Backend/src, found by walking up from the test output folder.</summary>
    private static string FindSource()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "ERFWeb.Infrastructure");
            if (Directory.Exists(candidate))
            {
                return Path.Combine(dir.FullName, "src");
            }
        }

        throw new DirectoryNotFoundException("ERFWeb/Backend/src was not found above the test output folder.");
    }
}
