using ERSystem.Web.Application.Common;
using ERSystem.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERSystem.Web.Infrastructure.Services;

public sealed class ReportAuthorizationService(IDbContextFactory<LegacyErDbContext> contextFactory) : IReportAuthorizationService
{
    public async Task EnsureManagerCanAccessAsync(int managerUserId, string reportId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var authorized = await (
            from report in db.Reports.AsNoTracking()
            join assignment in db.UserAuthorities.AsNoTracking() on report.UserId equals assignment.UserId
            where report.Id == reportId && assignment.AuthorityId == managerUserId
            select report.Id).AnyAsync(cancellationToken);
        if (!authorized) throw new ForbiddenException("The current manager cannot access this report.");
    }
}
