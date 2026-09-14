using System.Data;
using ERFWeb.Application.Common;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Services;

/// <summary>
/// Runs a unit of work in a serializable transaction, committing on success and
/// rolling back on any exception. Nested calls reuse the ambient transaction.
/// At the start of each transaction the acting user's id is published to PostgreSQL
/// as the session setting <c>app.current_user_id</c> (transaction-local), which the
/// <c>fn_record_history</c> trigger reads to stamp <c>record_history.changed_by</c>.
/// </summary>
public sealed class EfTransactionRunner(ErfDbContext db, ICurrentUser user) : ITransactionRunner
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (db.Database.CurrentTransaction is not null)
        {
            return await action(ct);
        }

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                await TagActorAsync(ct);
                var result = await action(ct);
                await tx.CommitAsync(ct);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await ExecuteAsync(async token =>
        {
            await action(token);
            return true;
        }, ct);
    }

    /// <summary>
    /// <c>set_config(..., is_local = true)</c> scopes the value to the current transaction, so it
    /// cannot leak to another request on a pooled connection. The id is passed as a parameter.
    /// </summary>
    private Task TagActorAsync(CancellationToken ct)
    {
        var actor = user.IsAuthenticated ? user.UserId.ToString() : string.Empty;
        return db.Database.ExecuteSqlAsync($"SELECT set_config('app.current_user_id', {actor}, true)", ct);
    }
}
