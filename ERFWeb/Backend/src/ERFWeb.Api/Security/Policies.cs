using ERFWeb.Domain;
using Microsoft.AspNetCore.Authorization;

namespace ERFWeb.Api.Security;

/// <summary>
/// Named authorization policies. Role claims carry the enum names of the caller's Active grants
/// (plus Employee), so a policy is a role requirement on those names. The API is authoritative;
/// the SPA's role checks are presentation only.
/// </summary>
public static class Policies
{
    public const string SystemAdministrator = "SystemAdministrator";
    public const string Finance = "Finance";
    public const string AuditReader = "AuditReader";
    public const string EntitlementReader = "EntitlementReader";
    public const string ScopeManager = "ScopeManager";
    public const string RateManager = "RateManager";
    public const string Approver = "Approver";
    public const string ExpenseVerifier = "ExpenseVerifier";
    public const string SmsSender = "SmsSender";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(SystemAdministrator, p => p.RequireRole(nameof(UserRole.SystemAdministrator)));
        options.AddPolicy(Finance, p => p.RequireRole(nameof(UserRole.Finance)));

        // Spec 9.10: Finance holds read-only oversight of the administrator's audit trail.
        options.AddPolicy(AuditReader, p => p.RequireRole(nameof(UserRole.SystemAdministrator), nameof(UserRole.Finance)));

        // Flow 8: signatories and Finance read scope and rates; the System Administrator keeps both
        // as administration settings and is the only one who changes them.
        options.AddPolicy(EntitlementReader, p => p.RequireRole(
            nameof(UserRole.SignatoryLevel1), nameof(UserRole.SignatoryLevel2),
            nameof(UserRole.Finance), nameof(UserRole.SystemAdministrator)));
        options.AddPolicy(ScopeManager, p => p.RequireRole(nameof(UserRole.SystemAdministrator)));
        options.AddPolicy(RateManager, p => p.RequireRole(nameof(UserRole.SystemAdministrator)));

        // The inbox is for signatories; the service checks that the step is theirs and it is their turn.
        options.AddPolicy(Approver, p => p.RequireRole(nameof(UserRole.SignatoryLevel1), nameof(UserRole.SignatoryLevel2)));

        // Flow 6: the verifier pool; the service checks 9.16 (not your own report, not one you approved).
        options.AddPolicy(ExpenseVerifier, p => p.RequireRole(nameof(UserRole.ExpenseVerifier)));

        // Spec permission matrix, "Send follow-up email / SMS": signatories, Finance, and the Expense Verifier.
        options.AddPolicy(SmsSender, p => p.RequireRole(
            nameof(UserRole.SignatoryLevel1), nameof(UserRole.SignatoryLevel2),
            nameof(UserRole.Finance), nameof(UserRole.ExpenseVerifier)));
    }
}
