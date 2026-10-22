using System.Data.Common;
using ERFWeb.Application.Common;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Services;

/// <summary>
/// Opens one connection with a short timeout, so monitoring gets a quick yes or no. It deliberately
/// bypasses EF's retry strategy: a health check polled every minute must not retry with backoff and log a
/// stack trace per attempt while the database is down.
/// </summary>
public sealed class DatabaseHealthCheck(ErfDbContext db) : IDatabaseHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<bool> CanConnectAsync(CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);
        var connection = db.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync(timeout.Token);
            return true;
        }
        catch (Exception ex) when (ex is DbException or TimeoutException
                                       || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return false;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
