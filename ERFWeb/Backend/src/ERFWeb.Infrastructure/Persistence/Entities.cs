using ERFWeb.Domain;

namespace ERFWeb.Infrastructure.Persistence;

public class Department
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? MobileNo { get; set; }
    public long? DepartmentId { get; set; }
    public UserRole Role { get; set; } = UserRole.Employee;
    public byte[]? Signature { get; set; }
    public bool WorkWithEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public class ApprovalRoute
{
    public long Id { get; set; }
    public long EmployeeUserId { get; set; }
    public int StepOrder { get; set; }
    public long ApproverUserId { get; set; }
}

public class DepartmentSignatory
{
    public long DepartmentId { get; set; }
    public string? EndorserInitials { get; set; }
    public string? ReviewerInitials { get; set; }
    public string? ApproverInitials { get; set; }
    public byte[]? EndorserSignature { get; set; }
    public byte[]? ReviewerSignature { get; set; }
    public byte[]? ApproverSignature { get; set; }
}

public class EmployeeRate
{
    public long UserId { get; set; }
    public decimal TransportRate { get; set; }
    public decimal BreakfastRate { get; set; }
    public decimal LunchRate { get; set; }
    public decimal DinnerRate { get; set; }
    public decimal OtMealRate { get; set; }
}

public class ExpenseReport
{
    public Guid Id { get; set; }
    public string? ReferenceNo { get; set; }
    public long EmployeeUserId { get; set; }
    public ReportType ReportType { get; set; }
    public string? Purpose { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.New;
    public decimal RevolvingFundAmount { get; set; }
    public int CurrentCycle { get; set; } = 1;
    public DateTimeOffset? FiledAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? CancelNote { get; set; }
    public bool SentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>One row of the R47 "CASH ADVANCE RECEIVED" block (Liquidation of Cash Advance only).</summary>
public class CashAdvanceLine
{
    public long Id { get; set; }
    public Guid ReportId { get; set; }
    public DateOnly? AdvanceDate { get; set; }
    public string? ReferenceDocument { get; set; }
    public string? ReferenceNo { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class ExpenseLine
{
    public long Id { get; set; }
    public Guid ReportId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? Particulars { get; set; }
    public string? WorkWith { get; set; }
    public string? InvoiceOrNo { get; set; }
    public ExpenseType ExpenseType { get; set; } = ExpenseType.Local;
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Location { get; set; }
    public string? ServiceNumber { get; set; }
    public string? Instrument { get; set; }
    public string? SerialNumber { get; set; }
    public bool IsPerdiem { get; set; }
    public int Multiplier { get; set; } = 1;
    public int? MinusDays { get; set; }
    public int? TotalDays { get; set; }
    public string? Computation { get; set; }
    public string? Remarks { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class ExpenseMealItem
{
    public long Id { get; set; }
    public long ExpenseLineId { get; set; }
    public string? MealType { get; set; }
    public string? PaidFor { get; set; }
    public long? PaidEmployeeId { get; set; }
}

public class ExpenseTransportItem
{
    public long Id { get; set; }
    public long ExpenseLineId { get; set; }
    public string? FareFrom { get; set; }
    public string? FareTo { get; set; }
    public decimal FareAmount { get; set; }
}

public class ApprovalStep
{
    public long Id { get; set; }
    public Guid ReportId { get; set; }
    public int Cycle { get; set; } = 1;
    public int StepOrder { get; set; }
    public long ApproverUserId { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTimeOffset? ActedAt { get; set; }
    public byte[]? Signature { get; set; }
    public string? Note { get; set; }
}

public class FinanceTracking
{
    public Guid ReportId { get; set; }
    public FinanceStatus Status { get; set; } = FinanceStatus.Pending;
    public bool PhysicalReceiptsReceived { get; set; }
    public long? ReceivedByUserId { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public string? Remarks { get; set; }
}

public class ReceiptAttachment
{
    public long Id { get; set; }
    public Guid ReportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
    public long UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

public class ReminderDelivery
{
    public long Id { get; set; }
    public Guid ReportId { get; set; }
    public long? StepId { get; set; }
    public int ReminderNumber { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SentAt { get; set; }
    public string? FailureCode { get; set; }
}

public class LoginSecurity
{
    public string Username { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
}

public class WorkflowAudit
{
    public long Id { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid? ReportId { get; set; }
    public string Event { get; set; } = string.Empty;
    public long? ActorUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Detail { get; set; }
}

public class RecordHistory
{
    public long Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string RowPk { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public long? ChangedBy { get; set; }
    public string? OldData { get; set; }
    public string? NewData { get; set; }
}

/// <summary>Per-year counter behind the ERF reference number series (<c>ERF-2026-000142</c>).</summary>
public class ReferenceSeries
{
    public string SeriesKey { get; set; } = string.Empty;
    public int NextValue { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; }
}
