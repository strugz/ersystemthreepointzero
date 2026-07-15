namespace ERSystem.Web.Infrastructure.Persistence;

public sealed class UserRegistrationEntity
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
    public string? UserLevel { get; set; }
    public int? DepartmentId { get; set; }
    public byte[]? Signature { get; set; }
    public int? ReportNumberStatus { get; set; }
}

public sealed class DepartmentEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public sealed class UserAuthorityEntity
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public int? AuthorityId { get; set; }
    public string? AuthorityName { get; set; }
    public int? Sort { get; set; }
}

public sealed class ReportDetailEntity
{
    public string Id { get; set; } = string.Empty;
    public DateOnly? ReportDateFrom { get; set; }
    public DateOnly? ReportDateTo { get; set; }
    public string? ReportDescription { get; set; }
    public int? UserId { get; set; }
    public string? ReportEndorseStatus { get; set; }
    public string? ReportFileStatus { get; set; }
    public string? ReportPrintStatus { get; set; }
    public string? ReportReturnedForModification { get; set; }
    public int? ReportNumberStatus { get; set; }
    public string? ReportReserveStatus1 { get; set; }
    public string? ReportReserveStatus2 { get; set; }
    public string? ReportCancelNote { get; set; }
    public string? ReportAttachment { get; set; }
    public string? ReportType { get; set; }
    public string? ErfReferenceNumber { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ExpenseDetailEntity
{
    public long? Id { get; set; }
    public DateOnly? TransactionDate { get; set; }
    public string? Particulars { get; set; }
    public string? Category { get; set; }
    public double? Amount { get; set; }
    public string? Remarks { get; set; }
    public double? TotalAmount { get; set; }
    public string? Location { get; set; }
    public string? ReportId { get; set; }
    public int? Sort { get; set; }
}

public sealed class CashAdvanceEntity
{
    public int Id { get; set; }
    public string? ReportId { get; set; }
    public double? Amount { get; set; }
    public string? Date { get; set; }
    public string? ReferenceDocument { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? RevolvingFund { get; set; }
}

public sealed class ReportAuthorityEntity
{
    public long Id { get; set; }
    public string? ReportId { get; set; }
    public long? SignId { get; set; }
    public long? UserId { get; set; }
    public byte[]? AuthoritySignature { get; set; }
}

public sealed class ReportApprovalTransactionEntity
{
    public long Id { get; set; }
    public string ReportId { get; set; } = string.Empty;
    public int ApprovalCycle { get; set; }
    public int EmployeeUserId { get; set; }
    public int ApproverUserId { get; set; }
    public int StepOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? ActionedAtUtc { get; set; }
    public string? ActionRemarks { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ReportFinanceTrackingEntity
{
    public long Id { get; set; }
    public string? ReportId { get; set; }
    public string? FinanceStatus { get; set; }
    public bool PhysicalReceiptsReceived { get; set; }
    public int? PhysicalReceiptsReceivedBy { get; set; }
    public DateTime? PhysicalReceiptsReceivedDate { get; set; }
    public string? FinanceRemarks { get; set; }
    public DateTime? ScannedReceiptsDeletedDate { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ScannedReceiptAttachmentEntity
{
    public int Id { get; set; }
    public string ReportId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public byte[] ReceiptContent { get; set; } = [];
    public DateTime CreatedDate { get; set; }
}

public sealed class WebLoginSecurityEntity
{
    public int UserId { get; set; }
    public int FailedAttemptCount { get; set; }
    public DateTime? FirstFailedAttemptUtc { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastSuccessfulLoginUtc { get; set; }
}

public sealed class WebWorkflowAuditEntity
{
    public long Id { get; set; }
    public string ReportId { get; set; } = string.Empty;
    public int ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PreviousState { get; set; }
    public string? NewState { get; set; }
    public string? Remarks { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid CorrelationId { get; set; }
}
