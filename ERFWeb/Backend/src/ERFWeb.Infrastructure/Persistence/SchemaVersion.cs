namespace ERFWeb.Infrastructure.Persistence;

/// <summary>
/// The database script this build needs: <c>Database/NNN_*.sql</c> scripts record their number in
/// <c>schema_versions</c>, and /api/health reports "schema_outdated" (503) while the database is behind, so
/// a deploy made before its script was run rolls back instead of failing on the first query.
/// Raise it in the same change that adds a script the code depends on.
/// </summary>
public static class SchemaVersion
{
    public const int Required = 15;
}
