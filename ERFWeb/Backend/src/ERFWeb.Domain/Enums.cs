namespace ERFWeb.Domain;

/// <summary>Application roles that gate access to features and endpoints.</summary>
public enum UserRole
{
    Employee,
    Manager,
    Finance,
    Admin,
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
