using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Crm;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Domain;
using ERFWeb.Infrastructure.Crm;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Expenses;

/// <summary>
/// Lists the employee's Completed CRM activities in a report's period for the line editor, marking those
/// already claimed. Only the report's owner may list them, with their own CRM link: the CRM is never asked
/// about anyone else.
/// </summary>
public sealed class CrmActivityPickService(ErfDbContext db, ICurrentUser user, ReportEditGate gate, ICrmActivityDirectory crm) : ICrmActivityPickService
{
    private const int DefaultPageSize = 100;

    public async Task<CrmActivityPickListDto> ListForReportAsync(Guid reportId, int page, int pageSize, CancellationToken ct = default)
    {
        var report = await gate.LoadOwnedAsync(reportId, ct);
        var employeeId = await db.Users.AsNoTracking()
            .Where(u => u.Id == user.UserId)
            .Select(u => u.CrmEmployeeId)
            .FirstOrDefaultAsync(ct);
        page = Math.Max(1, page);
        pageSize = pageSize is < 1 or > DefaultPageSize ? DefaultPageSize : pageSize;
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return new CrmActivityPickListDto(false, report.DateFrom, report.DateTo, [], 0, page, pageSize);
        }

        if (report.DateFrom is not { } from || report.DateTo is not { } to)
        {
            throw new ValidationException("dateFrom", "Set the report's Date From and Date To first; the CRM lists the activities in that period.");
        }

        if (to.DayNumber - from.DayNumber + 1 > CrmActivityDirectory.MaxRangeDays)
        {
            throw new ValidationException("dateFrom", $"The CRM lists at most {CrmActivityDirectory.MaxRangeDays} days at a time. Shorten the report period.");
        }

        var result = await crm.ListAsync(employeeId, from, to, page, pageSize, ct)
            ?? throw new ConflictException("The CRM no longer has your linked employee ID. Ask the system administrator to link your account again, or type the line instead.");

        var ids = result.Items.Select(a => a.ActivityId).ToList();
        var claims = await (
            from l in db.ExpenseLines.AsNoTracking()
            join r in db.ExpenseReports.AsNoTracking() on l.ReportId equals r.Id
            where l.CrmActivityId != null && ids.Contains(l.CrmActivityId!) && l.IsActive
                && r.EmployeeUserId == report.EmployeeUserId && r.Status != ReportStatus.Rejected
            select new { ActivityId = l.CrmActivityId!, r.Id, r.ReferenceNo })
            .ToListAsync(ct);

        // Work With prints the ERFWeb name of a linked colleague, else the name the CRM gives.
        var peopleIds = result.Items
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

        var items = result.Items.Select(a =>
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
                a.ActivityId, a.ActivityType, a.ActivityDate, a.Subject, a.ServiceNumber,
                a.CustomerName, a.InstrumentName, a.SerialNumber,
                isOwner ? "Lead" : "Companion",
                others.Count > 0 ? string.Join(", ", others) : null,
                a.TravelOrigin, a.TravelDestination, a.IsOvernight,
                elsewhere is not null, elsewhere?.ReferenceNo,
                mine.Count(c => c.Id == report.Id));
        }).ToList();

        return new CrmActivityPickListDto(true, from, to, items, result.Total, result.Page, result.PageSize);
    }
}
