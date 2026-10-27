using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using ERFWeb.Domain;
using ERFWeb.Infrastructure.Crm;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// The rules for linking an expense line to CRM activities (several when one visit served several
/// instruments). The CRM is asked only about links that are new on the line (a line keeps saving while the
/// CRM is down), outside the transaction; the claimed-elsewhere check runs inside it. One activity may fill
/// several lines of one report, but only one report of the employee's at a time; a Rejected report releases it.
/// </summary>
public sealed class CrmActivityClaimCheck(ErfDbContext db, ICurrentUser user, ICrmActivityDirectory crm)
{
    private const string Field = "crmActivityIds";

    /// <summary>The most activities one line may link; one client visit never comes near it.</summary>
    public const int MaxPerLine = 10;

    /// <summary>The activity ID in the one spelling ERFWeb stores (the CRM accepts ITI-/SR- in any case).</summary>
    public static string? Normalize(string? activityId) =>
        string.IsNullOrWhiteSpace(activityId) ? null : activityId.Trim().ToUpperInvariant();

    /// <summary>The line's activity IDs as stored: normalized, blanks and repeats dropped, in the order picked.</summary>
    public static List<string> NormalizeAll(IEnumerable<string?>? activityIds) =>
        (activityIds ?? []).Select(Normalize).OfType<string>().Distinct().ToList();

    /// <summary>
    /// Confirms with the CRM that each newly linked activity is the employee's own, Completed, and covers the
    /// line's date. Does nothing when the line has no links or keeps the ones it had.
    /// </summary>
    public async Task VerifyAsync(IReadOnlyList<string> activityIds, IReadOnlyCollection<string> currentActivityIds, DateOnly transactionDate, CancellationToken ct)
    {
        if (activityIds.Count > MaxPerLine)
        {
            throw new ValidationException(Field, $"A line can link at most {MaxPerLine} CRM activities.");
        }

        var added = activityIds.Where(id => !currentActivityIds.Contains(id)).ToList();
        if (added.Count == 0)
        {
            return;
        }

        if (added.Exists(id => id.Length > CrmActivityDirectory.MaxActivityIdLength))
        {
            throw new ValidationException(Field, "That is not a CRM activity ID.");
        }

        var employeeId = await db.Users.AsNoTracking()
            .Where(u => u.Id == user.UserId)
            .Select(u => u.CrmEmployeeId)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            throw new ValidationException(Field, "Your account is not linked to the CRM. Ask the system administrator to link it, or type the line instead.");
        }

        foreach (var id in added)
        {
            var activity = await crm.GetAsync(id, ct)
                ?? throw new ValidationException(Field, $"The CRM has no activity {id}.");
            if (!activity.IsCompleted)
            {
                throw new ValidationException(Field, $"{id} is not completed in the CRM yet. Only completed activities can be claimed.");
            }

            if (!activity.Involves(employeeId))
            {
                throw new ValidationException(Field, $"{id} is not one of your CRM activities.");
            }

            // Multi-day work is one CRM record, so any day from its start to its completion is allowed.
            var endDay = activity.EndAt is { } end ? DateOnly.FromDateTime(end.DateTime) : activity.ActivityDate;
            var lastDay = endDay > activity.ActivityDate ? endDay : activity.ActivityDate;
            if (transactionDate < activity.ActivityDate || transactionDate > lastDay)
            {
                throw new ValidationException("transactionDate", $"The date must fall on {id}'s CRM date ({activity.ActivityDate:yyyy-MM-dd}{(lastDay > activity.ActivityDate ? $" to {lastDay:yyyy-MM-dd}" : string.Empty)}).");
            }
        }
    }

    /// <summary>Refuses an activity that is already on another of the employee's reports that was not Rejected.</summary>
    public async Task EnsureNotClaimedElsewhereAsync(ExpenseReport report, IReadOnlyList<string> activityIds, CancellationToken ct)
    {
        if (activityIds.Count == 0)
        {
            return;
        }

        var claimedIn = await (
            from l in db.ExpenseLines.AsNoTracking()
            join r in db.ExpenseReports.AsNoTracking() on l.ReportId equals r.Id
            where l.CrmActivityIds.Any(a => activityIds.Contains(a)) && l.IsActive
                && r.EmployeeUserId == report.EmployeeUserId && r.Id != report.Id && r.Status != ReportStatus.Rejected
            select new { r.ReferenceNo, l.CrmActivityIds })
            .FirstOrDefaultAsync(ct);
        if (claimedIn is not null)
        {
            var id = activityIds.First(claimedIn.CrmActivityIds.Contains);
            throw new ValidationException(Field, claimedIn.ReferenceNo is { } reference
                ? $"{id} is already claimed on {reference}."
                : $"{id} is already claimed on another of your reports that has not been filed yet.");
        }
    }
}
