using System.Reflection;
using ERFWeb.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Anonymous health check for monitoring, nginx and the deploy script: the running version and whether the
/// database answers and has every script this build needs. 200 when healthy, 503 otherwise.
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController(IDatabaseHealthCheck database) : ControllerBase
{
    /// <summary>"0.1.0+a1b2c3d": the assembly version plus the commit it was built from, when known.</summary>
    public static readonly string Version = ReadVersion();

    [HttpGet]
    [ProducesResponseType<HealthDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthDto>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var db = await database.CheckAsync(ct);
        var ok = db.State == DatabaseState.Ok;
        var state = db.State switch
        {
            DatabaseState.Ok => "ok",
            DatabaseState.SchemaOutdated => "schema_outdated",
            _ => "unreachable",
        };
        var health = new HealthDto(ok ? "ok" : "degraded", "ERFWeb.Api", Version, state, db.SchemaVersion, db.RequiredSchemaVersion);
        return ok ? Ok(health) : StatusCode(StatusCodes.Status503ServiceUnavailable, health);
    }

    private static string ReadVersion()
    {
        var informational = typeof(HealthController).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(informational))
        {
            return "unknown";
        }

        // The SDK appends the full commit hash ("0.1.0+<40 hex>"); seven characters identify it.
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 && informational.Length > plus + 8 ? informational[..(plus + 8)] : informational;
    }
}

/// <param name="Database">"ok", "unreachable", or "schema_outdated" (a Database script has not been run).</param>
/// <param name="SchemaVersion">Highest Database script recorded; null when unreachable.</param>
/// <param name="RequiredSchemaVersion">The Database script this build needs.</param>
public sealed record HealthDto(string Status, string Service, string Version, string Database, int? SchemaVersion, int RequiredSchemaVersion);
