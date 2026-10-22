namespace ERFWeb.Application.Features.Authentication;

/// <summary>Credentials supplied at login.</summary>
public sealed record LoginRequest(string Username, string Password);

/// <summary>The authenticated user projection returned to the SPA.</summary>
/// <param name="MustChangePassword">
/// The password was set by an administrator (new account or reset): the SPA sends the user to change it, and
/// the API refuses everything else until they do.
/// </param>
public sealed record AuthenticatedUserDto(
    long UserId,
    string Username,
    string FullName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword = false);

public enum LoginOutcome
{
    Success,
    InvalidCredentials,

    /// <summary>Too many failed attempts for this username; see <see cref="LoginResult.LockedUntil"/>.</summary>
    LockedOut,
}

public sealed record LoginResult(LoginOutcome Outcome, AuthenticatedUserDto? User = null, DateTimeOffset? LockedUntil = null);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

/// <summary>Audit events for sign-in security. Never the password, only who and when.</summary>
public static class AuthAuditEvents
{
    public const string LockedOut = "Auth.LockedOut";
    public const string PasswordChanged = "Auth.PasswordChanged";
}
