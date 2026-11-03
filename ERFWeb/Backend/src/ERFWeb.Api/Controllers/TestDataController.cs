using ERFWeb.Api.Security;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERFWeb.Api.Controllers;

/// <summary>
/// Test-only hard deletes of records made while testing (one kind per thing an insert or update creates).
/// The System Administrator only, and refused (403) unless appsettings <c>App:Mode</c> is <c>Test</c>.
/// </summary>
[ApiController]
[Route("api/test-data")]
[Authorize(Policy = Policies.SystemAdministrator)]
[Produces("application/json")]
public sealed class TestDataController(ITestDataService testData) : ControllerBase
{
    /// <summary>The URL segment for each kind, as the SPA sends it.</summary>
    private static readonly Dictionary<string, TestDataKind> Kinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["reports"] = TestDataKind.Report,
        ["expense-lines"] = TestDataKind.ExpenseLine,
        ["cash-advances"] = TestDataKind.CashAdvance,
        ["attachments"] = TestDataKind.Attachment,
        ["verifications"] = TestDataKind.Verification,
        ["users"] = TestDataKind.User,
        ["departments"] = TestDataKind.Department,
        ["cost-centers"] = TestDataKind.CostCenter,
        ["cost-center-budgets"] = TestDataKind.CostCenterBudget,
        ["currencies"] = TestDataKind.Currency,
        ["sites"] = TestDataKind.Site,
        ["fare-modes"] = TestDataKind.FareMode,
        ["rate-profiles"] = TestDataKind.RateProfile,
        ["rate-profile-lines"] = TestDataKind.RateProfileLine,
        ["rate-assignments"] = TestDataKind.RateAssignment,
        ["scope-assignments"] = TestDataKind.ScopeAssignment,
        ["delegations"] = TestDataKind.Delegation,
        ["audit-entries"] = TestDataKind.AuditEntry,
        ["role-grants"] = TestDataKind.RoleGrant,
    };

    [HttpDelete("{kind}/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string kind, string id, CancellationToken ct)
    {
        if (!Kinds.TryGetValue(kind, out var testDataKind))
        {
            throw new NotFoundException();
        }

        await testData.DeleteAsync(testDataKind, id, ct);
        return NoContent();
    }
}
