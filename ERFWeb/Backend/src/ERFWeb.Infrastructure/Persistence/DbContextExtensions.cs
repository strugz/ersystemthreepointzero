using ERFWeb.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Persistence;

/// <summary>Shared EF helpers: concurrency-aware save and xmin row-version plumbing.</summary>
public static class DbContextExtensions
{
    public const string RowVersionProperty = "xmin";

    /// <summary>
    /// Saves pending changes, translating a stale <c>xmin</c> into a 409-mapped
    /// <see cref="ConflictException"/> so the caller's transaction rolls back cleanly.
    /// </summary>
    public static async Task SaveWithConcurrencyAsync(this ErfDbContext db, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record was changed by someone else. Reload and try again.");
        }
    }

    /// <summary>Pins the row version the client last saw so the UPDATE is rejected if the row moved on.</summary>
    public static void SetRowVersion<TEntity>(this ErfDbContext db, TEntity entity, uint rowVersion)
        where TEntity : class =>
        db.Entry(entity).Property<uint>(RowVersionProperty).OriginalValue = rowVersion;

    /// <summary>Reads the current row version of a tracked entity after a save.</summary>
    public static uint GetRowVersion<TEntity>(this ErfDbContext db, TEntity entity)
        where TEntity : class =>
        db.Entry(entity).Property<uint>(RowVersionProperty).CurrentValue;
}
