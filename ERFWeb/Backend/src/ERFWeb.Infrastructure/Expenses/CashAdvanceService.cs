using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Rows of the R47 "CASH ADVANCE RECEIVED" block. Only a Liquidation of Cash Advance carries them;
/// the reconciliation uses their total. Same edit rules and rollback discipline as expense lines.
/// </summary>
public sealed class CashAdvanceService(
    ErfDbContext db,
    IClock clock,
    ITransactionRunner tx,
    ReportEditGate gate,
    ExpenseReportDetailLoader loader) : ICashAdvanceService
{
    public async Task<CashAdvanceLineDto> AddAsync(Guid reportId, SaveCashAdvanceLineRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            Validate(request, report);

            var nextSort = await db.CashAdvanceLines.Where(c => c.ReportId == reportId).MaxAsync(c => (int?)c.SortOrder, token) ?? 0;
            var now = clock.UtcNow;
            var line = new CashAdvanceLine { ReportId = reportId, SortOrder = nextSort + 1, CreatedAt = now, UpdatedAt = now };
            Apply(line, request, now);
            db.CashAdvanceLines.Add(line);
            report.UpdatedAt = now;
            await db.SaveChangesAsync(token);
            return line.Id;
        }, ct);

        return await loader.LoadCashAdvanceAsync(id, ct);
    }

    public async Task<CashAdvanceLineDto> UpdateAsync(Guid reportId, long lineId, SaveCashAdvanceLineRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RowVersion is null)
        {
            throw new ValidationException("rowVersion", "rowVersion is required when updating a cash advance.");
        }

        await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            Validate(request, report);

            var line = await db.CashAdvanceLines.FirstOrDefaultAsync(c => c.Id == lineId && c.ReportId == reportId, token)
                ?? throw new NotFoundException("Cash advance not found.");

            db.SetRowVersion(line, request.RowVersion.Value);
            var now = clock.UtcNow;
            Apply(line, request, now);
            report.UpdatedAt = now;
            await db.SaveWithConcurrencyAsync(token);
        }, ct);

        return await loader.LoadCashAdvanceAsync(lineId, ct);
    }

    public Task RemoveAsync(Guid reportId, long lineId, CancellationToken ct = default) =>
        tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            var line = await db.CashAdvanceLines.FirstOrDefaultAsync(c => c.Id == lineId && c.ReportId == reportId, token)
                ?? throw new NotFoundException("Cash advance not found.");

            db.CashAdvanceLines.Remove(line);
            report.UpdatedAt = clock.UtcNow;
            await db.SaveWithConcurrencyAsync(token);
        }, ct);

    private static void Validate(SaveCashAdvanceLineRequest r, ExpenseReport report)
    {
        if (!ReportWorkflowRules.UsesCashAdvance(report.ReportType))
        {
            throw new ConflictException("Cash advances apply only to a Liquidation of Cash Advance report.");
        }

        var errors = new Dictionary<string, string[]>();
        if (r.Amount <= 0)
        {
            errors["amount"] = ["Amount must be greater than zero."];
        }

        if (r.ReferenceDocument is { Length: > 100 })
        {
            errors["referenceDocument"] = ["Reference document must be 100 characters or fewer."];
        }

        if (r.ReferenceNo is { Length: > 50 })
        {
            errors["referenceNo"] = ["Reference no. must be 50 characters or fewer."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void Apply(CashAdvanceLine line, SaveCashAdvanceLineRequest r, DateTimeOffset now)
    {
        line.AdvanceDate = r.AdvanceDate;
        line.ReferenceDocument = Clean(r.ReferenceDocument);
        line.ReferenceNo = Clean(r.ReferenceNo);
        line.Amount = Math.Round(r.Amount, 2, MidpointRounding.ToEven);
        line.UpdatedAt = now;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
