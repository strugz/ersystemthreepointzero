using ERFWeb.Domain.Reporting;

namespace ERFWeb.Application.Features.Reporting;

/// <summary>
/// The signatory names printed in the R47 footer, with when each step was approved (null until it is)
/// and the signature image to print, which is null unless the R47 signature rule lets it print.
/// </summary>
public sealed record R47Signatories(
    string? EndorsedBy,
    string? ReviewedBy,
    string? ApprovedBy,
    byte[]? EndorserSignature = null,
    byte[]? ApproverSignature = null,
    DateTimeOffset? EndorsedAt = null,
    DateTimeOffset? ApprovedAt = null);

/// <summary>One row of the CASH ADVANCE RECEIVED block.</summary>
public sealed record R47CashAdvanceRow(
    DateOnly? AdvanceDate,
    string? ReferenceDocument,
    string? ReferenceNo,
    decimal Amount);

/// <summary>
/// Everything needed to render the R47 "EXPENSE REPORT" for one ERF. Produced by the
/// LINQ loader; consumed by the PDF renderer. Contains no EF entities. <see cref="Format"/> is the
/// Rules page R47 format (engine and header details); null prints with the server's R47Report settings.
/// <see cref="Design"/> is the R47 designer's layout; null prints the standard form.
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
    IReadOnlyList<R47Line> Lines,
    DateTimeOffset? FiledAt = null,
    R47Format? Format = null,
    R47Design? Design = null)
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
