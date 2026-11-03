using System.Globalization;
using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Testing;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ERFWeb.Infrastructure.Testing;

/// <summary>
/// Removes records made while testing. Off unless <c>App:Mode</c> is <c>Test</c>; the API also limits it to
/// the System Administrator. Each delete runs in one transaction and writes a <c>TestData.Deleted</c> audit row.
/// <para>
/// A report is removed with everything under it, the same tables and order as
/// <c>Database/maintenance/reset_reports.sql</c>. Its <c>workflow_audit</c> rows go too: this is the one place
/// the API opts in to the script-021 purge (<c>SET LOCAL erfweb.allow_audit_purge</c>, this transaction only).
/// Any other record goes with the rows it owns; a foreign key still pointing at it (a user's reports,
/// a department's users) is answered with 409 so the tester deletes those first.
/// </para>
/// </summary>
public sealed class TestDataService(
    ErfDbContext db,
    ICurrentUser user,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    IOptions<AppModeOptions> mode) : ITestDataService
{
    public async Task DeleteAsync(TestDataKind kind, string id, CancellationToken ct = default)
    {
        if (!mode.Value.IsTest)
        {
            throw new ForbiddenException("Deleting test data is only possible when App:Mode is Test.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var number = kind is TestDataKind.Report or TestDataKind.Currency or TestDataKind.CostCenterBudget ? 0 : ParseLong(id);

        try
        {
            await tx.ExecuteAsync(async token =>
            {
                var removed = kind switch
                {
                    TestDataKind.Report => await DeleteReportAsync(ParseGuid(id), token),
                    TestDataKind.ExpenseLine => await DeleteExpenseLineAsync(number, token),
                    TestDataKind.CashAdvance => await db.CashAdvanceLines.Where(c => c.Id == number).ExecuteDeleteAsync(token),
                    TestDataKind.Attachment => await db.ReceiptAttachments.Where(a => a.Id == number).ExecuteDeleteAsync(token),
                    TestDataKind.Verification => await db.VerificationRequests.Where(v => v.Id == number).ExecuteDeleteAsync(token),
                    TestDataKind.User => await DeleteUserAsync(number, token),
                    TestDataKind.Department => await DeleteDepartmentAsync(number, token),
                    TestDataKind.CostCenter => await DeleteCostCenterAsync(number, token),
                    TestDataKind.CostCenterBudget => await DeleteBudgetAsync(id, token),
                    TestDataKind.Currency => await db.Currencies.Where(c => c.Code == id).ExecuteDeleteAsync(token),
                    TestDataKind.Site => await db.Sites.Where(s => s.Id == number).ExecuteDeleteAsync(token),
                    TestDataKind.FareMode => await db.FareModes.Where(f => f.Id == number).ExecuteDeleteAsync(token),
                    TestDataKind.RateProfile => await DeleteRateProfileAsync(number, token),
                    TestDataKind.RateProfileLine => await db.RateProfileLines.Where(l => l.Id == number).ExecuteDeleteAsync(token),
                    TestDataKind.RateAssignment => await db.RateProfileAssignments.Where(a => a.Id == number).ExecuteDeleteAsync(token),
                    TestDataKind.ScopeAssignment => await DeleteScopeAssignmentAsync(number, token),
                    TestDataKind.Delegation => await DeleteDelegationAsync(number, token),
                    _ => throw new ValidationException("kind", "Unknown kind of test data."),
                };

                if (removed == 0)
                {
                    throw new NotFoundException();
                }

                await audit.WriteAsync(new WorkflowAuditEntry("TestData.Deleted", null, user.UserId,
                    JsonSerializer.Serialize(new { kind = kind.ToString(), id })), token);
            }, ct);
        }
        catch (Exception ex) when (IsForeignKeyViolation(ex))
        {
            throw new ConflictException("Other records still use this one. Delete those first, then try again.");
        }
    }

    private async Task<int> DeleteReportAsync(Guid reportId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("SET LOCAL erfweb.allow_audit_purge = 'on'", ct);

        var lineIds = db.ExpenseLines.Where(l => l.ReportId == reportId).Select(l => l.Id);
        await db.VerificationRequests.Where(v => v.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.ExpenseMealItems.Where(m => lineIds.Contains(m.ExpenseLineId)).ExecuteDeleteAsync(ct);
        await db.ExpenseTransportItems.Where(t => lineIds.Contains(t.ExpenseLineId)).ExecuteDeleteAsync(ct);
        await db.ExpenseLines.Where(l => l.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.ReceiptAttachments.Where(a => a.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.ReminderDeliveries.Where(r => r.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.EmailDeliveries.Where(e => e.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.SmsDeliveries.Where(s => s.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.CashAdvanceLines.Where(c => c.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.FinanceTrackings.Where(f => f.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.ApprovalSteps.Where(s => s.ReportId == reportId).ExecuteDeleteAsync(ct);
        await db.WorkflowAudits.Where(a => a.ReportId == reportId).ExecuteDeleteAsync(ct);
        return await db.ExpenseReports.Where(r => r.Id == reportId).ExecuteDeleteAsync(ct);
    }

    private async Task<int> DeleteExpenseLineAsync(long lineId, CancellationToken ct)
    {
        await db.VerificationRequests.Where(v => v.ExpenseLineId == lineId).ExecuteDeleteAsync(ct);
        await db.ExpenseMealItems.Where(m => m.ExpenseLineId == lineId).ExecuteDeleteAsync(ct);
        await db.ExpenseTransportItems.Where(t => t.ExpenseLineId == lineId).ExecuteDeleteAsync(ct);
        return await db.ExpenseLines.Where(l => l.Id == lineId).ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Removes an account with the setup that belongs to it (roles, rates, routes, scope, delegations, sends).
    /// Its reports, and anything it did on someone else's report, stay and block the delete.
    /// </summary>
    private async Task<int> DeleteUserAsync(long userId, CancellationToken ct)
    {
        if (userId == user.UserId)
        {
            throw new ConflictException("You cannot delete your own account.");
        }

        var username = await db.Users.Where(u => u.Id == userId).Select(u => u.Username).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException();
        if (await db.ExpenseReports.AnyAsync(r => r.EmployeeUserId == userId, ct))
        {
            throw new ConflictException("This user still has expense reports. Delete those first, then try again.");
        }

        var heldScopes = db.ScopeAssignments.Where(s => s.HolderUserId == userId).Select(s => s.Id);
        await db.ScopeAssignmentMembers.Where(m => m.UserId == userId || heldScopes.Contains(m.ScopeAssignmentId)).ExecuteDeleteAsync(ct);
        await db.ScopeAssignments.Where(s => s.HolderUserId == userId).ExecuteDeleteAsync(ct);
        await db.RateProfileAssignments.Where(a => a.UserId == userId).ExecuteDeleteAsync(ct);
        await db.EmployeeRates.Where(r => r.UserId == userId).ExecuteDeleteAsync(ct);
        await db.ApprovalRoutes.Where(r => r.EmployeeUserId == userId || r.ApproverUserId == userId).ExecuteDeleteAsync(ct);

        var delegations = db.ApprovalDelegations.Where(d => d.SignatoryUserId == userId || d.DelegateUserId == userId).Select(d => d.Id);
        await db.ApprovalSteps.Where(s => s.DelegationId != null && delegations.Contains(s.DelegationId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DelegationId, (long?)null), ct);
        await db.ApprovalDelegations.Where(d => d.SignatoryUserId == userId || d.DelegateUserId == userId).ExecuteDeleteAsync(ct);

        await db.SmsDeliveries.Where(s => s.SenderUserId == userId || s.RecipientUserId == userId).ExecuteDeleteAsync(ct);
        await db.EmailDeliveries.Where(e => e.SenderUserId == userId).ExecuteDeleteAsync(ct);
        await db.UserRoleGrants.Where(g => g.UserId == userId).ExecuteDeleteAsync(ct);
        await db.LoginSecurities.Where(l => l.Username == username).ExecuteDeleteAsync(ct);
        return await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(ct);
    }

    private async Task<int> DeleteDepartmentAsync(long departmentId, CancellationToken ct)
    {
        await db.DepartmentSignatories.Where(s => s.DepartmentId == departmentId).ExecuteDeleteAsync(ct);
        return await db.Departments.Where(d => d.Id == departmentId).ExecuteDeleteAsync(ct);
    }

    private async Task<int> DeleteCostCenterAsync(long costCenterId, CancellationToken ct)
    {
        await db.CostCenterBudgets.Where(b => b.CostCenterId == costCenterId).ExecuteDeleteAsync(ct);
        return await db.CostCenters.Where(c => c.Id == costCenterId).ExecuteDeleteAsync(ct);
    }

    /// <summary><paramref name="id"/> is <c>{costCenterId}-{year}</c>.</summary>
    private Task<int> DeleteBudgetAsync(string id, CancellationToken ct)
    {
        var parts = id.Split('-');
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var costCenterId)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var year))
        {
            throw new NotFoundException();
        }

        return db.CostCenterBudgets.Where(b => b.CostCenterId == costCenterId && b.BudgetYear == year).ExecuteDeleteAsync(ct);
    }

    private async Task<int> DeleteRateProfileAsync(long profileId, CancellationToken ct)
    {
        await db.RateProfileAssignments.Where(a => a.RateProfileId == profileId).ExecuteDeleteAsync(ct);
        await db.RateProfileLines.Where(l => l.RateProfileId == profileId).ExecuteDeleteAsync(ct);
        return await db.RateProfiles.Where(p => p.Id == profileId).ExecuteDeleteAsync(ct);
    }

    private async Task<int> DeleteScopeAssignmentAsync(long assignmentId, CancellationToken ct)
    {
        await db.ScopeAssignmentMembers.Where(m => m.ScopeAssignmentId == assignmentId).ExecuteDeleteAsync(ct);
        return await db.ScopeAssignments.Where(s => s.Id == assignmentId).ExecuteDeleteAsync(ct);
    }

    /// <summary>Steps a delegate signed under it keep the signature and lose only the link.</summary>
    private async Task<int> DeleteDelegationAsync(long delegationId, CancellationToken ct)
    {
        await db.ApprovalSteps.Where(s => s.DelegationId == delegationId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DelegationId, (long?)null), ct);
        return await db.ApprovalDelegations.Where(d => d.Id == delegationId).ExecuteDeleteAsync(ct);
    }

    private static long ParseLong(string id) =>
        long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : throw new NotFoundException();

    private static Guid ParseGuid(string id) =>
        Guid.TryParse(id, out var value) ? value : throw new NotFoundException();

    private static bool IsForeignKeyViolation(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
            {
                return true;
            }
        }

        return false;
    }
}
