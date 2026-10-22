using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>
/// Fare modes maintained by the System Administrator. Same rules as the other master data: rows are
/// never deleted, only deactivated, and every change writes an <c>Admin.MasterDataChanged</c> audit row
/// in the same transaction.
/// </summary>
public sealed class FareModeAdminService(
    ErfDbContext db,
    ICurrentUser user,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit) : IFareModeAdminService
{
    private const int MaxCode = 20;
    private const int MaxName = 60;
    private const int MaxSortOrder = 9999;

    public async Task<IReadOnlyList<FareModeDto>> ListAsync(bool includeInactive, CancellationToken ct = default) =>
        await db.FareModes.AsNoTracking()
            .Where(m => includeInactive || m.IsActive)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
            .Select(m => new FareModeDto(m.Id, m.Code, m.Name, m.SortOrder, m.ReceiptExpected, m.RequiresDetail, m.LegacyFareId, m.IsActive,
                EF.Property<uint>(m, DbContextExtensions.RowVersionProperty)))
            .ToListAsync(ct);

    public async Task<FareModeDto> CreateAsync(SaveFareModeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = Validate(request);

        var id = await tx.ExecuteAsync(async token =>
        {
            if (await db.FareModes.AnyAsync(m => m.Code == code, token))
            {
                throw new ValidationException("code", "A fare mode with that code already exists.");
            }

            var entity = new FareMode { Code = code };
            Apply(entity, request);
            db.FareModes.Add(entity);
            await db.SaveChangesAsync(token);
            await AuditAsync("created", entity.Id, Snapshot(entity), token);
            return entity.Id;
        }, ct);

        return (await ListAsync(true, ct)).First(m => m.Id == id);
    }

    public async Task<FareModeDto> UpdateAsync(long id, SaveFareModeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = Validate(request);

        await tx.ExecuteAsync(async token =>
        {
            var entity = await db.FareModes.FirstOrDefaultAsync(m => m.Id == id, token)
                ?? throw new NotFoundException("Fare mode not found.");
            if (await db.FareModes.AnyAsync(m => m.Code == code && m.Id != id, token))
            {
                throw new ValidationException("code", "A fare mode with that code already exists.");
            }

            var before = Snapshot(entity);
            entity.Code = code;
            Apply(entity, request);
            db.SetRowVersion(entity, request.RowVersion ?? throw new ValidationException("rowVersion", "rowVersion is required for an update."));
            await db.SaveWithConcurrencyAsync(token);
            await AuditAsync("updated", id, new { before, after = Snapshot(entity) }, token);
        }, ct);

        return (await ListAsync(true, ct)).First(m => m.Id == id);
    }

    /// <summary>Checks the request and returns the normalized (upper-case) code.</summary>
    private static string Validate(SaveFareModeRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (code.Length == 0)
        {
            errors["code"] = ["Code is required."];
        }
        else if (code.Length > MaxCode)
        {
            errors["code"] = [$"Code must be at most {MaxCode} characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (request.Name.Trim().Length > MaxName)
        {
            errors["name"] = [$"Name must be at most {MaxName} characters."];
        }

        if (request.SortOrder is < 0 or > MaxSortOrder)
        {
            errors["sortOrder"] = [$"Sort order must be between 0 and {MaxSortOrder}."];
        }

        if (request.LegacyFareId is <= 0)
        {
            errors["legacyFareId"] = ["The legacy fare id must be a positive number."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return code;
    }

    private static void Apply(FareMode entity, SaveFareModeRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.SortOrder = request.SortOrder;
        entity.ReceiptExpected = request.ReceiptExpected;
        entity.RequiresDetail = request.RequiresDetail;
        entity.LegacyFareId = request.LegacyFareId;
        entity.IsActive = request.IsActive;
    }

    private static object Snapshot(FareMode m) =>
        new { m.Code, m.Name, m.SortOrder, m.ReceiptExpected, m.RequiresDetail, m.LegacyFareId, m.IsActive };

    private Task AuditAsync(string action, long key, object detail, CancellationToken ct) =>
        audit.WriteAsync(new WorkflowAuditEntry(
            AdminAuditEvents.MasterDataChanged, null, user.UserId,
            JsonSerializer.Serialize(new { entity = "fareMode", action, key, detail })), ct);
}
