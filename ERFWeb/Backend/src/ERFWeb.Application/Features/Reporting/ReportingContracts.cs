using ERFWeb.Domain.Reporting;

namespace ERFWeb.Application.Features.Reporting;

/// <summary>
/// The signatory names printed in the R47 footer, with the signature each approver
/// gave when approving (null until that step is approved).
/// </summary>
public sealed record R47Signatories(
    string? EndorsedBy,
    string? ReviewedBy,
    string? ApprovedBy,
    byte[]? EndorserSignature = null,
    byte[]? ApproverSignature = null);

/// <summary>One row of the CASH ADVANCE RECEIVED block.</summary>
public sealed record R47CashAdvanceRow(
    DateOnly? AdvanceDate,
    string? ReferenceDocument,
    string? ReferenceNo,
    decimal Amount);

/// <summary>
/// Everything needed to render the R47 "EXPENSE REPORT" for one ERF. Produced by the
/// LINQ loader; consumed by the PDF renderer. Contains no EF entities.
/// </summary>
public sealed record ExpenseReportDocumentDto(
    Guid ReportId,
    string? ReferenceNo,
    string EmployeeFullName,
    string Purpose,
    decimal RevolvingFundAmount,
    IReadOnlyList<R47CashAdvanceRow> CashAdvances,
    R47Signatories Signatories,
    byte[]? EmployeeSignature,
    IReadOnlyList<R47Line> Lines)
{
    /// <summary>The TOTAL of the CASH ADVANCE RECEIVED block; the reconciliation reduces from this.</summary>
    public decimal CashAdvanceTotal => CashAdvances.Sum(c => c.Amount);
}

/// <summary>Loads the R47 document model for a report (LINQ read model).</summary>
public interface IExpenseReportDocumentLoader
{
    Task<ExpenseReportDocumentDto> LoadAsync(Guid reportId, CancellationToken ct = default);
}

/// <summary>Renders an R47 document model to PDF bytes.</summary>
public interface IExpenseReportPdfRenderer
{
    byte[] Render(ExpenseReportDocumentDto document, DateTimeOffset printedAt);
}

/// <summary>Report-level authorization: may the current user view this report?</summary>
public interface IReportAccessPolicy
{
    Task<bool> CanViewAsync(Guid reportId, CancellationToken ct = default);
}
