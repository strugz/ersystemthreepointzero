namespace ERFWeb.Domain;

/// <summary>
/// Additive application roles (RBAC v0.6). Every user holds <see cref="Employee"/>; the other
/// roles are granted by a System Administrator, take effect at once, and stack, subject to
/// <c>RoleRules</c>.
/// </summary>
public enum UserRole
{
    Employee,
    SignatoryLevel1,
    SignatoryLevel2,
    Finance,
    ExpenseVerifier,
    SystemAdministrator,
}

/// <summary>Lifecycle of a role grant: Active from the moment it is granted until revoked.</summary>
public enum RoleGrantStatus
{
    Active,
    Revoked,
}

/// <summary>The kind of expense report, matching the official ERF categories.</summary>
public enum ReportType
{
    RevolvingFund,
    CashAdvanceLiquidation,
    Reimbursement,
}

/// <summary>Lifecycle status of an expense report (ERF).</summary>
public enum ReportStatus
{
    New,
    ForApproval,
    Returned,
    Approved,

    /// <summary>Refused on merit by an approver. Final: the claimant cannot edit, re-file, or reopen it.</summary>
    Rejected,
}

/// <summary>Status of a single approval step in an approval cycle.</summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Returned,
    Superseded,
    Rejected,

    /// <summary>A Level 1 step a Level 2 approved past with a recorded reason.</summary>
    Overridden,
}

/// <summary>Finance physical-receipt tracking status for an approved report.</summary>
public enum FinanceStatus
{
    /// <summary>Approved; physical receipts not received yet.</summary>
    Pending,

    /// <summary>Some receipts arrived; Finance is waiting for the rest.</summary>
    ReceiptsIncomplete,

    /// <summary>All receipts received. Confirmed once and never reverted.</summary>
    ReceiptsReceived,

    /// <summary>Released for payment. Final.</summary>
    ClearedForPayment,
}

/// <summary>
/// Lifecycle of a verification request (plan Flow 6). Open waits for a verifier; InProgress is
/// picked up; Found holds a Not Verified finding waiting for Finance to query Level 2; Queried waits
/// for Level 2's decision; Closed is resolved. While any request is unresolved the report cannot
/// be cleared for payment.
/// </summary>
public enum VerificationStatus
{
    Open,
    InProgress,
    Found,
    Queried,
    Closed,
}

/// <summary>The Expense Verifier's finding. Recorded once and never changed (9.17).</summary>
public enum VerificationFinding
{
    Verified,
    VerifiedWithNote,
    NotVerified,
}

/// <summary>Level 2's decision on a query about a Not Verified finding.</summary>
public enum QueryDisposition
{
    /// <summary>Let the expense through; Finance carries on.</summary>
    Resume,

    /// <summary>Send the report back to the employee for correction; approval restarts on re-filing.</summary>
    Return,

    /// <summary>Refuse the report. Final.</summary>
    Reject,
}

/// <summary>Whether an expense line is local travel or foreign.</summary>
public enum ExpenseType
{
    Local,
    Foreign,
}

/// <summary>Expense line category. Drives the R47 column the amount is bucketed into.</summary>
public enum ExpenseCategory
{
    Meals,
    Transportation,
    Toll,
    Parking,
    Others,
    Representation,
}

/// <summary>How a scope assignment selects the employees a signatory signs for (RBAC v0.6 section 4).</summary>
public enum ScopeAssignmentType
{
    /// <summary>Everyone in the department.</summary>
    Department,

    /// <summary>The named members only, regardless of department.</summary>
    Individual,

    /// <summary>Everyone in the department except the named members.</summary>
    Exclusion,
}

/// <summary>How a rate profile line's amount is applied (RBAC v0.6 section 5).</summary>
public enum RateType
{
    FixedPerDay,
    PerKm,
    CapPerTransaction,
    ActualWithCeiling,
}

/// <summary>Who a rate profile assignment targets. Resolution order: Individual, Grade, Department, Company.</summary>
public enum RateTargetType
{
    Individual,
    Grade,
    Department,
    Company,
}
