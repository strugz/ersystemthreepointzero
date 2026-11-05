using System.Text.Json;
using System.Text.Json.Nodes;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Persistence;
using ERFWeb.Infrastructure.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>
/// The R47 designer (Rules page): reads and saves the R47 layout as one <c>app_settings</c> row
/// (<c>Reports.R47Design</c>) and builds previews of an unsaved layout on a real report. Until a layout is
/// saved the standard form prints, with the server's <c>R47Report:DetailHeight</c>.
/// </summary>
public sealed class R47DesignService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ITransactionRunner tx,
    IWorkflowAuditWriter audit,
    IOptions<R47ReportOptions> r47Options,
    IReportAccessPolicy access,
    IExpenseReportDocumentLoader loader,
    ISystemSettingsService settings) : IR47DesignService
{
    public async Task<R47DesignDto> GetAsync(CancellationToken ct = default)
    {
        var standard = r47Options.Value.DefaultDesign();
        var row = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == R47DesignJson.SettingKey, ct);
        var saved = R47DesignJson.TryParse(row?.Value);

        string? updatedByName = null;
        if (saved is not null && row?.UpdatedByUserId is { } updatedBy)
        {
            updatedByName = await db.Users.AsNoTracking().Where(u => u.Id == updatedBy).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        }

        return new R47DesignDto(saved ?? standard, standard, saved is not null, saved is null ? null : row?.UpdatedAt, updatedByName);
    }

    public async Task<R47DesignDto> SaveAsync(R47Design design, CancellationToken ct = default)
    {
        var normalized = Validated(design);
        var json = R47DesignJson.Serialize(normalized);

        await tx.ExecuteAsync(async token =>
        {
            var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == R47DesignJson.SettingKey, token);
            var previous = R47DesignJson.TryParse(row?.Value) ?? r47Options.Value.DefaultDesign();
            var changes = Changes(previous, normalized);
            var now = clock.UtcNow;

            if (row is null)
            {
                db.AppSettings.Add(new AppSetting { Key = R47DesignJson.SettingKey, Value = json, UpdatedByUserId = user.UserId, UpdatedAt = now });
            }
            else if (row.Value != json)
            {
                row.Value = json;
                row.UpdatedByUserId = user.UserId;
                row.UpdatedAt = now;
            }

            await db.SaveChangesAsync(token);
            if (changes.Count > 0)
            {
                // Only the sizes and texts that changed, never the whole layout.
                await audit.WriteAsync(new WorkflowAuditEntry(
                    AdminAuditEvents.SettingsChanged, null, user.UserId,
                    JsonSerializer.Serialize(new Dictionary<string, object> { [R47DesignJson.SettingKey] = changes })), token);
            }
        }, ct);

        return await GetAsync(ct);
    }

    public async Task<ExpenseReportDocumentDto> PreviewDocumentAsync(R47DesignPreviewRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var referenceNo = request.ReferenceNo?.Trim().ToUpperInvariant();
        if (!request.Sample && request.ReportId is null && string.IsNullOrEmpty(referenceNo))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["referenceNo"] = ["Choose the sample report or a report to preview."] });
        }

        if (request.Engine is { } engine && !Enum.IsDefined(engine))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["engine"] = ["Choose QuestPDF or PDFsharp."] });
        }

        var design = Validated(request.Design);

        ExpenseReportDocumentDto document;
        if (request.Sample)
        {
            // Made-up data: no real employee or report is read.
            document = R47SampleReport.Create(await settings.GetR47FormatAsync(ct));
        }
        else
        {
            var reportId = request.ReportId
                ?? await db.ExpenseReports.AsNoTracking()
                    .Where(r => r.ReferenceNo != null && r.ReferenceNo.ToUpper() == referenceNo)
                    .Select(r => (Guid?)r.Id)
                    .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("No report has that ER reference.");

            // The same rule as the R47 PDF: a preview shows only a report the administrator may view.
            // (An unknown ID is not viewable either, so it answers 403 rather than revealing whether it exists.)
            if (!await access.CanViewAsync(reportId, ct))
            {
                throw new ForbiddenException("You are not allowed to view this report.");
            }

            document = await loader.LoadAsync(reportId, ct);
        }

        var format = document.Format ?? r47Options.Value.DefaultFormat();
        return document with
        {
            Design = design,
            Format = request.Engine is { } chosen ? format with { Engine = chosen } : format,
        };
    }

    public async Task<IReadOnlyList<R47PreviewReportDto>> SearchReportsAsync(string? search, CancellationToken ct = default)
    {
        var query =
            from r in db.ExpenseReports.AsNoTracking()
            join u in db.Users.AsNoTracking() on r.EmployeeUserId equals u.Id
            select new { r, u.FullName };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.FullName, term)
                || (x.r.Purpose != null && EF.Functions.ILike(x.r.Purpose, term))
                || (x.r.ReferenceNo != null && EF.Functions.ILike(x.r.ReferenceNo, term)));
        }

        var candidates = await query
            .OrderByDescending(x => x.r.UpdatedAt)
            .Take(SearchLimit)
            .Select(x => new R47PreviewReportDto(x.r.Id, x.r.ReferenceNo, x.FullName, x.r.Purpose, x.r.Status, x.r.UpdatedAt))
            .ToListAsync(ct);

        var visible = new List<R47PreviewReportDto>(candidates.Count);
        foreach (var report in candidates)
        {
            if (await access.CanViewAsync(report.Id, ct))
            {
                visible.Add(report);
            }
        }

        return visible;
    }

    private const int SearchLimit = 20;

    private static R47Design Validated(R47Design? design)
    {
        if (design is null)
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["r47Design"] = ["Send the R47 layout."] });
        }

        var normalized = R47DesignRules.Normalize(design);
        var errors = R47DesignRules.Validate(normalized);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors.ToDictionary(e => e.Key, e => new[] { e.Value }));
        }

        return normalized;
    }

    /// <summary>The layout properties whose value changed, as { property: { from, to } }.</summary>
    private static Dictionary<string, object> Changes(R47Design from, R47Design to)
    {
        var before = JsonNode.Parse(R47DesignJson.Serialize(from))!.AsObject();
        var after = JsonNode.Parse(R47DesignJson.Serialize(to))!.AsObject();
        var changes = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (name, value) in after)
        {
            var old = before[name];
            if (!JsonNode.DeepEquals(old, value))
            {
                changes[name] = new { from = old?.ToJsonString(), to = value?.ToJsonString() };
            }
        }

        return changes;
    }
}
