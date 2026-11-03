namespace ERFWeb.Application.Features.Testing;

/// <summary>A kind of record the test-only delete can remove; one per thing an insert or update creates.</summary>
public enum TestDataKind
{
    Report,
    ExpenseLine,
    CashAdvance,
    Attachment,
    Verification,
    User,
    Department,
    CostCenter,
    CostCenterBudget,
    Currency,
    Site,
    FareMode,
    RateProfile,
    RateProfileLine,
    RateAssignment,
    ScopeAssignment,
    Delegation,
    AuditEntry,
}

/// <summary>
/// Hard-deletes records made while testing, only when appsettings <c>App:Mode</c> is <c>Test</c>
/// (403 otherwise). A report goes with everything under it, its workflow audit rows included, and a single
/// audit entry can be removed too;
/// other records go with the rows they own and are refused (409) while something else still uses them.
/// </summary>
public interface ITestDataService
{
    /// <param name="id">The record's key: a number, a report GUID, a currency code, or <c>{costCenterId}-{year}</c> for a budget.</param>
    Task DeleteAsync(TestDataKind kind, string id, CancellationToken ct = default);
}
