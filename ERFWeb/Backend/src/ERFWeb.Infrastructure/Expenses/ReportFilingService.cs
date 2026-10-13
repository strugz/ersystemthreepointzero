using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;
using ERFWeb.Domain.Administration;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Files a report into the ordered approval pipeline (see plan §13.1) and reopens a filed
/// report for correction. Both run in one serializable transaction with an audit row.
/// </summary>
public sealed class ReportFilingService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    IApprovalNotifier notifier,
    IReferenceNumberGenerator referenceNumbers,
    ReportEditGate gate,
    ExpenseReportDetailLoader loader,
    ISystemSettingsService settings,
    ILogger<ReportFilingService> logger) : IReportFilingService
{
    public async Task<FileReportResultDto> FileAsync(Guid reportId, CancellationToken ct = default)
    {
        var outcome = await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadOwnedAsync(reportId, token);

            var employee = await db.Users.AsNoTracking()
                .Where(u => u.Id == report.EmployeeUserId)
                .Select(u => new { u.FullName, u.CreatedAt, HasSignature = u.Signature != null && u.Signature.Length > 0 })
                .FirstAsync(token);

            // Spec 9.11: a newly registered employee files only after the cooling period.
            var coolingDays = await settings.GetCoolingPeriodDaysAsync(token);
            var coolingElapsed = AccountRules.CoolingPeriodElapsed(employee.CreatedAt, clock.UtcNow, coolingDays);

            var route = await (
                from ar in db.ApprovalRoutes.AsNoTracking()
                where ar.EmployeeUserId == report.EmployeeUserId
                join a in db.Users.AsNoTracking() on ar.ApproverUserId equals a.Id
                where a.IsActive
                orderby ar.StepOrder
                select new { ar.StepOrder, a.Id, a.FullName, a.Email, a.MobileNo })
                .ToListAsync(token);

            var lineCount = await db.ExpenseLines.CountAsync(l => l.ReportId == reportId && l.IsActive, token);
            var cashAmount = await db.CashAdvanceLines.AsNoTracking()
                .Where(c => c.ReportId == reportId).SumAsync(c => (decimal?)c.Amount, token) ?? 0m;

            var blockers = ReportWorkflowRules.FilingBlockers(
                report.Status, lineCount, employee.HasSignature, route.Count, report.Purpose, report.ReportType, cashAmount, report.RevolvingFundAmount,
                coolingElapsed);
            if (blockers.Count > 0)
            {
                throw new ValidationException(new Dictionary<string, string[]> { ["report"] = blockers.ToArray() });
            }

            var isRefile = report.FiledAt is not null;
            var now = clock.UtcNow;

            // Re-filing after a return starts a fresh cycle; earlier steps are kept as history.
            if (isRefile)
            {
                report.CurrentCycle += 1;
            }

            // Snapshot the current route so later route edits never affect an in-flight report.
            foreach (var step in route)
            {
                db.ApprovalSteps.Add(new ApprovalStep
                {
                    ReportId = reportId,
                    Cycle = report.CurrentCycle,
                    StepOrder = step.StepOrder,
                    ApproverUserId = step.Id,
                    Status = ApprovalStatus.Pending,
                });
            }

            // The reference number is issued once, on first filing, and kept across returns and re-files
            // so the series has no gaps from deleted drafts.
            report.ReferenceNo ??= await referenceNumbers.NextAsync(now, token);

            report.Status = ReportStatus.ForApproval;
            report.FiledAt = now;
            report.ApprovedAt = null;
            report.CancelNote = null;
            report.UpdatedAt = now;
            await db.SaveWithConcurrencyAsync(token);

            var detail = JsonSerializer.Serialize(new { cycle = report.CurrentCycle, referenceNo = report.ReferenceNo, approvers = route.Select(r => r.Id), refile = isRefile });
            await audit.WriteAsync(new WorkflowAuditEntry(isRefile ? "ReportRefiled" : "ReportFiled", reportId, user.UserId, detail), token);

            var first = route[0];
            return new
            {
                Result = new FileReportResultDto(reportId, report.Status, report.CurrentCycle, first.FullName),
                Notification = new ReportFiledNotification(
                    reportId, report.ReferenceNo, employee.FullName, first.Id, first.FullName, first.Email, first.MobileNo,
                    report.CurrentCycle, isRefile),
            };
        }, ct);

        // Notify after commit: a delivery failure must never roll back a successful filing.
        try
        {
            await notifier.NotifyReportFiledAsync(outcome.Notification, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Filing notification failed for report {ReportId}", reportId);
        }

        return outcome.Result;
    }

    public async Task<ExpenseReportDetailDto> ReopenAsync(Guid reportId, CancellationToken ct = default)
    {
        await tx.ExecuteAsync(async token =>
        {
            var report = await gate.LoadOwnedAsync(reportId, token);
            if (!ReportWorkflowRules.CanReopen(report.Status))
            {
                throw new ConflictException($"A report in status '{report.Status}' cannot be reopened.");
            }

            var now = clock.UtcNow;
            var pending = await db.ApprovalSteps
                .Where(s => s.ReportId == reportId && s.Cycle == report.CurrentCycle && s.Status == ApprovalStatus.Pending)
                .ToListAsync(token);
            foreach (var step in pending)
            {
                step.Status = ApprovalStatus.Superseded;
                step.ActedAt = now;
            }

            // Approved reports already have Finance tracking; pulling the report back withdraws it.
            var finance = await db.FinanceTrackings.FirstOrDefaultAsync(f => f.ReportId == reportId, token);
            if (finance is not null)
            {
                db.FinanceTrackings.Remove(finance);
            }

            var previous = report.Status;
            report.Status = ReportStatus.New;
            report.ApprovedAt = null;
            report.UpdatedAt = now;
            await db.SaveWithConcurrencyAsync(token);

            var detail = JsonSerializer.Serialize(new { from = previous.ToString(), cycle = report.CurrentCycle, supersededSteps = pending.Count });
            await audit.WriteAsync(new WorkflowAuditEntry("ReportReopened", reportId, user.UserId, detail), token);
        }, ct);

        return await loader.LoadAsync(reportId, ct);
    }
}
