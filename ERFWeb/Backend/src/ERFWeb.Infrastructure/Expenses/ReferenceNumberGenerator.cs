using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Yearly counter in <c>reference_series</c>, one row per <c>PREFIX-YYYY</c>. The row is read and
/// incremented inside the caller's serializable transaction: two filings racing for the same
/// number make PostgreSQL fail one of them with a serialization error, which the transaction
/// runner's retry strategy replays, so the series stays gapless and unique without a stored
/// procedure or a database sequence. The unique index on <c>expense_reports.reference_no</c> is
/// the last line of defence.
/// </summary>
public sealed class ReferenceNumberGenerator(ErfDbContext db, IOptions<ReferenceNumberOptions> options) : IReferenceNumberGenerator
{
    private readonly ReferenceNumberOptions _options = options.Value;

    public async Task<string> NextAsync(DateTimeOffset filedAt, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Reference numbers must be issued inside the workflow transaction.");
        }

        var year = filedAt.ToOffset(TimeSpan.FromHours(_options.UtcOffsetHours)).Year;
        var key = ReferenceNumberFormat.SeriesKey(_options.Prefix, year);

        var series = await db.ReferenceSeries.FirstOrDefaultAsync(s => s.SeriesKey == key, ct);
        if (series is null)
        {
            series = new ReferenceSeries { SeriesKey = key, NextValue = 1, UpdatedAt = filedAt };
            db.ReferenceSeries.Add(series);
        }

        var sequence = series.NextValue;
        series.NextValue = sequence + 1;
        series.UpdatedAt = filedAt;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Two first-of-the-year filings inserted the same series row at once; the loser retries.
            throw new ConflictException("Another report was being filed at the same moment. Please try again.");
        }

        return ReferenceNumberFormat.Format(_options.Prefix, year, sequence, _options.Digits);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };
}
