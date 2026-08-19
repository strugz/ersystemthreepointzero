using ERSystem.Web.Application.Features.ReportReview;
using ERSystem.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERSystem.Web.Infrastructure.Services;

internal sealed record ReportReviewData(
    IReadOnlyList<ExpenseLineDto> Expenses,
    CashAdvanceDto? CashAdvance,
    IReadOnlyList<ReceiptAttachmentDto> Attachments,
    IReadOnlyList<ApprovalTrailItemDto> ApprovalTrail);

internal static class ReportReviewDataLoader
{
    public static async Task<ReportReviewData> LoadAsync(
        LegacyErDbContext db,
        string reportId,
        int? approvalCycle,
        CancellationToken cancellationToken)
    {
        // Match the legacy Crystal report: only active expense lines contribute to review totals.
        var expenseRows = await db.Expenses.AsNoTracking()
            .Where(x => x.ReportId == reportId && x.Status == "True")
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.TransactionDate)
            .ToListAsync(cancellationToken);
        var expenses = expenseRows.Select(x => new ExpenseLineDto(
            x.Id, x.TransactionDate, x.PerDiem == "1", x.Particulars?.Trim() ?? string.Empty,
            x.InvoiceNumber?.Trim() ?? string.Empty, x.Multiplier, x.ExpenseType?.Trim() ?? string.Empty,
            x.Category?.Trim() ?? string.Empty, ToMoney(x.Amount), ToNullableMoney(x.VatAmount),
            ToMoney(x.TotalAmount), x.Location?.Trim() ?? string.Empty, x.Remarks?.Trim() ?? string.Empty,
            x.WorkWith?.Trim() ?? string.Empty, x.ServiceNumber?.Trim() ?? string.Empty,
            x.Instrument?.Trim() ?? string.Empty, x.SerialNumber?.Trim() ?? string.Empty,
            x.MinusDays?.Trim() ?? string.Empty, x.TotalDays?.Trim() ?? string.Empty,
            x.Computation?.Trim() ?? string.Empty)).ToArray();

        var cash = await db.CashAdvances.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReportId == reportId, cancellationToken);
        var cashAdvance = cash is null ? null : new CashAdvanceDto(
            cash.Amount.HasValue ? ToMoney(cash.Amount) : null,
            cash.Date?.Trim() ?? string.Empty,
            cash.ReferenceDocument?.Trim() ?? string.Empty,
            cash.ReferenceNumber?.Trim() ?? string.Empty,
            cash.RevolvingFund?.Trim() ?? string.Empty);

        var attachments = await db.ScannedReceipts.AsNoTracking()
            .Where(x => x.ReportId == reportId)
            .OrderBy(x => x.Id)
            .Select(x => new ReceiptAttachmentDto(
                x.Id, x.OriginalFileName, x.ContentType, x.FileSizeBytes, x.CreatedDate))
            .ToArrayAsync(cancellationToken);

        var trail = approvalCycle.HasValue
            ? await LoadApprovalTrailAsync(db, reportId, approvalCycle.Value, cancellationToken)
            : [];

        return new ReportReviewData(expenses, cashAdvance, attachments, trail);
    }

    private static async Task<IReadOnlyList<ApprovalTrailItemDto>> LoadApprovalTrailAsync(
        LegacyErDbContext db,
        string reportId,
        int approvalCycle,
        CancellationToken cancellationToken)
    {
        var assignments = await db.ApprovalTransactions.AsNoTracking()
            .Where(x => x.ReportId == reportId && x.ApprovalCycle == approvalCycle)
            .OrderBy(x => x.StepOrder)
            .ToListAsync(cancellationToken);
        var approverIds = assignments.Select(x => x.ApproverUserId).ToArray();
        var approvers = await db.Users.AsNoTracking()
            .Where(x => x.UserId.HasValue && approverIds.Contains(x.UserId.Value))
            .ToDictionaryAsync(
                x => x.UserId!.Value,
                x => x.FullName ?? x.Username ?? string.Empty,
                cancellationToken);

        return assignments.Select(x => new ApprovalTrailItemDto(
            x.ApproverUserId,
            approvers.GetValueOrDefault(x.ApproverUserId, string.Empty).Trim(),
            x.StepOrder,
            x.ActionedAtUtc,
            x.Status)).ToArray();
    }

    private static decimal ToMoney(double? value) =>
        Math.Round(Convert.ToDecimal(value ?? 0d), 2, MidpointRounding.AwayFromZero);

    private static decimal? ToNullableMoney(double? value) => value.HasValue ? ToMoney(value) : null;
}
