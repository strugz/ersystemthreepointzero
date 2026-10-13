namespace ERFWeb.Domain.Expenses;

/// <summary>
/// Pure lifecycle rules for an expense report (ERF) from the employee's side:
/// when it may be edited, filed, reopened, or deleted, and what filing requires.
/// </summary>
public static class ReportWorkflowRules
{
    /// <summary>A report is editable only while it is not in the approval pipeline.</summary>
    public static bool CanEdit(ReportStatus status) =>
        status is ReportStatus.New or ReportStatus.Returned;

    /// <summary>Filing (and re-filing after a return) is allowed from the same states as editing.</summary>
    public static bool CanFile(ReportStatus status) => CanEdit(status);

    /// <summary>
    /// A filed or approved report can be pulled back for correction. This supersedes the
    /// current approval cycle, so approval restarts from step 1 when it is filed again.
    /// </summary>
    public static bool CanReopen(ReportStatus status) =>
        status is ReportStatus.ForApproval or ReportStatus.Approved;

    /// <summary>Only a never-filed report can be deleted outright.</summary>
    public static bool CanDelete(ReportStatus status, DateTimeOffset? filedAt) =>
        status == ReportStatus.New && filedAt is null;

    /// <summary>Only a Liquidation of Cash Advance carries the CASH ADVANCE RECEIVED block.</summary>
    public static bool UsesCashAdvance(ReportType type) => type == ReportType.CashAdvanceLiquidation;

    /// <summary>Only a Replenishment of Revolving Fund carries the "IF WITH REVOLVING FUND" amount.</summary>
    public static bool UsesRevolvingFund(ReportType type) => type == ReportType.RevolvingFund;

    /// <summary>
    /// Pre-conditions for filing. Returns human-readable reasons; empty when the report may be filed.
    /// </summary>
    public static IReadOnlyList<string> FilingBlockers(
        ReportStatus status,
        int activeLineCount,
        bool employeeHasSignature,
        int approverCount,
        string? purpose,
        ReportType reportType,
        decimal cashAdvanceAmount,
        decimal revolvingFundAmount,
        bool coolingPeriodElapsed = true,
        string? routingProblem = null)
    {
        var reasons = new List<string>();

        if (!coolingPeriodElapsed)
        {
            reasons.Add("Your account is still within its cooling period after registration. Filing opens once the period has elapsed.");
        }

        if (!CanFile(status))
        {
            reasons.Add($"A report in status '{status}' cannot be filed.");
        }

        if (activeLineCount == 0)
        {
            reasons.Add("No expense data to file. Add at least one expense line.");
        }

        if (!employeeHasSignature)
        {
            reasons.Add("Please insert your signature first (Account Settings > Signature).");
        }

        if (approverCount == 0)
        {
            reasons.Add(routingProblem ?? "No approval route is configured for your account. Ask an administrator to assign your approvers.");
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            reasons.Add("Purpose is required.");
        }

        if (UsesCashAdvance(reportType) && cashAdvanceAmount <= 0)
        {
            reasons.Add("A cash advance amount is required for a Liquidation of Cash Advance report.");
        }

        if (UsesRevolvingFund(reportType) && revolvingFundAmount <= 0)
        {
            reasons.Add("A revolving fund amount is required for a Replenishment of Revolving Fund report.");
        }

        return reasons;
    }
}
