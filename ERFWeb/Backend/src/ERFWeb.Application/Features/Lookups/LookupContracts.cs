namespace ERFWeb.Application.Features.Lookups;

/// <summary>A colleague selectable in the "Work With" picker.</summary>
public sealed record WorkWithOptionDto(long UserId, string FullName, string? Position, string? Department);

/// <summary>The signed-in employee's configured per-diem / transport rates.</summary>
public sealed record EmployeeRatesDto(
    decimal TransportRate,
    decimal BreakfastRate,
    decimal LunchRate,
    decimal DinnerRate,
    decimal OtMealRate);

/// <summary>Whether the signed-in employee can file (signature on record, approvers configured).</summary>
public sealed record MyFilingReadinessDto(bool HasSignature, int ApproverCount, IReadOnlyList<string> ApproverNames);

public interface ILookupService
{
    Task<IReadOnlyList<WorkWithOptionDto>> WorkWithAsync(string? search, CancellationToken ct = default);
    Task<EmployeeRatesDto> MyRatesAsync(CancellationToken ct = default);
    Task<MyFilingReadinessDto> MyFilingReadinessAsync(CancellationToken ct = default);
}
