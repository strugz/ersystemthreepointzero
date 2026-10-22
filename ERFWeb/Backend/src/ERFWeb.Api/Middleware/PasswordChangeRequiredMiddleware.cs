using ERFWeb.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Middleware;

/// <summary>
/// While a signed-in user still has an administrator-set password, every API call except the auth endpoints
/// (sign out, who am I, change password) and health is refused with 403 <c>password_change_required</c>.
/// The SPA sends them to the change-password page; this makes the rule hold for any client.
/// </summary>
public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        if (context.User.HasClaim(HttpCurrentUser.MustChangePasswordClaim, "true")
            && !context.Request.Path.StartsWithSegments("/api/auth")
            && !context.Request.Path.StartsWithSegments("/api/health"))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Please change your password before continuing.",
                Instance = context.Request.Path,
            };
            problem.Extensions["code"] = "password_change_required";
            if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
            {
                problem.Extensions["correlationId"] = correlationId;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
            return;
        }

        await next(context);
    }
}
