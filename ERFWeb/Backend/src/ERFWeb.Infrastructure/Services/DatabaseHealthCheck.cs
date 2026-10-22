using System.Data.Common;
using ERFWeb.Application.Common;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Services;

/// <summary>
/// Opens one connection with a short timeout and reads the schema version, so monitoring and the deploy
/// script get a quick answer. It deliberately bypasses EF's retry strategy: a health check polled every
/// minute must not retry with backoff and log a stack trace per attempt while the database is down.
/// </summary>
public sealed class DatabaseHealthCheck(ErfDbContext db) : IDatabaseHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<DatabaseHealth> CheckAsync(CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);
        var connection = db.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync(timeout.Token);
            var version = await ReadSchemaVersionAsync(connection, timeout.Token);
            var state = version >= SchemaVersion.Required ? DatabaseState.Ok : DatabaseState.SchemaOutdated;
            return new DatabaseHealth(state, version, SchemaVersion.Required);
        }
        catch (Exception ex) when (ex is DbException or TimeoutException
                                       || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return new DatabaseHealth(DatabaseState.Unreachable, null, SchemaVersion.Required);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    /// <summary>0 before script 015 created <c>schema_versions</c>.</summary>
    private static async Task<int> ReadSchemaVersionAsync(DbConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.schema_versions') IS NOT NULL";
        if (await command.ExecuteScalarAsync(ct) is not true)
        {
            return 0;
        }

        command.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_versions";
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }
}
