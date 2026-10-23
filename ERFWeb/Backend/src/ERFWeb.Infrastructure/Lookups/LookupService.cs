using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Entitlements;
using ERFWeb.Domain.Administration;
using ERFWeb.Application.Features.Lookups;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Lookups;

/// <summary>Reference lookups for the report editor (LINQ read-only).</summary>
public sealed class LookupService(
    ErfDbContext db,
    ICurrentUser user,
    ISystemSettingsService settings,
    IClock clock,
    IApprovalRouter router,
    IRateService rates,
    Entitlements.EntitlementContext calendar) : ILookupService
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

    public async Task<IReadOnlyList<DepartmentOptionDto>> DepartmentsAsync(CancellationToken ct = default) =>
        await db.Departments.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionDto(d.Id, d.Code, d.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FareModeOptionDto>> FareModesAsync(CancellationToken ct = default) =>
        await db.FareModes.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
            .Select(m => new FareModeOptionDto(m.Id, m.Code, m.Name, m.RequiresDetail, m.ReceiptExpected))
            .ToListAsync(ct);

    /// <summary>The rates in force on the expense date (spec section 5); today when no date is given.</summary>
    public Task<EmployeeRatesDto> MyRatesAsync(DateOnly? date = null, CancellationToken ct = default) =>
        rates.EditorRatesAsync(user.UserId, date ?? calendar.Today, ct);

    public async Task<MyFilingReadinessDto> MyFilingReadinessAsync(CancellationToken ct = default)
    {
        var account = await db.Users.AsNoTracking()
            .Where(u => u.Id == user.UserId)
            .Select(u => new { HasSignature = u.Signature != null && u.Signature.Length > 0, u.CreatedAt })
            .FirstOrDefaultAsync(ct);
        var hasSignature = account?.HasSignature ?? false;

        var coolingDays = await settings.GetCoolingPeriodDaysAsync(ct);
        var coolingElapsed = account is null || AccountRules.CoolingPeriodElapsed(account.CreatedAt, clock.UtcNow, coolingDays);
        var coolingEndsAt = account is null || coolingElapsed ? (DateTimeOffset?)null : account.CreatedAt.AddDays(coolingDays);

        // A zero total shows the base route; a larger report may still add a Level 2 step at filing.
        var routing = await router.RouteAsync(user.UserId, 0m, calendar.Today, ct);
        var approvers = routing.Approvers.Select(a => a.FullName).ToList();

        return new MyFilingReadinessDto(hasSignature, approvers.Count, approvers, coolingElapsed, coolingEndsAt, routing.Problem);
    }
}
