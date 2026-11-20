using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// <see cref="DuplicateMealRules"/> against the database: the employee's active Meals lines on the same dates, on this
/// report and their other reports that are not Rejected.
/// </summary>
public sealed class DuplicateMealCheck(ErfDbContext db)
{
    /// <summary>Why the line's meals cannot be claimed (already claimed that day), or null. <paramref name="lineId"/> is the line being edited.</summary>
    public async Task<string?> LineProblemAsync(ExpenseReport report, long? lineId, DateOnly date, IReadOnlyList<string> mealTypes, CancellationToken ct)
    {
        if (mealTypes.Count == 0)
        {
            return null;
        }

        var claimed = await ClaimedAsync(report.EmployeeUserId, [date], ct);
        return DuplicateMealRules.Problem(date, mealTypes, claimed.Where(c => c.LineId != lineId).Select(c => c.ToClaimed(report.Id)));
    }

    /// <summary>One reason per meal claimed twice by the report's lines (filing checks again, drafts included).</summary>
    public async Task<IReadOnlyList<string>> ReportProblemsAsync(Guid reportId, long employeeUserId, CancellationToken ct)
    {
        var dates = await db.ExpenseLines.AsNoTracking()
            .Where(l => l.ReportId == reportId && l.IsActive && l.Category == ExpenseCategory.Meals)
            .Select(l => l.TransactionDate)
            .Distinct()
            .ToListAsync(ct);
        if (dates.Count == 0)
        {
            return [];
        }

        var claimed = await ClaimedAsync(employeeUserId, dates, ct);
        return claimed
            .Where(c => c.ReportId == reportId)
            .OrderBy(c => c.Date).ThenBy(c => c.LineId)
            .Select(own => DuplicateMealRules.Problem(own.Date, own.MealTypes,
                claimed.Where(c => c.Date == own.Date && c.LineId != own.LineId).Select(c => c.ToClaimed(reportId))))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<List<MealLine>> ClaimedAsync(long employeeUserId, List<DateOnly> dates, CancellationToken ct)
    {
        var rows = await (
            from l in db.ExpenseLines.AsNoTracking()
            join r in db.ExpenseReports.AsNoTracking() on l.ReportId equals r.Id
            where l.IsActive && l.Category == ExpenseCategory.Meals && dates.Contains(l.TransactionDate)
                && r.EmployeeUserId == employeeUserId && r.Status != ReportStatus.Rejected
            select new { l.Id, l.ReportId, r.ReferenceNo, l.TransactionDate })
            .ToListAsync(ct);
        if (rows.Count == 0)
        {
            return [];
        }

        var lineIds = rows.Select(r => r.Id).ToList();
        var meals = (await db.ExpenseMealItems.AsNoTracking()
                .Where(m => lineIds.Contains(m.ExpenseLineId))
                .Select(m => new { m.ExpenseLineId, m.MealType })
                .ToListAsync(ct))
            .ToLookup(m => m.ExpenseLineId, m => m.MealType ?? string.Empty);
        return rows.Select(r => new MealLine(r.Id, r.ReportId, r.ReferenceNo, r.TransactionDate, meals[r.Id].ToList())).ToList();
    }

    private sealed record MealLine(long LineId, Guid ReportId, string? ReferenceNo, DateOnly Date, IReadOnlyList<string> MealTypes)
    {
        public ClaimedMeals ToClaimed(Guid currentReportId) => new(ReferenceNo, ReportId == currentReportId, MealTypes);
    }
}
