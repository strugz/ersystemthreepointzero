using System.Security.Claims;
using ERFWeb.Application.Common;
using ERFWeb.Domain;

namespace ERFWeb.Api.Security;

/// <summary>Resolves the current user from the request's authenticated claims.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string FullNameClaim = "full_name";

    /// <summary>Present ("true") while an administrator-set password has not been changed by its owner.</summary>
    public const string MustChangePasswordClaim = "must_change_password";

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public long UserId =>
        long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public string Username => Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string FullName => Principal?.FindFirstValue(FullNameClaim) ?? string.Empty;

    public IReadOnlyCollection<UserRole> Roles
    {
        get
        {
            var principal = Principal;
            if (principal is null)
            {
                return [];
            }

            var roles = new List<UserRole>();
            foreach (var claim in principal.FindAll(ClaimTypes.Role))
            {
                if (Enum.TryParse<UserRole>(claim.Value, out var role))
                {
                    roles.Add(role);
                }
            }

            return roles;
        }
    }

    public bool IsInRole(UserRole role) => Roles.Contains(role);
}
