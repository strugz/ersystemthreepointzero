using System.Security.Claims;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERFWeb.Api.Security;

namespace ERFWeb.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IUserAuthenticator authenticator,
    IAntiforgery antiforgery,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Issues an antiforgery token (and sets its cookie) for the SPA to send on mutations.</summary>
    [HttpGet("antiforgery")]
    [AllowAnonymous]
    public IActionResult GetAntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedUserDto>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await authenticator.ValidateAsync(request.Username, request.Password, ct);
        if (user is null)
        {
            var problem = new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Invalid username or password." };
            problem.Extensions["code"] = "invalid_credentials";
            return new ObjectResult(problem) { StatusCode = StatusCodes.Status401Unauthorized };
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(HttpCurrentUser.FullNameClaim, user.FullName),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
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
    public ActionResult<AuthenticatedUserDto> Me() =>
        Ok(new AuthenticatedUserDto(
            currentUser.UserId,
            currentUser.Username,
            currentUser.FullName,
            currentUser.Roles.Select(r => r.ToString()).ToList()));
}
