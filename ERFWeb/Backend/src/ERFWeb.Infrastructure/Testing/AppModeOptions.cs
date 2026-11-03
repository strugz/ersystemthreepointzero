namespace ERFWeb.Infrastructure.Testing;

/// <summary>
/// Whether this API is a test or a production install, bound from the optional <c>App</c> section of
/// appsettings.json. Anything other than <c>Test</c> (or a missing section) is production, so the
/// test-only deletes stay off unless a server's appsettings.json turns them on.
/// </summary>
public sealed class AppModeOptions
{
    public const string SectionName = "App";

    /// <summary><c>Test</c> or <c>Production</c>.</summary>
    public string Mode { get; set; } = "Production";

    public bool IsTest => string.Equals(Mode, "Test", StringComparison.OrdinalIgnoreCase);
}
