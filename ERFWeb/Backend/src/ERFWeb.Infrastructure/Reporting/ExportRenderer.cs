using System.Globalization;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Finance;
using ERFWeb.Domain.Reporting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// Turns summaries and queue snapshots into CSV or PDF files. Pure formatting: no data access and
/// no authorization (the <see cref="ExportService"/> does both). Timestamps print in company local
/// time (<see cref="R47ReportOptions.UtcOffsetHours"/>).
/// </summary>
public sealed class ExportRenderer
{
    private const string CsvType = "text/csv";
    private const string PdfType = "application/pdf";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly R47ReportOptions _opt;

    public ExportRenderer(IOptions<R47ReportOptions> options)
    {
        _opt = options.Value;
        QuestPdfSetup.Apply(_opt);
    }

    // ---------- Expense summary ----------

    /// <summary>CSV: one row per expense line (ready to pivot). PDF: employee totals by category, then the lines.</summary>
    public ExportFile ExpenseSummaryReport(ExpenseSummaryReport report, ExportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);
        var q = report.Query;
        var s = report.Summary;
        var name = $"ExpenseSummary_{q.From:yyyy-MM-dd}_{q.To:yyyy-MM-dd}";

        if (format == ExportFormat.Csv)
        {
            var csv = new CsvBuilder().Row("Expense date", "Employee", "Department", "ERF reference", "Report status", "Category", "Particulars", "Location", "Amount");
            foreach (var l in s.Lines)
            {
                csv.Row(l.ExpenseDate, l.EmployeeName, l.DepartmentName, l.ReferenceNo, StatusLabel(l.ReportStatus), CategoryLabel(l.Category), l.Particulars, l.Location, l.Amount);
            }

            return new ExportFile(csv.ToBytes(), CsvType, name + ".csv");
        }

        var categories = ExpenseSummaryBuilder.Categories;
        var totalsColumns = new List<PdfColumn> { new("Employee", 3), new("Department", 2.2f), new("Lines", 0.8f, true) };
        totalsColumns.AddRange(categories.Select(c => new PdfColumn(CategoryLabel(c), 1.4f, true)));
        totalsColumns.Add(new PdfColumn("Total", 1.5f, true));

        var totalsRows = s.Employees.Select(e => new[] { e.EmployeeName, e.DepartmentName ?? string.Empty, e.LineCount.ToString(Inv) }
            .Concat(categories.Select(c => Money(e.ByCategory[c])))
            .Append(Money(e.Total))
            .ToArray()).ToList();
        var grand = new[] { "Total", string.Empty, s.Lines.Count.ToString(Inv) }
            .Concat(categories.Select(c => Money(s.ByCategory[c])))
            .Append(Money(s.Total))
            .ToArray();

        var detail = new PdfTable(
            "Expense lines",
            [new("Date", 1.1f), new("Employee", 2.4f), new("ERF reference", 1.8f), new("Category", 1.4f), new("Particulars", 4), new("Location", 1.6f), new("Amount", 1.3f, true)],
            s.Lines.Select(l => new[] { Date(l.ExpenseDate), l.EmployeeName, l.ReferenceNo ?? string.Empty, CategoryLabel(l.Category), l.Particulars ?? string.Empty, l.Location ?? string.Empty, Money(l.Amount) }).ToList(),
            ["Total", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, Money(s.Total)]);

        var subtitle = new List<string>
        {
            $"Expense dates {Date(q.From)} to {Date(q.To)}",
            $"{ScopeLabel(q.Scope)} · {(q.ApprovedOnly ? "Approved reports only" : "Filed and approved reports")}" +
                (report.DepartmentName is { } dept ? $" · Department: {dept}" : string.Empty),
        };

        var pdf = new ExportPdfDocument("EXPENSE SUMMARY", subtitle,
            [new PdfTable("Totals by employee", totalsColumns, totalsRows, grand), detail],
            Stamp(report.GeneratedAt), report.GeneratedBy, _opt).GeneratePdf();
        return new ExportFile(pdf, PdfType, name + ".pdf");
    }

    // ---------- Cost center budgets ----------

    /// <summary>CSV: one row per cost center with its budget, spending, and spending per category.</summary>
    public ExportFile CostCenterBudgetReport(CostCenterBudgetReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var categories = ExpenseSummaryBuilder.Categories;
        var header = new List<object?> { "Year", "Cost center", "Name", "Department", "Budget", "Used", "Remaining", "% used", "Over budget", "Lines" };
        header.AddRange(categories.Select(c => (object?)CategoryLabel(c)));
        var csv = new CsvBuilder().Row([.. header]);
        foreach (var c in report.CostCenters)
        {
            var cells = new List<object?> { report.Year, c.Code, c.Name, c.DepartmentName, c.Budget, c.Used, c.Remaining, c.PercentUsed, c.OverBudget, c.LineCount };
            cells.AddRange(categories.Select(cat => (object?)c.ByCategory[cat]));
            csv.Row([.. cells]);
        }

        var suffix = report.ApprovedOnly ? "_ApprovedOnly" : string.Empty;
        return new ExportFile(csv.ToBytes(), CsvType, $"CostCenterBudgets_{report.Year}{suffix}.csv");
    }

    // ---------- Approver inbox ----------

    public ExportFile ApprovalInboxReport(IReadOnlyList<ApprovalInboxItemDto> items, ApprovalInboxView view, DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(items);
        var csv = new CsvBuilder().Row("ERF reference", "Employee", "Department", "Report type", "Purpose", "Report status", "Total amount",
            "Filed", "Days waiting", "My step", "My level", "My step status", "Acted", "Current approver", "My turn", "On behalf of");
        foreach (var i in items)
        {
            csv.Row(i.ReferenceNo, i.EmployeeName, i.DepartmentName, ReportTypeLabel(i.ReportType), i.Purpose, StatusLabel(i.Status), i.TotalAmount,
                Stamp(i.FiledAt), i.DaysWaiting, i.MyStepOrder, i.MyLevel is { } level ? $"Level {level}" : null, i.MyStepStatus.ToString(),
                Stamp(i.MyActedAt), i.CurrentApproverName, i.IsMyTurn, i.OnBehalfOfName);
        }

        return new ExportFile(csv.ToBytes(), CsvType, $"ApprovalInbox_{view}_{LocalDay(generatedAt)}.csv");
    }

    // ---------- Finance ----------

    public ExportFile FinanceQueueReport(IReadOnlyList<FinanceQueueItemDto> items, DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(items);
        var csv = QueueCsvHeader(withBucket: false);
        foreach (var i in items)
        {
            QueueCsvRow(csv, i, bucket: null);
        }

        return new ExportFile(csv.ToBytes(), CsvType, $"FinanceQueue_{LocalDay(generatedAt)}.csv");
    }

    /// <summary>CSV: the open items with their aging bucket. PDF: the bucket totals, then the items, longest waiting first.</summary>
    public ExportFile FinanceAgingReport(FinanceAgingDto aging, ExportFormat format, string generatedBy)
    {
        ArgumentNullException.ThrowIfNull(aging);
        var name = $"FinanceAging_{LocalDay(aging.AsOf)}";

        if (format == ExportFormat.Csv)
        {
            var csv = QueueCsvHeader(withBucket: true);
            foreach (var i in aging.Items)
            {
                QueueCsvRow(csv, i, FinanceAging.Label(FinanceAging.BucketFor(i.DaysSinceApproval)));
            }

            return new ExportFile(csv.ToBytes(), CsvType, name + ".csv");
        }

        var buckets = new PdfTable(
            "Open reports by days since approval",
            [new("Days since approval", 3), new("Reports", 1, true), new("Amount", 1.5f, true)],
            aging.Buckets.Select(b => new[] { b.Label, b.Count.ToString(Inv), Money(b.Amount) }).ToList(),
            ["Total", aging.Count.ToString(Inv), Money(aging.Amount)]);

        var items = new PdfTable(
            "Open reports, longest waiting first",
            [new("ERF reference", 1.8f), new("Employee", 2.4f), new("Department", 1.8f), new("Approved", 1.2f), new("Days", 0.7f, true),
                new("Aging", 1.2f), new("Receipts", 1.6f), new("Follow-ups", 0.9f, true), new("Amount", 1.3f, true)],
            aging.Items.Select(i => new[]
            {
                i.ReferenceNo ?? string.Empty, i.EmployeeName, i.DepartmentName ?? string.Empty, LocalDate(i.ApprovedAt),
                i.DaysSinceApproval.ToString(Inv), FinanceAging.Label(FinanceAging.BucketFor(i.DaysSinceApproval)),
                FinanceStatusLabel(i.Status), i.FollowUpCount.ToString(Inv), Money(i.TotalAmount),
            }).ToList());

        var pdf = new ExportPdfDocument("FINANCE AGING", [$"Open reports as of {Stamp(aging.AsOf)} (not yet cleared for payment)"],
            [buckets, items], Stamp(aging.AsOf), generatedBy, _opt).GeneratePdf();
        return new ExportFile(pdf, PdfType, name + ".pdf");
    }

    private static CsvBuilder QueueCsvHeader(bool withBucket)
    {
        var header = new List<object?> { "ERF reference", "Employee", "Department", "Report type", "Purpose", "Total amount", "Approved", "Days since approval" };
        if (withBucket)
        {
            header.Add("Aging");
        }

        header.AddRange(["Receipts status", "Remarks", "Received by", "Received", "Cleared by", "Cleared", "Follow-ups", "Last follow-up",
            "Attachments", "Open verifications"]);
        return new CsvBuilder().Row([.. header]);
    }

    private void QueueCsvRow(CsvBuilder csv, FinanceQueueItemDto i, string? bucket)
    {
        var cells = new List<object?> { i.ReferenceNo, i.EmployeeName, i.DepartmentName, ReportTypeLabel(i.ReportType), i.Purpose, i.TotalAmount,
            Stamp(i.ApprovedAt), i.DaysSinceApproval };
        if (bucket is not null)
        {
            cells.Add(bucket);
        }

        cells.AddRange([FinanceStatusLabel(i.Status), i.Remarks, i.ReceivedByName, Stamp(i.ReceivedAt), i.ClearedByName, Stamp(i.ClearedAt),
            i.FollowUpCount, Stamp(i.LastFollowUpAt), i.AttachmentCount, i.OpenVerifications]);
        csv.Row([.. cells]);
    }

    // ---------- Formatting ----------

    private DateTimeOffset Local(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(_opt.UtcOffsetHours));

    private string? Stamp(DateTimeOffset? value) => value is { } v ? Local(v).ToString("yyyy-MM-dd HH:mm", Inv) : null;

    private string Stamp(DateTimeOffset value) => Local(value).ToString("yyyy-MM-dd HH:mm", Inv);

    private string LocalDate(DateTimeOffset? value) => value is { } v ? Local(v).ToString("yyyy-MM-dd", Inv) : string.Empty;

    private string LocalDay(DateTimeOffset value) => Local(value).ToString("yyyyMMdd", Inv);

    private static string Date(DateOnly value) => value.ToString("yyyy-MM-dd", Inv);

    private static string Money(decimal value) => value.ToString("N2", Inv);

    internal static string CategoryLabel(ExpenseCategory category) => category.ToString();

    internal static string StatusLabel(ReportStatus status) => status switch
    {
        ReportStatus.ForApproval => "For approval",
        _ => status.ToString(),
    };

    private static string ReportTypeLabel(ReportType type) => type switch
    {
        ReportType.RevolvingFund => "Revolving fund",
        ReportType.CashAdvanceLiquidation => "Liquidation of cash advance",
        _ => "Reimbursement",
    };

    private static string FinanceStatusLabel(FinanceStatus status) => status switch
    {
        FinanceStatus.Pending => "Receipts not yet received",
        FinanceStatus.ReceiptsIncomplete => "Receipts incomplete",
        FinanceStatus.ReceiptsReceived => "Receipts received",
        _ => "Cleared for payment",
    };

    private static string ScopeLabel(SummaryScope scope) => scope switch
    {
        SummaryScope.Mine => "My reports",
        SummaryScope.Approvals => "Reports I sign for",
        _ => "All employees",
    };
}

/// <summary>Applies the configured QuestPDF license tier once per renderer.</summary>
internal static class QuestPdfSetup
{
    public static void Apply(R47ReportOptions options) =>
        QuestPDF.Settings.License = Enum.TryParse<QuestPDF.Infrastructure.LicenseType>(options.QuestPdfLicense, ignoreCase: true, out var license)
            ? license
            : QuestPDF.Infrastructure.LicenseType.Community;
}
