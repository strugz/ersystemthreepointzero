using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Authorization;

/// <summary>
/// Report-level view authorization: Finance sees all reports; an employee sees their own;
/// an approver sees reports that have an approval step assigned to them. An Expense Verifier sees
/// a report only while it has an open request in the pool or one they picked up (scoped to the
/// open request, plan Phase 6); a Level 2 sees reports they were queried about. A System
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

        if (await db.ApprovalSteps.AsNoTracking().AnyAsync(s => s.ReportId == reportId && s.ApproverUserId == user.UserId, ct))
        {
            return true;
        }

        var me = user.UserId;
        var isVerifier = user.IsInRole(UserRole.ExpenseVerifier);
        return await db.VerificationRequests.AsNoTracking().AnyAsync(v => v.ReportId == reportId
            && (v.QueryToUserId == me
                || (isVerifier && (v.Status == VerificationStatus.Open
                    || (v.VerifierUserId == me && v.Status != VerificationStatus.Closed)))), ct);
    }
}
