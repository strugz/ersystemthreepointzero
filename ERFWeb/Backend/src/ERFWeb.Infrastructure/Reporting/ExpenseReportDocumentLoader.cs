using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using ERFWeb.Infrastructure.Administration;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERFWeb.Infrastructure.Reporting;

/// <summary>
/// LINQ read model for the R47 document: report header + employee, cash advance,
/// department signatories, and the active expense lines in print order.
/// </summary>
public sealed class ExpenseReportDocumentLoader(ErfDbContext db, ISystemSettingsService settings) : IExpenseReportDocumentLoader
{
    public async Task<ExpenseReportDocumentDto> LoadAsync(Guid reportId, CancellationToken ct = default)
    {
        // The R47 signature rule: an image prints only in E-signature mode and once Finance approved it;
        // otherwise each signed place prints its electronic status with the date and time.
        var mode = await settings.GetR47SignatureModeAsync(ct);

        var header = await (
            from r in db.ExpenseReports.AsNoTracking()
            where r.Id == reportId
            join u in db.Users.AsNoTracking() on r.EmployeeUserId equals u.Id
            join dsj in db.DepartmentSignatories.AsNoTracking() on u.DepartmentId equals (long?)dsj.DepartmentId into dss
            from ds in dss.DefaultIfEmpty()
            select new
            {
                r.Id,
                r.ReferenceNo,
                u.FullName,
                r.Purpose,
                r.RevolvingFundAmount,
                u.Signature,
                SignatureApproved = u.SignatureApprovedAt != null,
                EmployeeReviewer = u.R47ReviewedBy,
                r.CurrentCycle,
                r.FiledAt,
                Endorser = ds != null ? ds.EndorserInitials : null,
                Reviewer = ds != null ? ds.ReviewerInitials : null,
                Approver = ds != null ? ds.ApproverInitials : null,
            }).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Expense report not found.");

        // The routed approvers of the current cycle sign the form: with a two-step route the
        // Level 1 approver is ENDORSED BY and the Level 2 approver is APPROVED BY; a single
        // step is the final approver. Department initials remain the fallback before filing.
        // The approver's signature on file prints once that step is approved (and the signature rule
        // above lets it print), matching how the employee's signature is printed. (The per-step snapshot taken at approval time is not
        // used: it would keep a placeholder signature after the account's signature is replaced.)
        // When a delegate approved on the signatory's behalf, the delegate signed: their name and
        // signature print, never the absent signatory's signature.
        var approvers = (await (
            from s in db.ApprovalSteps.AsNoTracking()
            where s.ReportId == reportId && s.Cycle == header.CurrentCycle
            join a in db.Users.AsNoTracking() on s.ApproverUserId equals a.Id
            join dj in db.Users.AsNoTracking() on s.ActedByUserId equals (long?)dj.Id into ds
            from d in ds.DefaultIfEmpty()
            orderby s.StepOrder
            select new
            {
                Approved = s.Status == ApprovalStatus.Approved,
                s.ActedAt,
                Delegated = s.ActedByUserId != null && s.ActedByUserId != s.ApproverUserId && d != null,
                SignatoryName = a.FullName,
                SignatorySignature = a.Signature,
                SignatoryApproved = a.SignatureApprovedAt != null,
                DelegateName = d != null ? d.FullName : null,
                DelegateSignature = d != null ? d.Signature : null,
                DelegateApproved = d != null && d.SignatureApprovedAt != null,
            }).ToListAsync(ct))
            .Select(x => new
            {
                FullName = x.Delegated ? $"{x.DelegateName} for {x.SignatoryName}" : x.SignatoryName,
                Signature = !x.Approved ? null
                    : x.Delegated ? Printable(mode, x.DelegateSignature, x.DelegateApproved)
                    : Printable(mode, x.SignatorySignature, x.SignatoryApproved),
                ApprovedAt = x.Approved ? x.ActedAt : null,
            })
            .ToList();

        var endorser = approvers.Count >= 2 ? approvers[0] : null;
        var approver = approvers.Count >= 1 ? approvers[^1] : null;

        // REVIEWED BY is not a routed approver: the employee's own value wins, then the department's,
        // otherwise the System Administrator's rule (default FINANCE) prints.
        var reviewedBy = string.IsNullOrWhiteSpace(header.EmployeeReviewer) ? header.Reviewer : header.EmployeeReviewer;
        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            var rule = await db.AppSettings.AsNoTracking()
                .Where(s => s.Key == SystemSettingKeys.R47ReviewedBy)
                .Select(s => s.Value)
                .FirstOrDefaultAsync(ct);
            reviewedBy = string.IsNullOrWhiteSpace(rule) ? SystemSettingKeys.Defaults[SystemSettingKeys.R47ReviewedBy] : rule;
        }

        var cashAdvances = await db.CashAdvanceLines.AsNoTracking()
            .Where(c => c.ReportId == reportId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new R47CashAdvanceRow(c.AdvanceDate, c.ReferenceDocument, c.ReferenceNo, c.Amount))
            .ToListAsync(ct);

        var rawLines = await db.ExpenseLines.AsNoTracking()
            .Where(l => l.ReportId == reportId && l.IsActive)
            .OrderBy(l => l.SortOrder)
            .Select(l => new
            {
                l.TransactionDate,
                l.Particulars,
                l.InvoiceOrNo,
                l.ExpenseType,
                l.Category,
                l.Amount,
                l.VatAmount,
                l.TotalAmount,
                l.Location,
                l.Instrument,
                l.SerialNumber,
                l.ServiceNumber,
                l.WorkWith,
                l.SortOrder,
            })
            .ToListAsync(ct);

        var lines = rawLines.Select(l => new R47Line(
            l.TransactionDate,
            l.Particulars ?? string.Empty,
            l.InvoiceOrNo,
            l.ExpenseType,
            l.Category,
            l.Amount,
            l.VatAmount,
            l.TotalAmount,
            l.Location,
            l.Instrument,
            l.SerialNumber,
            l.ServiceNumber,
            l.WorkWith,
            l.SortOrder)).ToList();

        return new ExpenseReportDocumentDto(
            header.Id,
            header.ReferenceNo,
            header.FullName,
            header.Purpose ?? string.Empty,
            header.RevolvingFundAmount,
            cashAdvances,
            new R47Signatories(
                endorser?.FullName ?? header.Endorser,
                reviewedBy,
                approver?.FullName ?? header.Approver,
                endorser?.Signature,
                approver?.Signature,
                endorser?.ApprovedAt,
                approver?.ApprovedAt),
            Printable(mode, header.Signature, header.SignatureApproved),
            lines,
            header.FiledAt);
    }

    private static byte[]? Printable(R47SignatureMode mode, byte[]? image, bool approved) =>
        R47Signatures.PrintsImage(mode, image is { Length: > 0 }, approved) ? image : null;
}
