using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Verification;
using ERFWeb.Domain;
using ERFWeb.Domain.Verification;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Verification;

/// <summary>
/// Read side of verification: the panel on a report (with what the viewer may do, from the same
/// rules the actions enforce) and the rows of the verifier and Level 2 queues.
/// </summary>
public sealed class VerificationReader(ErfDbContext db, ICurrentUser user, IClock clock)
{
    /// <summary>
    /// Everyone who approved a step of the report in any cycle, including a delegate who approved
    /// on a signatory's behalf (9.16: they may not verify it).
    /// </summary>
    public async Task<HashSet<long>> ApproversAsync(Guid reportId, CancellationToken ct)
    {
        var rows = await db.ApprovalSteps.AsNoTracking()
            .Where(s => s.ReportId == reportId && s.Status == ApprovalStatus.Approved)
            .Select(s => new { s.ApproverUserId, s.ActedByUserId })
            .ToListAsync(ct);
        return rows.Select(r => r.ApproverUserId)
            .Concat(rows.Where(r => r.ActedByUserId != null).Select(r => r.ActedByUserId!.Value))
            .ToHashSet();
    }

    public static VerificationState ToState(VerificationRequest v, long claimantUserId) =>
        new(v.Status, claimantUserId, v.RequestedByUserId, v.VerifierUserId, v.Finding, v.QueryToUserId);

    public async Task<VerificationPanelDto> PanelAsync(
        Guid reportId,
        long claimantUserId,
        ReportStatus reportStatus,
        FinanceStatus? financeStatus,
        CancellationToken ct)
    {
        var rows = await (
            from v in db.VerificationRequests.AsNoTracking()
            where v.ReportId == reportId
            join l in db.ExpenseLines.AsNoTracking() on v.ExpenseLineId equals l.Id
            orderby v.RequestedAt
            select new { Request = v, l.Particulars, l.TotalAmount, RowVersion = EF.Property<uint>(v, DbContextExtensions.RowVersionProperty) })
            .ToListAsync(ct);

        var me = user.UserId;
        var isClaimant = me == claimantUserId;
        var isFinance = user.IsInRole(UserRole.Finance);
        var isVerifier = user.IsInRole(UserRole.ExpenseVerifier);
        var approvers = isVerifier ? await ApproversAsync(reportId, ct) : [];
        var names = await NamesAsync(rows.SelectMany(r => new[]
        {
            (long?)r.Request.RequestedByUserId, r.Request.VerifierUserId, r.Request.QueryToUserId,
        }), ct);

        var requests = rows.Select(r =>
        {
            var v = r.Request;
            var state = ToState(v, claimantUserId);
            return new VerificationDto(
                v.Id, v.ExpenseLineId, r.Particulars, r.TotalAmount, v.Status,
                isClaimant ? null : v.Reason,
                names.GetValueOrDefault(v.RequestedByUserId, "Finance"),
                v.RequestedAt,
                isClaimant ? null : Name(names, v.VerifierUserId),
                v.PickedUpAt,
                v.Finding,
                isClaimant ? null : v.FindingNote,
                v.FoundAt,
                isClaimant ? null : Name(names, v.QueryToUserId),
                isClaimant ? null : v.QueryNote,
                v.QueriedAt,
                v.Disposition,
                v.DispositionNote,
                v.DecidedAt,
                r.RowVersion,
                CanPickUp: isVerifier && VerificationRules.CanPickUp(state, approvers, me).IsAllowed,
                CanRecordFinding: isVerifier && v.Status == VerificationStatus.InProgress && v.VerifierUserId == me,
                CanQuery: isFinance && !isClaimant && v.Status == VerificationStatus.Found && v.Finding == VerificationFinding.NotVerified,
                CanDecide: !isClaimant && v.Status == VerificationStatus.Queried && v.QueryToUserId == me);
        }).ToList();

        var canRequest = isFinance && VerificationRules.CanRaise(reportStatus, financeStatus, claimantUserId, me, false, "x").IsAllowed;
        var underVerification = rows.Any(r => VerificationRules.IsUnresolved(r.Request.Status));
        return new VerificationPanelDto(canRequest, underVerification, requests);
    }

    /// <summary>Queue rows for the verifier pool or a Level 2's queries.</summary>
    public async Task<List<VerificationQueueItemDto>> ItemsAsync(IQueryable<VerificationRequest> requests, CancellationToken ct)
    {
        var rows = await (
            from v in requests
            join l in db.ExpenseLines.AsNoTracking() on v.ExpenseLineId equals l.Id
            join r in db.ExpenseReports.AsNoTracking() on v.ReportId equals r.Id
            join e in db.Users.AsNoTracking() on r.EmployeeUserId equals e.Id
            join dj in db.Departments.AsNoTracking() on e.DepartmentId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            select new
            {
                Request = v,
                Line = l,
                r.ReferenceNo,
                r.EmployeeUserId,
                EmployeeName = e.FullName,
                DepartmentName = d != null ? d.Name : null,
            }).ToListAsync(ct);

        var names = await NamesAsync(rows.SelectMany(r => new[] { (long?)r.Request.RequestedByUserId, r.Request.VerifierUserId }), ct);
        var now = clock.UtcNow;
        return rows.Select(x =>
        {
            var v = x.Request;
            var since = v.Status switch
            {
                VerificationStatus.Queried => v.QueriedAt,
                VerificationStatus.InProgress => v.PickedUpAt,
                _ => v.RequestedAt,
            } ?? v.RequestedAt;
            return new VerificationQueueItemDto(
                v.Id, v.ReportId, x.ReferenceNo, x.EmployeeName, x.DepartmentName,
                v.ExpenseLineId, x.Line.TransactionDate, x.Line.Particulars, x.Line.TotalAmount,
                v.Reason, names.GetValueOrDefault(v.RequestedByUserId, "Finance"), v.RequestedAt, v.Status,
                Name(names, v.VerifierUserId), v.Finding, v.FindingNote, v.FoundAt, v.QueryNote, v.QueriedAt,
                v.Disposition, v.DecidedAt, Math.Max(0, (int)(now - since).TotalDays));
        }).ToList();
    }

    /// <summary>Unresolved requests and whether one waits for Finance's query, per report.</summary>
    public async Task<Dictionary<Guid, (int Open, bool NeedsQuery)>> OpenByReportAsync(IReadOnlyCollection<Guid> reportIds, CancellationToken ct)
    {
        var rows = await db.VerificationRequests.AsNoTracking()
            .Where(v => reportIds.Contains(v.ReportId) && v.Status != VerificationStatus.Closed)
            .GroupBy(v => v.ReportId)
            .Select(g => new { g.Key, Open = g.Count(), NeedsQuery = g.Any(v => v.Status == VerificationStatus.Found) })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Key, r => (r.Open, r.NeedsQuery));
    }

    private async Task<Dictionary<long, string>> NamesAsync(IEnumerable<long?> ids, CancellationToken ct)
    {
        var list = ids.OfType<long>().Distinct().ToList();
        return await db.Users.AsNoTracking().Where(u => list.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
    }

    private static string? Name(Dictionary<long, string> names, long? id) => id is { } x ? names.GetValueOrDefault(x) : null;
}
