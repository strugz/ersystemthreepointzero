using ERFWeb.Application.Features.Administration;
using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Finance requirement 2 against the database (<see cref="WorkWithMealRules"/>): which accounts a line's Work With names
/// tag, whether another employee's tag covers what the claimant claims, and, in both directions, the warnings shown
/// on the lines. The one who paid (the payer, who tagged) claims the meal; the tagged employee's claim is the one the
/// approver or Finance fixes. The Expense Verifier has no part in it.
/// </summary>
public sealed class WorkWithMealCheck(ErfDbContext db, ISystemSettingsService settings)
{
    private static readonly ReportStatus[] BindingStatuses = Enum.GetValues<ReportStatus>().Where(WorkWithMealRules.Binds).ToArray();

    /// <summary>
    /// The active Work With accounts whose full name (ignoring case) is one of the names, other than the claimant.
    /// A name two accounts share, or one typed by hand, tags nobody.
    /// </summary>
    public async Task<List<long>> ResolveAsync(string? workWith, long claimantUserId, CancellationToken ct)
    {
        var names = WorkWithMealRules.Names(workWith).Select(n => n.ToLowerInvariant()).Distinct().ToList();
        if (names.Count == 0)
        {
            return [];
        }

        var matches = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.WorkWithEnabled && u.Id != claimantUserId && names.Contains(u.FullName.ToLower()))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);
        return matches
            .GroupBy(u => u.FullName.ToLowerInvariant())
            .Where(g => g.Count() == 1)
            .Select(g => g.Single().Id)
            .Order()
            .ToList();
    }

    /// <summary>Why the claimant cannot claim this line, or null.</summary>
    public async Task<string?> LineProblemAsync(long claimantUserId, WorkWithClaim claim, FinanceRuleSet rules, CancellationToken ct)
    {
        if (!rules.WorkWithMealsEnabled)
        {
            return null;
        }

        var tags = await TagsAsync(claimantUserId, claim.Date, ct);
        return WorkWithMealRules.Problem(claim, tags, rules);
    }

    /// <summary>One reason per active line of the report that someone else's tag covers (filing re-checks tags added later).</summary>
    public async Task<IReadOnlyList<string>> ReportProblemsAsync(Guid reportId, FinanceRuleSet rules, CancellationToken ct) =>
        (await ConflictsAsync(reportId, rules, ct))
            .Where(c => !c.OwnIsPayer)
            .OrderBy(c => c.Own.Date)
            .Select(c => WorkWithMealRules.Problem(Claim(c.Own), [Tag(c.Other)], rules))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>The warnings to show on each line of the report, by line id; empty when there are none.</summary>
    public async Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> WarningsAsync(Guid reportId, CancellationToken ct)
    {
        var rules = await settings.GetFinanceRulesAsync(ct);
        return (await ConflictsAsync(reportId, rules, ct))
            .GroupBy(c => c.Own.LineId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(c => c.OwnIsPayer
                        ? WorkWithMealRules.AlsoClaimedWarning(c.Other.EmployeeName, c.Other.ReferenceNo, c.Other.Status, c.Other.Cleared, c.Covered, c.Own.Date)
                        : WorkWithMealRules.PaidByWarning(c.Other.EmployeeName, c.Other.ReferenceNo, c.Other.Status, c.Other.Cleared, c.Covered, c.Own.Date))
                    .Distinct(StringComparer.Ordinal)
                    .ToList());
    }

    /// <summary>
    /// Every pair of this report's active lines with another employee's active line, on a report that is not Rejected,
    /// where one tags the other's employee and covers what that line claims. Empty when the rule is off or this report
    /// is Rejected.
    /// </summary>
    private async Task<List<WorkWithConflict>> ConflictsAsync(Guid reportId, FinanceRuleSet rules, CancellationToken ct)
    {
        if (!rules.WorkWithMealsEnabled)
        {
            return [];
        }

        var own = await FactsAsync(db.ExpenseLines.Where(l => l.ReportId == reportId && l.IsActive), ct);
        if (own.Count == 0 || !WorkWithMealRules.Binds(own[0].Status))
        {
            return [];
        }

        var claimant = own[0].EmployeeUserId;
        var dates = own.Select(o => o.Date).Distinct().ToList();
        var tagged = own.SelectMany(o => o.TaggedUserIds).Distinct().ToList();
        var others = await FactsAsync(
            db.ExpenseLines.Where(l => l.IsActive && l.ReportId != reportId && dates.Contains(l.TransactionDate)
                && db.ExpenseReports.Any(r => r.Id == l.ReportId && BindingStatuses.Contains(r.Status) && r.EmployeeUserId != claimant
                    && (l.WorkWithUserIds.Contains(claimant) || tagged.Contains(r.EmployeeUserId)))),
            ct);

        var conflicts = new List<WorkWithConflict>();
        foreach (var o in own)
        {
            foreach (var x in others.Where(x => x.Date == o.Date))
            {
                if (x.TaggedUserIds.Contains(claimant) && WorkWithMealRules.Covered(Tag(x), Claim(o), rules) is { } paid)
                {
                    conflicts.Add(new WorkWithConflict(o, x, OwnIsPayer: false, paid));
                }

                if (o.TaggedUserIds.Contains(x.EmployeeUserId) && WorkWithMealRules.Covered(Tag(o), Claim(x), rules) is { } claimed)
                {
                    conflicts.Add(new WorkWithConflict(o, x, OwnIsPayer: true, claimed));
                }
            }
        }

        return conflicts;
    }

    private async Task<List<WorkWithTag>> TagsAsync(long claimantUserId, DateOnly date, CancellationToken ct) =>
        (await FactsAsync(
            db.ExpenseLines.Where(l => l.IsActive && l.TransactionDate == date && l.WorkWithUserIds.Contains(claimantUserId)
                && db.ExpenseReports.Any(r => r.Id == l.ReportId && BindingStatuses.Contains(r.Status) && r.EmployeeUserId != claimantUserId)),
            ct))
        .Select(Tag)
        .ToList();

    private async Task<List<LineFacts>> FactsAsync(IQueryable<ExpenseLine> lines, CancellationToken ct)
    {
        var rows = await (
            from l in lines.AsNoTracking()
            join r in db.ExpenseReports.AsNoTracking() on l.ReportId equals r.Id
            join u in db.Users.AsNoTracking() on r.EmployeeUserId equals u.Id
            select new
            {
                l.Id,
                l.ReportId,
                r.EmployeeUserId,
                u.FullName,
                r.ReferenceNo,
                r.Status,
                Cleared = db.FinanceTrackings.Any(f => f.ReportId == r.Id && f.Status == FinanceStatus.ClearedForPayment),
                l.TransactionDate,
                l.Category,
                l.WorkWithUserIds,
            })
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
        return rows.Select(r => new LineFacts(r.Id, r.ReportId, r.EmployeeUserId, r.FullName, r.ReferenceNo, r.Status, r.Cleared,
                r.TransactionDate, r.Category, meals[r.Id].ToList(), r.WorkWithUserIds))
            .ToList();
    }

    private static WorkWithTag Tag(LineFacts f) => new(f.EmployeeName, f.Date, f.Category, f.MealTypes);

    private static WorkWithClaim Claim(LineFacts f) => new(f.Date, f.Category, f.MealTypes);

    private sealed record LineFacts(
        long LineId,
        Guid ReportId,
        long EmployeeUserId,
        string EmployeeName,
        string? ReferenceNo,
        ReportStatus Status,
        bool Cleared,
        DateOnly Date,
        ExpenseCategory Category,
        IReadOnlyList<string> MealTypes,
        IReadOnlyList<long> TaggedUserIds);

    private sealed record WorkWithConflict(LineFacts Own, LineFacts Other, bool OwnIsPayer, string Covered);
}
