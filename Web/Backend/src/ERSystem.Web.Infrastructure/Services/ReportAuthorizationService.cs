using ERSystem.Web.Application.Common;
using ERSystem.Web.Domain.Common;
using ERSystem.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERSystem.Web.Infrastructure.Services;

public sealed class ReportAuthorizationService(IDbContextFactory<LegacyErDbContext> contextFactory) : IReportAuthorizationService
{
    public async Task EnsureManagerCanAccessAsync(int managerUserId, string reportId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var authorized = await db.ApprovalTransactions.AsNoTracking().AnyAsync(
            x => x.ReportId == reportId && x.ApproverUserId == managerUserId &&
                 x.Status != ApprovalTransactionStates.Superseded,
            cancellationToken);
        if (!authorized) throw new ForbiddenException("The current manager cannot access this report.");
    }
}
