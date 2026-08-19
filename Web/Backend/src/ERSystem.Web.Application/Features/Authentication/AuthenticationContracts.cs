namespace ERSystem.Web.Application.Features.Authentication;

public sealed record LoginRequest(string Username, string Password);
public sealed record AuthenticatedUserDto(int UserId, string Username, string FullName, string UserLevel, IReadOnlyList<string> Roles);

public interface IAuthenticationService
{
    Task<AuthenticatedUserDto?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken);
}
