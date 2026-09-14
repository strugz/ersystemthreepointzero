namespace ERFWeb.Application.Features.Authentication;

/// <summary>Credentials supplied at login.</summary>
public sealed record LoginRequest(string Username, string Password);

/// <summary>The authenticated user projection returned to the SPA.</summary>
public sealed record AuthenticatedUserDto(
    long UserId,
    string Username,
    string FullName,
    IReadOnlyList<string> Roles);
