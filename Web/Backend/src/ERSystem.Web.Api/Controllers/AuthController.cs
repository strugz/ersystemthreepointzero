using System.Security.Claims;
using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERSystem.Web.Api.Controllers;

using WebAuthenticationService = ERSystem.Web.Application.Features.Authentication.IAuthenticationService;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(WebAuthenticationService authentication, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("antiforgery")]
    [AllowAnonymous]
    public ActionResult<object> Antiforgery([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthenticatedUserDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await authentication.AuthenticateAsync(request, cancellationToken);
        if (user is null) return Unauthorized(new ProblemDetails { Status = 401, Title = "Invalid username or password" });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("full_name", user.FullName),
            new("user_level", user.UserLevel)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
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
    public ActionResult<AuthenticatedUserDto> Me()
    {
        var fullName = User.FindFirstValue("full_name") ?? currentUser.Username;
        var userLevel = User.FindFirstValue("user_level") ?? string.Empty;
        return Ok(new AuthenticatedUserDto(currentUser.UserId, currentUser.Username, fullName, userLevel, currentUser.Roles.ToArray()));
    }
}
