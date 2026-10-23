namespace ERFWeb.Application.Features.Administration;

/// <summary>How a fare leg was travelled. Listed by <see cref="SortOrder"/>; "Others" asks for the mode (<see cref="RequiresDetail"/>).</summary>
public sealed record FareModeDto(
    long Id,
    string Code,
    string Name,
    int SortOrder,
    bool ReceiptExpected,
    bool RequiresDetail,
    int? LegacyFareId,
    bool IsActive,
    uint RowVersion);

/// <summary>Create or replace one fare mode. <see cref="RowVersion"/> is required on update.</summary>
public sealed record SaveFareModeRequest(
    string Code,
    string Name,
    int SortOrder,
    bool ReceiptExpected,
    bool RequiresDetail,
    int? LegacyFareId,
    bool IsActive,
    uint? RowVersion);

/// <summary>
/// The fare-mode list maintained by the System Administrator (master data). Modes are deactivated,
/// never deleted, because fare legs reference them.
/// </summary>
public interface IFareModeAdminService
{
    Task<IReadOnlyList<FareModeDto>> ListAsync(bool includeInactive, CancellationToken ct = default);
    Task<FareModeDto> CreateAsync(SaveFareModeRequest request, CancellationToken ct = default);
    Task<FareModeDto> UpdateAsync(long id, SaveFareModeRequest request, CancellationToken ct = default);
}
