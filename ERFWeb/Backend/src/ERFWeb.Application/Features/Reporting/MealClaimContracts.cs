using ERFWeb.Domain.Reporting;

namespace ERFWeb.Application.Features.Reporting;

/// <summary>
/// A daily meal claims request (Finance only): Meals lines whose expense date falls in From..To. Filed (For Approval)
/// and approved reports count; drafts, returned and rejected reports never do. The department is the one saved on the
/// report at filing (the employee's current one for a report filed before that was saved).
/// </summary>
public sealed record DailyMealClaimQuery(DateOnly From, DateOnly To, long? DepartmentId = null, bool ApprovedOnly = false);

/// <summary>A built daily meal claims report plus what produced it.</summary>
public sealed record DailyMealClaimReport(
    DailyMealClaimQuery Query,
    string? DepartmentName,
    DateTimeOffset GeneratedAt,
    string GeneratedBy,
    DailyMealClaims Claims);

/// <summary>The on-screen daily meal claims.</summary>
public sealed record DailyMealClaimReportDto(
    DateOnly From,
    DateOnly To,
    string? DepartmentName,
    bool ApprovedOnly,
    IReadOnlyList<MealClaimDay> Days,
    int EmployeeDays,
    decimal Total)
{
    public static DailyMealClaimReportDto Create(DailyMealClaimReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var q = report.Query;
        var c = report.Claims;
        return new DailyMealClaimReportDto(q.From, q.To, report.DepartmentName, q.ApprovedOnly, c.Days, c.EmployeeDays, c.Total);
    }
}

public interface IDailyMealClaimService
{
    Task<DailyMealClaimReport> BuildAsync(DailyMealClaimQuery query, CancellationToken ct = default);
}
