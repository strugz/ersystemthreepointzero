using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Expenses;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// The claimant's CRM activities over the report period, for the signatory reviewing the report. The server takes
/// the CRM employee ID from the claimant's own account link; the caller never supplies one. Every status is listed
/// (the CRM filters one status per call) and activities used on the report's lines are marked.
/// </summary>
public sealed class CrmCalendarService(ErfDbContext db, ICurrentUser user, IReportAccessPolicy access, ICrmActivityDirectory crm) : ICrmCalendarService
{
    private static readonly string[] Statuses = [CrmActivityDto.CompletedStatus, "Open", "Cancelled"];

    /// <summary>The CRM's largest page.</summary>
    private const int PageSize = 100;

    /// <summary>Stops a runaway paging loop; 62 days of one person's activities stay far below this.</summary>
    private const int MaxPages = 10;

    public async Task<CrmCalendarDto> GetForReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await (
            from r in db.ExpenseReports.AsNoTracking()
            join u in db.Users.AsNoTracking() on r.EmployeeUserId equals u.Id
            where r.Id == reportId
            select new { r.Id, r.DateFrom, r.DateTo, u.FullName, u.CrmEmployeeId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Report not found.");

        if (!(user.IsInRole(UserRole.SignatoryLevel1) || user.IsInRole(UserRole.SignatoryLevel2)) || !await access.CanViewAsync(reportId, ct))
        {
            throw new ForbiddenException("You are not allowed to view this report's CRM calendar.");
        }

        if (string.IsNullOrWhiteSpace(report.CrmEmployeeId))
        {
            return new CrmCalendarDto(false, report.FullName, report.DateFrom, report.DateTo, []);
        }

        if (report.DateFrom is not { } from || report.DateTo is not { } to)
        {
            return new CrmCalendarDto(true, report.FullName, report.DateFrom, report.DateTo, []);
        }

        if (!CrmCalendarRules.FitsCrmRange(from, to))
        {
            throw new ValidationException("dateFrom", $"The CRM calendar shows at most {CrmCalendarRules.MaxDays} days; this report's period is longer.");
        }

        var employeeId = report.CrmEmployeeId;
        var activities = await ListAllAsync(employeeId, from, to, ct);

        var lines = await db.ExpenseLines.AsNoTracking()
            .Where(l => l.ReportId == reportId && l.IsActive)
            .Select(l => new { l.TransactionDate, l.CrmActivityIds })
            .ToListAsync(ct);
        var claimedHere = lines.SelectMany(l => l.CrmActivityIds).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var linesPerDay = lines.GroupBy(l => l.TransactionDate).ToDictionary(g => g.Key, g => g.Count());

        // Work With prints the ERFWeb name of a linked colleague, else the name the CRM gives.
        var peopleIds = activities
            .SelectMany(a => a.Companions.Append(a.Employee))
            .OfType<CrmActivityPersonDto>()
            .Select(p => p.EmployeeId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var erfNames = (await db.Users.AsNoTracking()
                .Where(u => u.CrmEmployeeId != null && peopleIds.Contains(u.CrmEmployeeId!))
                .Select(u => new { u.CrmEmployeeId, u.FullName })
                .ToListAsync(ct))
            .ToDictionary(u => u.CrmEmployeeId!, u => u.FullName, StringComparer.OrdinalIgnoreCase);

        var entriesByDay = activities
            .SelectMany(a => CrmCalendarRules.DaysCovered(a.ActivityDate, a.EndAt is { } end ? DateOnly.FromDateTime(end.DateTime) : null, from, to),
                (a, day) => new { Day = day, Entry = ToEntry(a, employeeId, erfNames, claimedHere) })
            .ToLookup(x => x.Day, x => x.Entry);

        var days = CrmCalendarRules.PeriodDays(from, to)
            .Select(day => new CrmCalendarDayDto(
                day,
                linesPerDay.GetValueOrDefault(day),
                entriesByDay[day].OrderBy(e => e.StartAt ?? DateTimeOffset.MaxValue).ThenBy(e => e.ActivityId, StringComparer.Ordinal).ToList()))
            .ToList();

        return new CrmCalendarDto(true, report.FullName, from, to, days);
    }

    private async Task<List<CrmActivityDto>> ListAllAsync(string employeeId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var activities = new List<CrmActivityDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var status in Statuses)
        {
            for (var page = 1; page <= MaxPages; page++)
            {
                var result = await crm.ListAsync(employeeId, from, to, status, page, PageSize, ct)
                    ?? throw new ConflictException("The CRM no longer has this employee's linked ID. Ask the system administrator to link the account again.");
                activities.AddRange(result.Items.Where(a => seen.Add(a.ActivityId)));
                if (result.Items.Count == 0 || page * PageSize >= result.Total)
                {
                    break;
                }
            }
        }

        return activities;
    }

    private static CrmCalendarEntryDto ToEntry(CrmActivityDto a, string employeeId, Dictionary<string, string> erfNames, HashSet<string> claimedHere)
    {
        var others = a.Companions.Append(a.Employee)
            .OfType<CrmActivityPersonDto>()
            .Where(p => !string.Equals(p.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase))
            .Select(p => erfNames.TryGetValue(p.EmployeeId, out var name) ? name : p.Name)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var isOwner = string.Equals(a.Employee?.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase);
        return new CrmCalendarEntryDto(
            a.ActivityId, a.ActivityType, a.Status, a.IsCompleted, a.StartAt, a.EndAt, a.Subject, a.ServiceNumber,
            a.CustomerName, a.InstrumentName, a.SerialNumber,
            isOwner ? "Lead" : "Companion",
            others.Count > 0 ? string.Join(", ", others) : null,
            claimedHere.Contains(a.ActivityId));
    }
}
