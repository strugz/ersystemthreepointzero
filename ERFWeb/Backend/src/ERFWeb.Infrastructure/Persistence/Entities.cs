using ERFWeb.Domain;

namespace ERFWeb.Infrastructure.Persistence;

public class Department
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CostCenter
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>A cost center's overall budget for one calendar year (monitoring only).</summary>
public class CostCenterBudget
{
    public long CostCenterId { get; set; }
    public int BudgetYear { get; set; }
    public decimal Amount { get; set; }
    public long? UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class Currency
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>How a fare leg was travelled (Taxi, Grab, Jeepney, ...). Replaces the legacy dbo.tbFare list.</summary>
public class FareMode
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Recorded for Finance (usually an official receipt); not enforced.</summary>
    public bool ReceiptExpected { get; set; }

    /// <summary>The employee types the mode ("Others").</summary>
    public bool RequiresDetail { get; set; }

    /// <summary>The matching dbo.tbFare.id, for traceability to the desktop list.</summary>
    public int? LegacyFareId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class Site
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>Key/value store for the System Administrator's workflow, business, and notification rules.</summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public long? UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// One additive role on an account (RBAC v0.6). A grant is Active until revoked; the countersign
/// and bootstrap columns are history from the dual-approval period. The Employee base role is never stored here.
/// </summary>
public class UserRoleGrant
{
    public long UserId { get; set; }
    public UserRole Role { get; set; }
    public RoleGrantStatus Status { get; set; } = RoleGrantStatus.Active;
    public long? GrantedByUserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public long? CountersignedByUserId { get; set; }
    public DateTimeOffset? CountersignedAt { get; set; }
    public long? RevokedByUserId { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsBootstrap { get; set; }
    public string? Note { get; set; }
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
    public string? JobGrade { get; set; }
    public long? DepartmentId { get; set; }
    public long? CostCenterId { get; set; }
    public byte[]? Signature { get; set; }
    public bool WorkWithEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>The CRM employee ID (e.g. CNT-001) this account is linked to; null until an admin links it.</summary>
    public string? CrmEmployeeId { get; set; }

    /// <summary>This employee's own R47 REVIEWED BY; null follows the department, then the Rules default.</summary>
    public string? R47ReviewedBy { get; set; }

    /// <summary>Set when an administrator sets the password (new account, reset); cleared when the user changes it.</summary>
    public bool MustChangePassword { get; set; }
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

    /// <summary>The employee's department when the report was last filed; its cost center counts the spending.</summary>
    public long? DepartmentId { get; set; }

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

    /// <summary>The CRM activityId (ITI-/SR-) the line was pre-filled from; null for a line typed by hand.</summary>
    public string? CrmActivityId { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Amount and total as filed, kept when an approver lowers the line (null when never adjusted).</summary>
    public decimal? OriginalAmount { get; set; }
    public decimal? OriginalTotalAmount { get; set; }
    public long? AdjustedByUserId { get; set; }
    public DateTimeOffset? AdjustedAt { get; set; }
    public string? AdjustmentReason { get; set; }
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

    /// <summary>Null on legs saved before fare modes existed (script 016).</summary>
    public long? FareModeId { get; set; }

    /// <summary>The typed mode when the fare mode requires it ("Others").</summary>
    public string? FareModeDetail { get; set; }
}

public class ApprovalStep
{
    public long Id { get; set; }
    public Guid ReportId { get; set; }
    public int Cycle { get; set; } = 1;
    public int StepOrder { get; set; }

    /// <summary>Signatory level (1 or 2) the step was routed at; null for steps filed before scope routing.</summary>
    public short? SignatoryLevel { get; set; }
    public long ApproverUserId { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTimeOffset? ActedAt { get; set; }
    public byte[]? Signature { get; set; }
    public string? Note { get; set; }

    /// <summary>Who acted on the step; differs from <see cref="ApproverUserId"/> when a delegate acted.</summary>
    public long? ActedByUserId { get; set; }

    /// <summary>The delegation a delegate acted under; null when the signatory acted.</summary>
    public long? DelegationId { get; set; }
}

/// <summary>
/// A signatory's delegate for a date range (plan Phase 7). Only approving is delegated. Cancelled
/// rows are kept for the audit trail; the row never changes otherwise.
/// </summary>
public class ApprovalDelegation
{
    public long Id { get; set; }
    public long SignatoryUserId { get; set; }
    public long DelegateUserId { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string? Reason { get; set; }

    /// <summary>Assigned by the Level 2 for an absent Level 1.</summary>
    public bool IsForced { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long? CancelledByUserId { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class FinanceTracking
{
    public Guid ReportId { get; set; }
    public FinanceStatus Status { get; set; } = FinanceStatus.Pending;
    public bool PhysicalReceiptsReceived { get; set; }
    public long? ReceivedByUserId { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public string? Remarks { get; set; }
    public long? ClearedByUserId { get; set; }
    public DateTimeOffset? ClearedAt { get; set; }
    public int FollowUpCount { get; set; }
    public DateTimeOffset? LastFollowUpAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Finance's request to an Expense Verifier to fact-check one line of an approved report (plan
/// Flow 6), and the follow-on query to Level 2 when the finding is Not Verified. Findings and
/// decisions are written once; the row only moves forward through <see cref="VerificationStatus"/>.
/// </summary>
public class VerificationRequest
{
    public long Id { get; set; }
    public Guid ReportId { get; set; }
    public long ExpenseLineId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public VerificationStatus Status { get; set; } = VerificationStatus.Open;
    public long RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public long? VerifierUserId { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public VerificationFinding? Finding { get; set; }
    public string? FindingNote { get; set; }
    public DateTimeOffset? FoundAt { get; set; }
    public long? QueryToUserId { get; set; }
    public long? QueriedByUserId { get; set; }
    public string? QueryNote { get; set; }
    public DateTimeOffset? QueriedAt { get; set; }
    public QueryDisposition? Disposition { get; set; }
    public string? DispositionNote { get; set; }
    public long? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
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

/// <summary>Who signs for whom (RBAC v0.6 section 4). Replaces <c>approval_routes</c>.</summary>
public class ScopeAssignment
{
    public long Id { get; set; }
    public long HolderUserId { get; set; }
    public short SignatoryLevel { get; set; }
    public ScopeAssignmentType AssignmentType { get; set; }
    public long? DepartmentId { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBootstrap { get; set; }
    public string? Note { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Named people on an Individual (covered) or Exclusion (carved out) assignment.</summary>
public class ScopeAssignmentMember
{
    public long ScopeAssignmentId { get; set; }
    public long UserId { get; set; }
}

/// <summary>A named, effective-dated entitlement set (RBAC v0.6 section 5). Replaces <c>employee_rates</c>.</summary>
public class RateProfile
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>One version of one category rate. Closed by setting EffectiveTo, never edited otherwise.</summary>
public class RateProfileLine
{
    public long Id { get; set; }
    public long RateProfileId { get; set; }
    public ExpenseCategory Category { get; set; }
    public string? SubCategory { get; set; }
    public RateType RateType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    public int? FrequencyCap { get; set; }
    public bool RequiresReceipt { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public long ChangedByUserId { get; set; }
    public string? ChangeReason { get; set; }
    public bool BackdateOverride { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Who a profile applies to, by person, job grade, department, or company-wide.</summary>
public class RateProfileAssignment
{
    public long Id { get; set; }
    public long RateProfileId { get; set; }
    public RateTargetType TargetType { get; set; }
    public long? UserId { get; set; }
    public string? JobGrade { get; set; }
    public long? DepartmentId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsBootstrap { get; set; }
    public string? ChangeReason { get; set; }
    public long AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}

/// <summary>Per-year counter behind the ERF reference number series (<c>ERF-2026-000142</c>).</summary>
public class ReferenceSeries
{
    public string SeriesKey { get; set; } = string.Empty;
    public int NextValue { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// One notification SMS sent through Lasius. The unique (sender, idempotency key) pair is what stops
/// a retry from texting twice. The message text is never stored, only its SHA-256 so that a reused
/// key with a different message can be refused.
/// </summary>
public class SmsDelivery
{
    public long Id { get; set; }
    public long SenderUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public long RecipientUserId { get; set; }

    /// <summary>The expense report the SMS is about, when there is one (its signatories are texted too).</summary>
    public Guid? ReportId { get; set; }

    public int RecipientCount { get; set; }
    public string MessageSha256 { get; set; } = string.Empty;

    /// <summary>Sending | Sent | Failed | Unconfirmed.</summary>
    public string Status { get; set; } = string.Empty;

    public int? GatewayHttpStatus { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>One email about an expense report (manual "Email R47" or a workflow event), claimed before SMTP is called.</summary>
public class EmailDelivery
{
    public long Id { get; set; }
    public long SenderUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid ReportId { get; set; }

    /// <summary>"R47" for a manual send, otherwise the workflow event name.</summary>
    public string Kind { get; set; } = string.Empty;

    public int RecipientCount { get; set; }

    /// <summary>Sending | Sent | Failed | Unconfirmed.</summary>
    public string Status { get; set; } = string.Empty;

    public int? SmtpStatusCode { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
