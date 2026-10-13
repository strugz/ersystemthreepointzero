using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Authorization;

/// <summary>
/// Report-level view authorization: Finance sees all reports; an employee sees their own;
/// an approver sees reports that have an approval step assigned to them. A System
/// Administrator holds no transacting role and never sees report contents (RBAC v0.6).
/// </summary>
public sealed class ReportAccessPolicy(ErfDbContext db, ICurrentUser user) : IReportAccessPolicy
{
    public async Task<bool> CanViewAsync(Guid reportId, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated)
        {
            return false;
        }

        if (user.IsInRole(UserRole.Finance))
        {
            return true;
        }

        var isOwner = await db.ExpenseReports.AsNoTracking()
            .AnyAsync(r => r.Id == reportId && r.EmployeeUserId == user.UserId, ct);
        if (isOwner)
        {
            return true;
        }

        return await db.ApprovalSteps.AsNoTracking()
            .AnyAsync(s => s.ReportId == reportId && s.ApproverUserId == user.UserId, ct);
    }
}
