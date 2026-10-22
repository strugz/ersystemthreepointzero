namespace ERFWeb.Api.Security;

/// <summary>Named rate-limit policies (Program.cs registers them; endpoints opt in with [EnableRateLimiting]).</summary>
public static class RateLimitPolicies
{
    /// <summary>Sign-in and password change, per client address: slows password guessing across usernames.</summary>
    public const string SignIn = "sign-in";
}
