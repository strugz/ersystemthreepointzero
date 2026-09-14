namespace ERFWeb.Domain.Expenses;

/// <summary>
/// Who the closing balance of an ERF is owed to. The legacy report never stored this: the
/// "BAL DUE TO" text follows the sign of <c>cash advance − expenses</c>. A positive
/// remainder means the employee still holds company money (due to the company); a negative
/// remainder means the employee spent beyond the advance (due to the employee). A zero
/// balance keeps the employee label the desktop always printed.
/// </summary>
public static class BalanceDueRules
{
    public const string EmployeeLabel = "EMPLOYEE";

    public static string DueTo(decimal balanceDue, string companyLabel)
    {
        if (string.IsNullOrWhiteSpace(companyLabel))
        {
            throw new ArgumentException("Company label is required.", nameof(companyLabel));
        }

        return balanceDue > 0 ? companyLabel.Trim().ToUpperInvariant() : EmployeeLabel;
    }

    public static bool IsDueToCompany(decimal balanceDue) => balanceDue > 0;
}
