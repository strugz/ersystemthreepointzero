using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Reporting;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Exports (plan Phase 7). Data comes from the same services the screens use, so each export sees
/// exactly what its screen would; the API policies (Approver, Finance) guard the queue exports and
/// the summary service checks the summary scope. Every export is written to the workflow audit
/// trail with its filters and row count, never its contents.
/// </summary>
public sealed class ExportService(
    IExpenseSummaryService summaries,
    ICostCenterBudgetService budgets,
    IApprovalService approvals,
    IFinanceService finance,
    ExportRenderer renderer,
    ICurrentUser user,
    IClock clock,
    IWorkflowAuditWriter audit) : IExportService
{
    public async Task<ExportFile> ExpenseSummaryAsync(ExpenseSummaryQuery query, ExportFormat format, CancellationToken ct = default)
    {
        var report = await summaries.BuildAsync(query, ct);
        var file = renderer.ExpenseSummaryReport(report, format);
        await AuditAsync("ExpenseSummary", format, report.Summary.Lines.Count,
            new { from = query.From, to = query.To, scope = query.Scope.ToString(), query.DepartmentId, query.ApprovedOnly }, ct);
        return file;
    }

    public async Task<ExportFile> CostCenterBudgetsAsync(CostCenterBudgetQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var report = await budgets.BuildAsync(query, ct);
        var file = renderer.CostCenterBudgetReport(report);
        await AuditAsync("CostCenterBudgets", ExportFormat.Csv, report.CostCenters.Count, new { query.Year, query.ApprovedOnly }, ct);
        return file;
    }

    public async Task<ExportFile> ApprovalInboxAsync(ApprovalInboxView view, string? search, CancellationToken ct = default)
    {
        var items = await approvals.InboxExportAsync(view, search, ct);
        if (items.Count > IExportService.MaxRows)
        {
            throw new ValidationException("search", $"The inbox has more than {IExportService.MaxRows:N0} reports. Narrow it with a search.");
        }

        var file = renderer.ApprovalInboxReport(items, view, clock.UtcNow);
        await AuditAsync("ApprovalInbox", ExportFormat.Csv, items.Count, new { view = view.ToString(), search }, ct);
        return file;
    }

    public async Task<ExportFile> FinanceQueueAsync(FinanceQueueQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var items = await finance.QueueExportAsync(query, IExportService.MaxRows, ct);
        if (items.Count > IExportService.MaxRows)
        {
            throw new ValidationException("search", $"The queue has more than {IExportService.MaxRows:N0} reports. Filter by status or search.");
        }

        var file = renderer.FinanceQueueReport(items, clock.UtcNow);
        await AuditAsync("FinanceQueue", ExportFormat.Csv, items.Count,
            new { status = query.Status?.ToString(), query.OpenOnly, query.Search }, ct);
        return file;
    }

    public async Task<ExportFile> FinanceAgingAsync(ExportFormat format, CancellationToken ct = default)
    {
        var aging = await finance.AgingAsync(ct);
        var file = renderer.FinanceAgingReport(aging, format, user.FullName);
        await AuditAsync("FinanceAging", format, aging.Count, new { }, ct);
        return file;
    }

    private Task AuditAsync(string export, ExportFormat format, int rows, object filters, CancellationToken ct) =>
        audit.WriteAsync(new WorkflowAuditEntry(ExportAuditEvents.Generated, null, user.UserId,
            JsonSerializer.Serialize(new { export, format = format.ToString(), rows, filters })), ct);
}
