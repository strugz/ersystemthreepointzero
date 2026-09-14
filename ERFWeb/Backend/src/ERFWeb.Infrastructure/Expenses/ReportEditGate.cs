using ERFWeb.Application.Common;
using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Loads a report for mutation and enforces the employee-side rules: the caller must own the
/// report (Admins may act on any), and the report must be in an editable status.
/// </summary>
public sealed class ReportEditGate(ErfDbContext db, ICurrentUser user)
{
    /// <summary>Tracked report the current user may edit (New/Returned). Throws 404/403/409 otherwise.</summary>
    public async Task<ExpenseReport> LoadEditableAsync(Guid reportId, CancellationToken ct)
    {
        var report = await LoadOwnedAsync(reportId, ct);
        if (!ReportWorkflowRules.CanEdit(report.Status))
        {
            throw new ConflictException($"The report is '{report.Status}' and can no longer be edited. Reopen it first.");
        }

        return report;
    }

    /// <summary>Tracked report owned by (or administrable by) the current user, in any status.</summary>
    public async Task<ExpenseReport> LoadOwnedAsync(Guid reportId, CancellationToken ct)
    {
        var report = await db.ExpenseReports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new NotFoundException("Expense report not found.");

        if (report.EmployeeUserId != user.UserId && !user.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("You can only change your own expense reports.");
        }

        return report;
    }
}
