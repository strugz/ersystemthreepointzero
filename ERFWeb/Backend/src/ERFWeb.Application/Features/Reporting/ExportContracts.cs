using ERFWeb.Application.Features.Approvals;
using ERFWeb.Domain;
using ERFWeb.Domain.Finance;
using ERFWeb.Domain.Reporting;

namespace ERFWeb.Application.Features.Reporting;

public enum ExportFormat
{
    Csv,
    Pdf,
}

/// <summary>Whose reports an expense summary covers. The service checks the caller may use the scope.</summary>
public enum SummaryScope
{
    /// <summary>The caller's own reports. Anyone.</summary>
    Mine,

    /// <summary>Reports with an approval step the caller holds or acted on. Signatories.</summary>
    Approvals,

    /// <summary>Every employee's reports. Finance.</summary>
    All,
}

/// <summary>
/// An expense summary request. Lines are selected by expense date within From..To. Reports that are
/// filed (For Approval) or approved count; drafts, returned, and rejected reports never do.
/// </summary>
public sealed record ExpenseSummaryQuery(
    DateOnly From,
    DateOnly To,
    SummaryScope Scope = SummaryScope.Mine,
    long? DepartmentId = null,
    bool ApprovedOnly = false);

/// <summary>A built expense summary plus what produced it (printed in the export header).</summary>
public sealed record ExpenseSummaryReport(
    ExpenseSummaryQuery Query,
    string? DepartmentName,
    DateTimeOffset GeneratedAt,
    string GeneratedBy,
    ExpenseSummary Summary);

/// <summary>The on-screen expense summary: employee and category totals, without the line detail.</summary>
public sealed record ExpenseSummaryDto(
    DateOnly From,
    DateOnly To,
    SummaryScope Scope,
    string? DepartmentName,
    bool ApprovedOnly,
    int LineCount,
    IReadOnlyList<SummaryEmployeeTotals> Employees,
    IReadOnlyDictionary<ExpenseCategory, decimal> ByCategory,
    decimal Total)
{
    public static ExpenseSummaryDto Create(ExpenseSummaryReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var q = report.Query;
        var s = report.Summary;
        return new ExpenseSummaryDto(q.From, q.To, q.Scope, report.DepartmentName, q.ApprovedOnly, s.Lines.Count, s.Employees, s.ByCategory, s.Total);
    }
}

public interface IExpenseSummaryService
{
    Task<ExpenseSummaryReport> BuildAsync(ExpenseSummaryQuery query, CancellationToken ct = default);
}

public sealed record FinanceAgingBucketDto(AgingBucket Bucket, string Label, int Count, decimal Amount);

/// <summary>Open Finance queue items (not yet cleared for payment) by days since final approval.</summary>
public sealed record FinanceAgingDto(
    DateTimeOffset AsOf,
    IReadOnlyList<FinanceAgingBucketDto> Buckets,
    int Count,
    decimal Amount,
    IReadOnlyList<FinanceQueueItemDto> Items);

/// <summary>A generated export, ready to stream as a download.</summary>
public sealed record ExportFile(byte[] Content, string ContentType, string FileName);

/// <summary>
/// Builds the downloadable summaries and queue exports (plan Phase 7). Each export applies the same
/// authorization as the screen it comes from and is recorded in the workflow audit trail.
/// </summary>
public interface IExportService
{
    /// <summary>The most rows one export returns; a larger result asks the caller to narrow it.</summary>
    const int MaxRows = 10_000;

    Task<ExportFile> ExpenseSummaryAsync(ExpenseSummaryQuery query, ExportFormat format, CancellationToken ct = default);
    Task<ExportFile> CostCenterBudgetsAsync(CostCenterBudgetQuery query, CancellationToken ct = default);
    Task<ExportFile> ApprovalInboxAsync(ApprovalInboxView view, string? search, CancellationToken ct = default);
    Task<ExportFile> FinanceQueueAsync(FinanceQueueQuery query, CancellationToken ct = default);
    Task<ExportFile> FinanceAgingAsync(ExportFormat format, CancellationToken ct = default);
}

/// <summary>Audit event name for exports (the detail names the export and its filters).</summary>
public static class ExportAuditEvents
{
    public const string Generated = "ExportGenerated";
}
