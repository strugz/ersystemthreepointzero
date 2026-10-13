using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;
using ERFWeb.Domain.Approval;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using ERFWeb.Infrastructure.Verification;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// LINQ read model for the report editor: header + employee, cash advance, active lines with
/// their meal/transport children, approval steps of the current cycle, and attachment metadata.
/// </summary>
public sealed class ExpenseReportDetailLoader(ErfDbContext db, ICurrentUser user, VerificationReader verification)
{
    public async Task<ExpenseReportDetailDto> LoadAsync(Guid reportId, CancellationToken ct)
    {
        var header = await (
            from r in db.ExpenseReports.AsNoTracking()
            where r.Id == reportId
            join u in db.Users.AsNoTracking() on r.EmployeeUserId equals u.Id
            select new
            {
                Report = r,
                RowVersion = EF.Property<uint>(r, DbContextExtensions.RowVersionProperty),
                EmployeeName = u.FullName,
            }).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Expense report not found.");

        var lines = await LoadLinesAsync(reportId, ct);
        var cashAdvances = await LoadCashAdvancesAsync(reportId, ct);

        var stepRows = await (
            from s in db.ApprovalSteps.AsNoTracking()
            where s.ReportId == reportId && s.Cycle == header.Report.CurrentCycle
            join a in db.Users.AsNoTracking() on s.ApproverUserId equals a.Id
            orderby s.StepOrder
            select new { Step = s, ApproverName = a.FullName })
            .ToListAsync(ct);
        var steps = stepRows
            .Select(x => new ApprovalStepDto(x.Step.StepOrder, x.Step.ApproverUserId, x.ApproverName, x.Step.Status, x.Step.ActedAt, x.Step.Note, x.Step.SignatoryLevel))
            .ToList();
        var workflowSteps = stepRows
            .Select(x => new WorkflowStep(x.Step.Id, x.Step.StepOrder, x.Step.SignatoryLevel, x.Step.ApproverUserId, x.Step.Status))
            .ToList();
        var anyApproverActed = workflowSteps.Any(s => s.Status is ApprovalStatus.Approved or ApprovalStatus.Overridden);

        var finance = await db.FinanceTrackings.AsNoTracking()
            .Where(f => f.ReportId == reportId)
            .Select(f => new FinanceSummaryDto(f.Status, f.Remarks, f.ReceivedAt, f.ClearedAt))
            .FirstOrDefaultAsync(ct);

        var attachments = await LoadAttachmentsAsync(reportId, ct);

        var rep = header.Report;
        var panel = await verification.PanelAsync(reportId, rep.EmployeeUserId, rep.Status, finance?.Status, ct);
        if (finance is not null)
        {
            finance = finance with { UnderVerification = panel.UnderVerification };
        }

        var isOwner = rep.EmployeeUserId == user.UserId;
        var review = ApprovalWorkflow.Permissions(rep.Status, workflowSteps, rep.EmployeeUserId, user.UserId);

        return new ExpenseReportDetailDto(
            rep.Id,
            rep.ReferenceNo,
            rep.EmployeeUserId,
            header.EmployeeName,
            rep.ReportType,
            rep.Purpose,
            rep.DateFrom,
            rep.DateTo,
            rep.Status,
            rep.RevolvingFundAmount,
            rep.CurrentCycle,
            rep.FiledAt,
            rep.ApprovedAt,
            rep.CancelNote,
            rep.CreatedAt,
            rep.UpdatedAt,
            header.RowVersion,
            CanEdit: isOwner && ReportWorkflowRules.CanEdit(rep.Status),
            CanFile: isOwner && ReportWorkflowRules.CanFile(rep.Status),
            CanReopen: isOwner && ReportWorkflowRules.CanReopen(rep.Status, anyApproverActed),
            CanDelete: isOwner && ReportWorkflowRules.CanDelete(rep.Status, rep.FiledAt),
            TotalAmount: lines.Sum(l => l.TotalAmount),
            CashAdvanceTotal: cashAdvances.Sum(c => c.Amount),
            cashAdvances,
            lines,
            steps,
            attachments,
            new ReviewActionsDto(review.CanApprove, review.CanReturn, review.CanReject, review.CanOverride, review.CanAdjustLines),
            finance,
            panel.Requests.Count > 0 || panel.CanRequest ? panel : null);
    }

    public async Task<IReadOnlyList<ExpenseLineDto>> LoadLinesAsync(Guid reportId, CancellationToken ct)
    {
        var lines = await db.ExpenseLines.AsNoTracking()
            .Where(l => l.ReportId == reportId && l.IsActive)
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Id)
            .Select(l => new { Line = l, RowVersion = EF.Property<uint>(l, DbContextExtensions.RowVersionProperty) })
            .ToListAsync(ct);

        var lineIds = lines.Select(x => x.Line.Id).ToList();
        var adjusterIds = lines.Where(x => x.Line.AdjustedByUserId != null).Select(x => x.Line.AdjustedByUserId!.Value).Distinct().ToList();
        var adjusters = await db.Users.AsNoTracking()
            .Where(u => adjusterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var meals = await db.ExpenseMealItems.AsNoTracking()
            .Where(m => lineIds.Contains(m.ExpenseLineId))
            .OrderBy(m => m.Id)
            .ToListAsync(ct);

        var transports = await db.ExpenseTransportItems.AsNoTracking()
            .Where(t => lineIds.Contains(t.ExpenseLineId))
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        var mealsByLine = meals.ToLookup(m => m.ExpenseLineId);
        var transportsByLine = transports.ToLookup(t => t.ExpenseLineId);

        return lines.Select(x => ToDto(x.Line, x.RowVersion,
                mealsByLine[x.Line.Id].Select(m => new ExpenseMealItemDto(m.MealType ?? string.Empty, m.PaidFor, m.PaidEmployeeId)).ToList(),
                transportsByLine[x.Line.Id].Select(t => new ExpenseTransportItemDto(t.FareFrom, t.FareTo, t.FareAmount)).ToList(),
                x.Line.AdjustedByUserId is { } by ? adjusters.GetValueOrDefault(by) : null))
            .ToList();
    }

    public async Task<ExpenseLineDto> LoadLineAsync(long lineId, CancellationToken ct)
    {
        var x = await db.ExpenseLines.AsNoTracking()
            .Where(l => l.Id == lineId)
            .Select(l => new { Line = l, RowVersion = EF.Property<uint>(l, DbContextExtensions.RowVersionProperty) })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Expense line not found.");

        var meals = await db.ExpenseMealItems.AsNoTracking()
            .Where(m => m.ExpenseLineId == lineId).OrderBy(m => m.Id)
            .Select(m => new ExpenseMealItemDto(m.MealType ?? string.Empty, m.PaidFor, m.PaidEmployeeId))
            .ToListAsync(ct);

        var transports = await db.ExpenseTransportItems.AsNoTracking()
            .Where(t => t.ExpenseLineId == lineId).OrderBy(t => t.Id)
            .Select(t => new ExpenseTransportItemDto(t.FareFrom, t.FareTo, t.FareAmount))
            .ToListAsync(ct);

        return ToDto(x.Line, x.RowVersion, meals, transports);
    }

    public Task<List<CashAdvanceLineDto>> LoadCashAdvancesAsync(Guid reportId, CancellationToken ct) =>
        db.CashAdvanceLines.AsNoTracking()
            .Where(c => c.ReportId == reportId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new CashAdvanceLineDto(c.Id, c.AdvanceDate, c.ReferenceDocument, c.ReferenceNo, c.Amount,
                EF.Property<uint>(c, DbContextExtensions.RowVersionProperty)))
            .ToListAsync(ct);

    public async Task<CashAdvanceLineDto> LoadCashAdvanceAsync(long lineId, CancellationToken ct) =>
        await db.CashAdvanceLines.AsNoTracking()
            .Where(c => c.Id == lineId)
            .Select(c => new CashAdvanceLineDto(c.Id, c.AdvanceDate, c.ReferenceDocument, c.ReferenceNo, c.Amount,
                EF.Property<uint>(c, DbContextExtensions.RowVersionProperty)))
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Cash advance not found.");

    public Task<List<AttachmentDto>> LoadAttachmentsAsync(Guid reportId, CancellationToken ct) =>
        (from a in db.ReceiptAttachments.AsNoTracking()
         where a.ReportId == reportId
         join u in db.Users.AsNoTracking() on a.UploadedByUserId equals u.Id
         orderby a.UploadedAt
         select new AttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes, u.FullName, a.UploadedAt))
        .ToListAsync(ct);

    private static ExpenseLineDto ToDto(
        ExpenseLine l,
        uint rowVersion,
        IReadOnlyList<ExpenseMealItemDto> meals,
        IReadOnlyList<ExpenseTransportItemDto> transports,
        string? adjustedByName = null) =>
        new(l.Id, l.TransactionDate, l.Particulars, l.WorkWith, l.InvoiceOrNo, l.ExpenseType, l.Category,
            l.Amount, l.VatAmount, l.TotalAmount, l.Location, l.ServiceNumber, l.Instrument, l.SerialNumber,
            l.IsPerdiem, l.Multiplier, l.MinusDays, l.TotalDays, l.Computation, l.Remarks, l.SortOrder,
            rowVersion, meals, transports,
            l.OriginalAmount is { } original && l.AdjustedAt is { } at
                ? new LineAdjustmentDto(original, l.OriginalTotalAmount ?? l.TotalAmount, adjustedByName, at, l.AdjustmentReason)
                : null);
}
