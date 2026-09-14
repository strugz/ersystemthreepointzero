namespace ERFWeb.Application.Features.Authentication;

/// <summary>Validates credentials and returns the authenticated user, or null if invalid.</summary>
public interface IUserAuthenticator
{
    Task<AuthenticatedUserDto?> ValidateAsync(string username, string password, CancellationToken ct = default);
}
