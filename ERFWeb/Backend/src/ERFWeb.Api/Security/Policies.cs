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
    public const string Countersigner = "Countersigner";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(SystemAdministrator, p => p.RequireRole(nameof(UserRole.SystemAdministrator)));
        options.AddPolicy(Finance, p => p.RequireRole(nameof(UserRole.Finance)));

        // Spec 9.10: Finance holds read-only oversight of the administrator's audit trail.
        options.AddPolicy(AuditReader, p => p.RequireRole(nameof(UserRole.SystemAdministrator), nameof(UserRole.Finance)));

        // Spec 9.8: the Business Owner countersigns; a second System Administrator may do so only
        // while no Business Owner exists (the service enforces that bootstrap condition).
        options.AddPolicy(Countersigner, p => p.RequireRole(nameof(UserRole.BusinessOwner), nameof(UserRole.SystemAdministrator)));
    }
}
