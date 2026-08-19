namespace ERSystem.Web.Application.Features.ReportReview;

public sealed record ExpenseLineDto(
    long? Id, DateOnly? TransactionDate, bool IsPerDiem, string Particulars,
    string InvoiceNumber, int? Multiplier, string ExpenseType, string Category,
    decimal Amount, decimal? VatAmount, decimal TotalAmount, string Location,
    string Remarks, string WorkWith, string ServiceNumber, string Instrument,
    string SerialNumber, string MinusDays, string TotalDays, string Computation);

public sealed record CashAdvanceDto(
    decimal? Amount,
    string Date,
    string ReferenceDocument,
    string ReferenceNumber,
    string RevolvingFund);

public sealed record ReceiptAttachmentDto(
    int Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime CreatedDateUtc);

public sealed record ApprovalTrailItemDto(
    int ApproverUserId,
    string ApproverName,
    int Sort,
    DateTime? OccurredAtUtc,
    string Status);

public sealed record AttachmentContentDto(string FileName, string ContentType, byte[] Content);
