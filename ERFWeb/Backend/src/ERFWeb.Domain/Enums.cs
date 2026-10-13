namespace ERFWeb.Domain;

/// <summary>
/// Additive application roles (RBAC v0.6). Every user holds <see cref="Employee"/>; the other
/// roles are granted separately and stack, subject to <c>RoleRules</c>. <see cref="BusinessOwner"/>
/// is the designated countersigning authority (CFO or HR Head), modelled as a grant so that it
/// gets the same Pending Activation and countersign path as the privileged roles.
/// </summary>
public enum UserRole
{
    Employee,
    SignatoryLevel1,
    SignatoryLevel2,
    Finance,
    ExpenseVerifier,
    SystemAdministrator,
    BusinessOwner,
}

/// <summary>Lifecycle of a role grant: privileged roles wait for a countersign before they apply.</summary>
public enum RoleGrantStatus
{
    PendingActivation,
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
}

/// <summary>Status of a single approval step in an approval cycle.</summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Returned,
    Superseded,
}

/// <summary>Finance physical-receipt tracking status for an approved report.</summary>
public enum FinanceStatus
{
    Pending,
    ReceiptsReceived,
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
