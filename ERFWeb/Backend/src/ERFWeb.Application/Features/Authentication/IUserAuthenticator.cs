namespace ERFWeb.Application.Features.Authentication;

/// <summary>Signs users in (with lockout) and lets them change their own password.</summary>
public interface IUserAuthenticator
{
    /// <summary>
    /// Checks the credentials and the sign-in lockout. A locked username is refused without checking the
    /// password; each failure counts towards the lockout, a success clears it.
    /// </summary>
    Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Changes the signed-in user's own password after checking the current one, and clears "must change
    /// password". Throws a validation error for a wrong current password or an unacceptable new one.
    /// </summary>
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct = default);
}
