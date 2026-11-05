using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// A made-up R47 for the designer's preview, so a layout can be checked before any real report exists. It
/// holds no real person or report and exercises what a layout must hold: several dates and Work With
/// groups, particulars and invoice numbers that wrap, the main amount columns, enough lines for a second page,
/// a cash advance, and the filed / endorsed / approved stamps.
/// </summary>
public static class R47SampleReport
{
    public static ExpenseReportDocumentDto Create(R47Format format)
    {
        var sort = 0;
        R47Line Line(int day, string location, string? workWith, string particulars, string? invoice, ExpenseCategory category, decimal amount, decimal vat = 0m) =>
            new(new DateOnly(2026, 1, day), particulars, invoice, ExpenseType.Local, category, amount, vat, amount + vat, location, null, null, null, workWith, ++sort);

        var lines = new List<R47Line>();
        for (var week = 0; week < 3; week++)
        {
            var day = 5 + week * 7;
            lines.AddRange(
            [
                Line(day, "Sample Medical Center", "Ana B Cruz", "OFFICE - SAMPLE MEDICAL CENTER (TOLL)", "1000123456", ExpenseCategory.Toll, 84m),
                Line(day, "Sample Medical Center", "Ana B Cruz", "SAMPLE MEDICAL CENTER - OFFICE (TOLL)", "1000123457", ExpenseCategory.Toll, 84m),
                Line(day, "Sample Medical Center", "Ana B Cruz", "PARKING", null, ExpenseCategory.Parking, 60m),
                Line(day, "Sample Medical Center", "Ana B Cruz", "MEAL", null, ExpenseCategory.Meals, 250m),
                Line(day + 1, "Sample Diagnostic Laboratory - Main Branch - Analyzer AU480 (SN: 2022054754)", null,
                    "PREVENTIVE MAINTENANCE OF THE CHEMISTRY ANALYZER, INCLUDING CALIBRATION AND QUALITY CONTROL RUNS",
                    "SI-0001234567-89", ExpenseCategory.Transportation, 350m),
                Line(day + 1, "Sample Diagnostic Laboratory - Main Branch - Analyzer AU480 (SN: 2022054754)", null,
                    "LUNCH WITH THE LABORATORY HEAD", "OR-778899", ExpenseCategory.Representation, 1200m, 144m),
                Line(day + 2, "Office", null, "COURIER OF SERVICE REPORTS", "CR-5521", ExpenseCategory.Others, 180m),
            ]);
        }

        return new ExpenseReportDocumentDto(
            Guid.Empty,
            "SAMPLE-R47",
            "Sample Employee",
            "Sample report for the R47 designer (01/01/2026 - 01/31/2026)",
            RevolvingFundAmount: 0m,
            CashAdvances: [new R47CashAdvanceRow(new DateOnly(2026, 1, 2), "Cash advance voucher", "CA-0001", 5000m)],
            new R47Signatories(
                "Sample Endorser", "FINANCE", "Sample Approver",
                EndorsedAt: new DateTimeOffset(2026, 2, 2, 1, 30, 0, TimeSpan.Zero),
                ApprovedAt: new DateTimeOffset(2026, 2, 3, 6, 5, 0, TimeSpan.Zero)),
            EmployeeSignature: null,
            lines,
            FiledAt: new DateTimeOffset(2026, 2, 1, 9, 0, 0, TimeSpan.Zero),
            Format: format);
    }
}
