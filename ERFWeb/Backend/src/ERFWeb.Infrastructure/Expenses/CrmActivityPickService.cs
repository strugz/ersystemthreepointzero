using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Lists every CRM activity the employee has on one day (the expense line's date) for the line editor:
/// Completed first, which can be claimed, then Open and Cancelled, shown so the employee sees the whole day.
/// Marks those already claimed. Only the report's owner may list them, with their own CRM link: the CRM is
/// never asked about anyone else.
/// </summary>
public sealed class CrmActivityPickService(ErfDbContext db, ICurrentUser user, ReportEditGate gate, ICrmActivityDirectory crm) : ICrmActivityPickService
{
    /// <summary>The CRM statuses, in the order the picker lists them.</summary>
    private static readonly string[] Statuses = [CrmActivityDto.CompletedStatus, "Open", "Cancelled"];

    /// <summary>The CRM's largest page; one employee's day never comes near it.</summary>
    private const int PageSize = 100;

    public async Task<CrmActivityPickListDto> ListForDateAsync(Guid reportId, DateOnly date, CancellationToken ct = default)
    {
        var report = await gate.LoadOwnedAsync(reportId, ct);
        var employeeId = await db.Users.AsNoTracking()
            .Where(u => u.Id == user.UserId)
            .Select(u => u.CrmEmployeeId)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return new CrmActivityPickListDto(false, date, []);
        }

        if ((report.DateFrom is { } from && date < from) || (report.DateTo is { } to && date > to))
        {
            throw new ValidationException("transactionDate", "Set the line's date inside the report period first; the CRM lists the activities of that day.");
        }

        // The CRM filters by one status per call, so the day takes one call per status.
        var activities = new List<CrmActivityDto>();
        foreach (var status in Statuses)
        {
            var page = await crm.ListAsync(employeeId, date, date, status, 1, PageSize, ct)
                ?? throw new ConflictException("The CRM no longer has your linked employee ID. Ask the system administrator to link your account again, or type the line instead.");
            activities.AddRange(page.Items.Where(a => activities.TrueForAll(seen => seen.ActivityId != a.ActivityId)));
        }

        var ids = activities.Select(a => a.ActivityId).ToList();
        var claims = await (
            from l in db.ExpenseLines.AsNoTracking()
            join r in db.ExpenseReports.AsNoTracking() on l.ReportId equals r.Id
            where l.CrmActivityId != null && ids.Contains(l.CrmActivityId!) && l.IsActive
                && r.EmployeeUserId == report.EmployeeUserId && r.Status != ReportStatus.Rejected
            select new { ActivityId = l.CrmActivityId!, r.Id, r.ReferenceNo })
            .ToListAsync(ct);

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

        var items = activities.Select(a =>
        {
            var mine = claims.Where(c => c.ActivityId == a.ActivityId).ToList();
            var elsewhere = mine.FirstOrDefault(c => c.Id != report.Id);
            var others = a.Companions.Append(a.Employee)
                .OfType<CrmActivityPersonDto>()
                .Where(p => !string.Equals(p.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase))
                .Select(p => erfNames.TryGetValue(p.EmployeeId, out var name) ? name : p.Name)
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var isOwner = string.Equals(a.Employee?.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase);
            return new CrmActivityOptionDto(
                a.ActivityId, a.ActivityType, a.Status, a.IsCompleted, a.ActivityDate, a.Subject, a.ServiceNumber,
                a.CustomerName, a.InstrumentName, a.SerialNumber,
                isOwner ? "Lead" : "Companion",
                others.Count > 0 ? string.Join(", ", others) : null,
                a.TravelOrigin, a.TravelDestination, a.IsOvernight,
                elsewhere is not null, elsewhere?.ReferenceNo,
                mine.Count(c => c.Id == report.Id));
        }).ToList();

        return new CrmActivityPickListDto(true, date, items);
    }
}
