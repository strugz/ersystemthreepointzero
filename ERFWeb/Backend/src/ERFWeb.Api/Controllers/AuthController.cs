using System.Security.Claims;
using ERFWeb.Api.Middleware;
using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Authentication;
using ERFWeb.Domain.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERFWeb.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IUserAuthenticator authenticator,
    IAntiforgery antiforgery,
    ICurrentUser currentUser,
    IClock clock) : ControllerBase
{
    /// <summary>Issues an antiforgery token (and sets its cookie) for the SPA to send on mutations.</summary>
    [HttpGet("antiforgery")]
    [AllowAnonymous]
    public IActionResult GetAntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    /// <summary>
    /// Signs in. 401 for wrong credentials; 423 <c>account_locked</c> after five failures in a row (for 15
    /// minutes, known usernames or not); 429 when this address sends too many attempts.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.SignIn)]
    [ProducesResponseType<AuthenticatedUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authenticator.LoginAsync(request.Username, request.Password, ct);
        switch (result.Outcome)
        {
            case LoginOutcome.Success when result.User is { } user:
                await SignInAsync(user);
                return Ok(user);
            case LoginOutcome.LockedOut when result.LockedUntil is { } until:
                var minutes = LoginLockoutRules.MinutesLeft(until, clock.UtcNow);
                return Failure(StatusCodes.Status423Locked, "account_locked",
                    $"Too many failed sign-in attempts. Try again in {minutes} minute{(minutes == 1 ? string.Empty : "s")}, or ask an administrator to reset your password.");
            default:
                return Failure(StatusCodes.Status401Unauthorized, "invalid_credentials", "Invalid username or password.");
        }
    }

    /// <summary>
    /// Changes the signed-in user's own password. Also the only way out of "must change password": the
    /// session is re-issued without it.
    /// </summary>
    [HttpPost("password")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.SignIn)]
    [ProducesResponseType<AuthenticatedUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await authenticator.ChangePasswordAsync(currentUser.UserId, request, ct);
        var user = CurrentUserDto() with { MustChangePassword = false };
        await SignInAsync(user);
        return Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<AuthenticatedUserDto> Me() => Ok(CurrentUserDto());

    private AuthenticatedUserDto CurrentUserDto() =>
        new(
            currentUser.UserId,
            currentUser.Username,
            currentUser.FullName,
            currentUser.Roles.Select(r => r.ToString()).ToList(),
            User.HasClaim(HttpCurrentUser.MustChangePasswordClaim, "true"));

    private Task SignInAsync(AuthenticatedUserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(HttpCurrentUser.FullNameClaim, user.FullName),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        if (user.MustChangePassword)
        {
            claims.Add(new Claim(HttpCurrentUser.MustChangePasswordClaim, "true"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    /// <summary>Same ProblemDetails shape the exception middleware produces, so the SPA reads every error alike.</summary>
    private ObjectResult Failure(int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Instance = HttpContext.Request.Path };
        problem.Extensions["code"] = code;
        if (HttpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
