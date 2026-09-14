using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Employee-side report header + cash advance use cases. All writes run inside the
/// serializable transaction runner and roll back on any failure.
/// </summary>
public sealed class ExpenseReportService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    ReportEditGate gate,
    ExpenseReportDetailLoader loader) : IExpenseReportService
{
    private const int MaxPurposeLength = 300;

    public async Task<PagedResult<ExpenseReportListItemDto>> ListMineAsync(ExpenseReportListQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var reports = db.ExpenseReports.AsNoTracking().Where(r => r.EmployeeUserId == user.UserId);

        if (query.Status is { } status)
        {
            reports = reports.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            reports = reports.Where(r =>
                (r.Purpose != null && EF.Functions.ILike(r.Purpose, term)) ||
                (r.ReferenceNo != null && EF.Functions.ILike(r.ReferenceNo, term)));
        }

        var total = await reports.CountAsync(ct);
        if (total == 0)
        {
            return PagedResult<ExpenseReportListItemDto>.Empty(query);
        }

        var projected = reports.Select(r => new
        {
            r.Id,
            r.ReferenceNo,
            r.ReportType,
            r.Purpose,
            r.DateFrom,
            r.DateTo,
            r.Status,
            r.FiledAt,
            r.UpdatedAt,
            r.CreatedAt,
            TotalAmount = db.ExpenseLines.Where(l => l.ReportId == r.Id && l.IsActive).Sum(l => (decimal?)l.TotalAmount) ?? 0m,
            LineCount = db.ExpenseLines.Count(l => l.ReportId == r.Id && l.IsActive),
        });

        var desc = query.Direction == SortDirection.Desc;
        projected = (query.Sort?.ToLowerInvariant()) switch
        {
            "status" => desc ? projected.OrderByDescending(x => x.Status).ThenByDescending(x => x.UpdatedAt) : projected.OrderBy(x => x.Status).ThenByDescending(x => x.UpdatedAt),
            "datefrom" => desc ? projected.OrderByDescending(x => x.DateFrom).ThenByDescending(x => x.CreatedAt) : projected.OrderBy(x => x.DateFrom).ThenBy(x => x.CreatedAt),
            "totalamount" => desc ? projected.OrderByDescending(x => x.TotalAmount).ThenByDescending(x => x.CreatedAt) : projected.OrderBy(x => x.TotalAmount).ThenBy(x => x.CreatedAt),
            "referenceno" => desc ? projected.OrderByDescending(x => x.ReferenceNo).ThenByDescending(x => x.CreatedAt) : projected.OrderBy(x => x.ReferenceNo).ThenBy(x => x.CreatedAt),
            _ => desc ? projected.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Id) : projected.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id),
        };

        var items = await projected
            .Skip(query.Skip)
            .Take(query.NormalizedPageSize)
            .Select(x => new ExpenseReportListItemDto(
                x.Id, x.ReferenceNo, x.ReportType, x.Purpose, x.DateFrom, x.DateTo, x.Status,
                x.TotalAmount, x.LineCount, x.FiledAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<ExpenseReportListItemDto>(items, total, query.NormalizedPage, query.NormalizedPageSize);
    }

    public Task<ExpenseReportDetailDto> GetAsync(Guid reportId, CancellationToken ct = default) => loader.LoadAsync(reportId, ct);

    public async Task<ExpenseReportDetailDto> CreateAsync(CreateExpenseReportRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateHeader(request.Purpose, request.DateFrom, request.DateTo, request.RevolvingFundAmount);

        var reportId = await tx.ExecuteAsync(async token =>
        {
            var now = clock.UtcNow;
            var report = new ExpenseReport
            {
                Id = Guid.NewGuid(),
                EmployeeUserId = user.UserId,
                ReportType = request.ReportType,
                Purpose = request.Purpose?.Trim(),
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                Status = ReportStatus.New,
                RevolvingFundAmount = ReportWorkflowRules.UsesRevolvingFund(request.ReportType) ? request.RevolvingFundAmount : 0m,
                CurrentCycle = 1,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.ExpenseReports.Add(report);
            await db.SaveChangesAsync(token);

            await audit.WriteAsync(new WorkflowAuditEntry("ReportCreated", report.Id, user.UserId, null), token);
            return report.Id;
        }, ct);

        return await loader.LoadAsync(reportId, ct);
    }

    public async Task<ExpenseReportDetailDto> UpdateAsync(Guid reportId, UpdateExpenseReportRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateHeader(request.Purpose, request.DateFrom, request.DateTo, request.RevolvingFundAmount);

        await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadEditableAsync(reportId, token);
            db.SetRowVersion(report, request.RowVersion);

            // Only a Liquidation carries cash advances; switching type away from it clears them,
            // as the desktop form cleared the block.
            if (ReportWorkflowRules.UsesCashAdvance(report.ReportType) && !ReportWorkflowRules.UsesCashAdvance(request.ReportType))
            {
                var stale = await db.CashAdvanceLines.Where(c => c.ReportId == reportId).ToListAsync(token);
                db.CashAdvanceLines.RemoveRange(stale);
            }

            report.ReportType = request.ReportType;
            report.Purpose = request.Purpose?.Trim();
            report.DateFrom = request.DateFrom;
            report.DateTo = request.DateTo;
            report.RevolvingFundAmount = ReportWorkflowRules.UsesRevolvingFund(request.ReportType) ? request.RevolvingFundAmount : 0m;
            report.UpdatedAt = clock.UtcNow;
            await db.SaveWithConcurrencyAsync(token);
        }, ct);

        return await loader.LoadAsync(reportId, ct);
    }

    public Task DeleteAsync(Guid reportId, CancellationToken ct = default) =>
        tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadOwnedAsync(reportId, token);
            if (!ReportWorkflowRules.CanDelete(report.Status, report.FiledAt))
            {
                throw new ConflictException("Only a report that has never been filed can be deleted.");
            }

            // Children (cash advance, lines, attachments) are removed by ON DELETE CASCADE.
            db.ExpenseReports.Remove(report);
            await db.SaveWithConcurrencyAsync(token);
            await audit.WriteAsync(new WorkflowAuditEntry("ReportDeleted", reportId, user.UserId, null), token);
        }, ct);

    private static void ValidateHeader(string? purpose, DateOnly? from, DateOnly? to, decimal revolvingFund)
    {
        var errors = new Dictionary<string, string[]>();

        if (purpose is { Length: > MaxPurposeLength })
        {
            errors["purpose"] = [$"Purpose must be {MaxPurposeLength} characters or fewer."];
        }

        if (from is not null && to is not null && to < from)
        {
            errors["dateTo"] = ["Date To must be on or after Date From."];
        }

        if (revolvingFund < 0)
        {
            errors["revolvingFundAmount"] = ["Revolving fund amount cannot be negative."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
