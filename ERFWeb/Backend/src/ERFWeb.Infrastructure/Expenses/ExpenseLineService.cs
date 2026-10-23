using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Expense line use cases. Each line's total, per-diem computation text and meal/transport
/// children are (re)derived server-side from the request; the client never supplies totals.
/// A CRM activity link is checked with the CRM before the transaction (see <see cref="CrmActivityClaimCheck"/>).
/// </summary>
public sealed class ExpenseLineService(
    ErfDbContext db,
    IClock clock,
    ITransactionRunner tx,
    ReportEditGate gate,
    ExpenseReportDetailLoader loader,
    CrmActivityClaimCheck crmClaims) : IExpenseLineService
{
    public async Task<ExpenseLineDto> AddAsync(Guid reportId, SaveExpenseLineRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await crmClaims.VerifyAsync(request.CrmActivityId, null, request.TransactionDate, ct);

        var lineId = await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            var modes = await ValidateAsync(request, report, null, token);
            await crmClaims.EnsureNotClaimedElsewhereAsync(report, request.CrmActivityId, token);

            var nextSort = await db.ExpenseLines
                .Where(l => l.ReportId == reportId)
                .MaxAsync(l => (int?)l.SortOrder, token) ?? 0;

            var now = clock.UtcNow;
            var line = new ExpenseLine
            {
                ReportId = reportId,
                SortOrder = nextSort + 1,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            Apply(line, request, now);
            db.ExpenseLines.Add(line);
            await db.SaveChangesAsync(token);

            ReplaceChildren(line.Id, request, modes);
            report.UpdatedAt = now;
            await db.SaveChangesAsync(token);
            return line.Id;
        }, ct);

        return await loader.LoadLineAsync(lineId, ct);
    }

    public async Task<ExpenseLineDto> UpdateAsync(Guid reportId, long lineId, SaveExpenseLineRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RowVersion is null)
        {
            throw new ValidationException("rowVersion", "rowVersion is required when updating a line.");
        }

        var currentActivityId = await db.ExpenseLines.AsNoTracking()
            .Where(l => l.Id == lineId && l.ReportId == reportId)
            .Select(l => l.CrmActivityId)
            .FirstOrDefaultAsync(ct);
        await crmClaims.VerifyAsync(request.CrmActivityId, currentActivityId, request.TransactionDate, ct);

        await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            var modes = await ValidateAsync(request, report, lineId, token);
            await crmClaims.EnsureNotClaimedElsewhereAsync(report, request.CrmActivityId, token);

            var line = await db.ExpenseLines.FirstOrDefaultAsync(l => l.Id == lineId && l.ReportId == reportId && l.IsActive, token)
                ?? throw new NotFoundException("Expense line not found.");

            db.SetRowVersion(line, request.RowVersion.Value);
            var now = clock.UtcNow;
            Apply(line, request, now);

            var existingMeals = await db.ExpenseMealItems.Where(m => m.ExpenseLineId == lineId).ToListAsync(token);
            var existingTransports = await db.ExpenseTransportItems.Where(t => t.ExpenseLineId == lineId).ToListAsync(token);
            db.ExpenseMealItems.RemoveRange(existingMeals);
            db.ExpenseTransportItems.RemoveRange(existingTransports);
            ReplaceChildren(lineId, request, modes);

            report.UpdatedAt = now;
            await db.SaveWithConcurrencyAsync(token);
        }, ct);

        return await loader.LoadLineAsync(lineId, ct);
    }

    public Task RemoveAsync(Guid reportId, long lineId, CancellationToken ct = default) =>
        tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            var line = await db.ExpenseLines.FirstOrDefaultAsync(l => l.Id == lineId && l.ReportId == reportId && l.IsActive, token)
                ?? throw new NotFoundException("Expense line not found.");

            // Soft delete: the row stays for history; the R47 and totals only read active lines.
            line.IsActive = false;
            line.UpdatedAt = clock.UtcNow;
            report.UpdatedAt = line.UpdatedAt;
            await db.SaveWithConcurrencyAsync(token);
        }, ct);

    /// <summary>Validates the line and its fare legs; returns the fare modes the legs name, for saving them.</summary>
    private async Task<IReadOnlyDictionary<long, FareModeRule>> ValidateAsync(
        SaveExpenseLineRequest r, ExpenseReport report, long? lineId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>(ExpenseLineRules.Validate(
            r.TransactionDate, r.Particulars, r.Amount, r.VatAmount, r.InvoiceOrNo, r.Multiplier,
            r.IsPerdiem, r.TotalDays, r.MinusDays, report.DateFrom, report.DateTo));

        if (r.Category == ExpenseCategory.Meals && r.Meals is { Count: > 0 } &&
            r.Meals.Any(m => string.IsNullOrWhiteSpace(m.MealType)))
        {
            errors["meals"] = ["Each meal item needs a meal type (Breakfast, Lunch, Dinner, OT)."];
        }

        IReadOnlyDictionary<long, FareModeRule> modes = new Dictionary<long, FareModeRule>();
        if (r.Transports is { Count: > 0 } legs)
        {
            var modeIds = legs.Where(t => t.FareModeId != null).Select(t => t.FareModeId!.Value).Distinct().ToList();
            modes = await db.FareModes.AsNoTracking()
                .Where(m => modeIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => new FareModeRule(m.Id, m.IsActive, m.RequiresDetail), ct);

            // A mode deactivated after the line was saved stays usable on that line.
            var alreadyOnLine = lineId is { } id
                ? (await db.ExpenseTransportItems.AsNoTracking()
                    .Where(t => t.ExpenseLineId == id && t.FareModeId != null)
                    .Select(t => t.FareModeId!.Value)
                    .ToListAsync(ct)).ToHashSet()
                : [];

            var legErrors = FareLegRules.Validate(
                legs.Select(t => new FareLegInput(t.FareModeId, t.FareModeDetail, t.FareAmount)).ToList(), modes, alreadyOnLine);
            foreach (var (key, messages) in legErrors)
            {
                errors[key] = messages;
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return modes;
    }

    /// <summary>Copies request fields onto the entity and derives multiplier, total and computation text.</summary>
    private static void Apply(ExpenseLine line, SaveExpenseLineRequest r, DateTimeOffset now)
    {
        var multiplier = r.Multiplier;
        string? computation = null;
        if (r.IsPerdiem && r.TotalDays is { } totalDays)
        {
            var allowance = AllowanceCalculator.Compute(totalDays, r.MinusDays ?? 0, r.Amount);
            multiplier = allowance.Multiplier;
            computation = allowance.ComputationText;
        }

        line.TransactionDate = r.TransactionDate;
        line.Particulars = r.Particulars?.Trim();
        line.WorkWith = Clean(r.WorkWith);
        line.InvoiceOrNo = Clean(r.InvoiceOrNo);
        line.ExpenseType = r.ExpenseType;
        line.Category = r.Category;
        line.Amount = r.Amount;
        line.VatAmount = ExpenseLineRules.AllowsVat(line.InvoiceOrNo) ? r.VatAmount : 0m;
        line.Multiplier = multiplier;
        line.TotalAmount = ExpenseLineRules.Total(r.Amount, multiplier, ExpenseLineRules.AllowsVat(line.InvoiceOrNo));
        line.Location = Clean(r.Location);
        line.ServiceNumber = Clean(r.ServiceNumber);
        line.Instrument = Clean(r.Instrument);
        line.SerialNumber = Clean(r.SerialNumber);
        line.IsPerdiem = r.IsPerdiem;
        line.MinusDays = r.IsPerdiem ? r.MinusDays ?? 0 : null;
        line.TotalDays = r.IsPerdiem ? r.TotalDays : null;
        line.Computation = computation;
        line.Remarks = Clean(r.Remarks);
        line.CrmActivityId = CrmActivityClaimCheck.Normalize(r.CrmActivityId);
        line.UpdatedAt = now;
    }

    private void ReplaceChildren(long lineId, SaveExpenseLineRequest r, IReadOnlyDictionary<long, FareModeRule> modes)
    {
        foreach (var meal in r.Meals ?? [])
        {
            db.ExpenseMealItems.Add(new ExpenseMealItem
            {
                ExpenseLineId = lineId,
                MealType = meal.MealType.Trim(),
                PaidFor = Clean(meal.PaidFor),
                PaidEmployeeId = meal.PaidEmployeeId,
            });
        }

        foreach (var fare in r.Transports ?? [])
        {
            db.ExpenseTransportItems.Add(new ExpenseTransportItem
            {
                ExpenseLineId = lineId,
                FareFrom = Clean(fare.FareFrom),
                FareTo = Clean(fare.FareTo),
                FareAmount = fare.FareAmount,
                FareModeId = fare.FareModeId,
                FareModeDetail = fare.FareModeId is { } modeId ? FareLegRules.NormalizeDetail(modes[modeId], fare.FareModeDetail) : null,
            });
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
