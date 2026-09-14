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
/// </summary>
public sealed class ExpenseLineService(
    ErfDbContext db,
    IClock clock,
    ITransactionRunner tx,
    ReportEditGate gate,
    ExpenseReportDetailLoader loader) : IExpenseLineService
{
    public async Task<ExpenseLineDto> AddAsync(Guid reportId, SaveExpenseLineRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lineId = await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            Validate(request, report);

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

            ReplaceChildren(line.Id, request);
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

        await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            Validate(request, report);

            var line = await db.ExpenseLines.FirstOrDefaultAsync(l => l.Id == lineId && l.ReportId == reportId && l.IsActive, token)
                ?? throw new NotFoundException("Expense line not found.");

            db.SetRowVersion(line, request.RowVersion.Value);
            var now = clock.UtcNow;
            Apply(line, request, now);

            var existingMeals = await db.ExpenseMealItems.Where(m => m.ExpenseLineId == lineId).ToListAsync(token);
            var existingTransports = await db.ExpenseTransportItems.Where(t => t.ExpenseLineId == lineId).ToListAsync(token);
            db.ExpenseMealItems.RemoveRange(existingMeals);
            db.ExpenseTransportItems.RemoveRange(existingTransports);
            ReplaceChildren(lineId, request);

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

    private static void Validate(SaveExpenseLineRequest r, ExpenseReport report)
    {
        var errors = new Dictionary<string, string[]>(ExpenseLineRules.Validate(
            r.TransactionDate, r.Particulars, r.Amount, r.VatAmount, r.InvoiceOrNo, r.Multiplier,
            r.IsPerdiem, r.TotalDays, r.MinusDays, report.DateFrom, report.DateTo));

        if (r.Category == ExpenseCategory.Meals && r.Meals is { Count: > 0 } &&
            r.Meals.Any(m => string.IsNullOrWhiteSpace(m.MealType)))
        {
            errors["meals"] = ["Each meal item needs a meal type (Breakfast, Lunch, Dinner, OT)."];
        }

        if (r.Transports is { Count: > 0 } && r.Transports.Any(t => t.FareAmount < 0))
        {
            errors["transports"] = ["Fare amounts cannot be negative."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
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
        line.UpdatedAt = now;
    }

    private void ReplaceChildren(long lineId, SaveExpenseLineRequest r)
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
            });
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
