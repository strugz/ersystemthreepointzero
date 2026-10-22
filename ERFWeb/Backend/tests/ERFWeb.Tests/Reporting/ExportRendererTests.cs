using System.Text;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Finance;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Reporting;
using Microsoft.Extensions.Options;
using Xunit;

namespace ERFWeb.Tests.Reporting;

public class ExportRendererTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 1, 30, 0, TimeSpan.Zero);

    private static ExportRenderer Renderer() => new(Options.Create(new R47ReportOptions()));

    private static ExpenseSummaryReport Summary(params SummaryLine[] lines) => new(
        new ExpenseSummaryQuery(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), SummaryScope.All),
        null, Now, "Finance User", ExpenseSummaryBuilder.Build(lines));

    private static SummaryLine Line(string particulars, decimal amount) =>
        new(1, "Ann Cruz", "Sales", "ERF-2026-000001", ReportStatus.ForApproval, new DateOnly(2026, 10, 5),
            ExpenseCategory.Meals, particulars, "MDMPI", amount);

    private static string Text(ExportFile file) => Encoding.UTF8.GetString(file.Content, 3, file.Content.Length - 3);

    private static FinanceQueueItemDto QueueItem(string reference, int days, decimal amount) => new(
        Guid.NewGuid(), reference, 1, "Ann Cruz", "Sales", ReportType.Reimbursement, "Trip", amount, Now.AddDays(-days),
        FinanceStatus.Pending, null, null, null, null, null, 0, null, 2, days, false, 1);

    [Fact]
    public void SummaryCsv_StartsWithBom_AndHasOneRowPerLine()
    {
        var file = Renderer().ExpenseSummaryReport(Summary(Line("MEAL", 250m), Line("MEAL", 1601.5m)), ExportFormat.Csv);

        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("ExpenseSummary_2026-10-01_2026-10-31.csv", file.FileName);
        Assert.Equal(Encoding.UTF8.GetPreamble(), file.Content.Take(3));

        var rows = Text(file).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, rows.Length);
        Assert.StartsWith("Expense date,Employee,", rows[0]);
        Assert.Equal("2026-10-05,Ann Cruz,Sales,ERF-2026-000001,For approval,Meals,MEAL,MDMPI,1601.50", rows[2]);
    }

    [Fact]
    public void SummaryCsv_QuotesSeparators_AndNeutralizesFormulas()
    {
        var file = Renderer().ExpenseSummaryReport(Summary(Line("=HYPERLINK(\"x\")", 1m), Line("Lunch, client \"A\"", 2m)), ExportFormat.Csv);
        var text = Text(file);

        Assert.Contains("\"'=HYPERLINK(\"\"x\"\")\"", text, StringComparison.Ordinal);
        Assert.Contains("\"Lunch, client \"\"A\"\"\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryPdf_IsAPdf_EvenWhenEmpty()
    {
        foreach (var report in new[] { Summary(), Summary(Line("MEAL", 250m)) })
        {
            var file = Renderer().ExpenseSummaryReport(report, ExportFormat.Pdf);
            Assert.Equal("application/pdf", file.ContentType);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(file.Content, 0, 4));
        }
    }

    [Fact]
    public void FinanceAgingCsv_CarriesTheBucket_AndPdfRenders()
    {
        var items = new[] { QueueItem("ERF-A", 45, 100m), QueueItem("ERF-B", 3, 50m) };
        var aging = new FinanceAgingDto(Now, [new(AgingBucket.Days0To7, "0-7 days", 1, 50m)], 2, 150m, items);

        var csv = Renderer().FinanceAgingReport(aging, ExportFormat.Csv, "Finance User");
        var rows = Text(csv).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("FinanceAging_20261022.csv", csv.FileName);
        Assert.Contains("Aging", rows[0].Split(','));
        Assert.Contains("31-60 days", rows[1], StringComparison.Ordinal);

        var pdf = Renderer().FinanceAgingReport(aging, ExportFormat.Pdf, "Finance User");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf.Content, 0, 4));
    }

    [Fact]
    public void FinanceQueueCsv_PrintsTimesInCompanyLocalTime()
    {
        var file = Renderer().FinanceQueueReport([QueueItem("ERF-A", 0, 10m)], Now);
        var row = Text(file).Split("\r\n")[1];

        // 01:30 UTC is 09:30 in Manila (UtcOffsetHours = 8).
        Assert.Contains("2026-10-22 09:30", row, StringComparison.Ordinal);
    }
}
