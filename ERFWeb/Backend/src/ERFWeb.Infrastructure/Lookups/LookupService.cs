using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Lookups;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Lookups;

/// <summary>Reference lookups for the report editor (LINQ read-only).</summary>
public sealed class LookupService(ErfDbContext db, ICurrentUser user) : ILookupService
{
    private const int MaxWorkWithResults = 50;

    public async Task<IReadOnlyList<WorkWithOptionDto>> WorkWithAsync(string? search, CancellationToken ct = default)
    {
        var users =
            from u in db.Users.AsNoTracking()
            where u.IsActive && u.WorkWithEnabled && u.Id != user.UserId
            join dj in db.Departments.AsNoTracking() on u.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            select new { u.Id, u.FullName, u.Position, Department = d != null ? d.Name : null };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            users = users.Where(x => EF.Functions.ILike(x.FullName, term));
        }

        return await users
            .OrderBy(x => x.FullName)
            .Take(MaxWorkWithResults)
            .Select(x => new WorkWithOptionDto(x.Id, x.FullName, x.Position, x.Department))
            .ToListAsync(ct);
    }

    public async Task<EmployeeRatesDto> MyRatesAsync(CancellationToken ct = default)
    {
        var rate = await db.EmployeeRates.AsNoTracking()
            .Where(r => r.UserId == user.UserId)
            .Select(r => new EmployeeRatesDto(r.TransportRate, r.BreakfastRate, r.LunchRate, r.DinnerRate, r.OtMealRate))
            .FirstOrDefaultAsync(ct);

        return rate ?? new EmployeeRatesDto(0m, 0m, 0m, 0m, 0m);
    }

    public async Task<MyFilingReadinessDto> MyFilingReadinessAsync(CancellationToken ct = default)
    {
        var hasSignature = await db.Users.AsNoTracking()
            .Where(u => u.Id == user.UserId)
            .Select(u => u.Signature != null && u.Signature.Length > 0)
            .FirstOrDefaultAsync(ct);

        var approvers = await (
            from ar in db.ApprovalRoutes.AsNoTracking()
            where ar.EmployeeUserId == user.UserId
            join a in db.Users.AsNoTracking() on ar.ApproverUserId equals a.Id
            where a.IsActive
            orderby ar.StepOrder
            select a.FullName).ToListAsync(ct);

        return new MyFilingReadinessDto(hasSignature, approvers.Count, approvers);
    }
}
