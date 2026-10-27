using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Domain;
using ERFWeb.Domain.Notifications;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Emails a report's R47 PDF, at most once per idempotency key, to its claimant and its Level 1 and
/// Level 2 signatories (and, for a manual send, the sender), whoever has an address on file. A workflow
/// event addressed to particular people (a verification step) goes to those people only.
/// <para>
/// Same delivery discipline as <see cref="SmsService"/>: the (sender, key) pair is claimed with an insert
/// the unique index lets only one request win; a retry of a sent email replays "sent"; only an email the
/// server definitely refused may be sent again under the key; an unanswered send is never resent. Once
/// claimed, rendering, SMTP and the bookkeeping ignore the request's cancellation token.
/// </para>
/// </summary>
public sealed class ReportEmailService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    IEmailGateway gateway,
    IExpenseReportDocumentLoader loader,
    IExpenseReportPdfRenderer renderer,
    IWorkflowAuditWriter audit,
    ILogger<ReportEmailService> logger) : IReportEmailService
{
    public const string ManualKind = "R47";

    private const string Sending = "Sending";
    private const string Sent = "Sent";
    private const string Failed = "Failed";
    private const string Unconfirmed = "Unconfirmed";

    /// <summary>Without a client key, a second "Email R47" of the same report within this window counts as a retry.</summary>
    private static readonly TimeSpan AutoKeyWindow = TimeSpan.FromMinutes(10);

    private const int HistoryLimit = 20;

    public async Task<EmailSendResult> SendR47Async(Guid reportId, SendReportEmailRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        if (EmailRules.IdempotencyKeyError(key) is { } keyError)
        {
            return new EmailSendResult(EmailSendOutcome.InvalidRequest, Errors: new Dictionary<string, string[]> { ["idempotencyKey"] = [keyError] });
        }

        var report = await FindReportAsync(reportId, ct);
        if (report is null)
        {
            return new EmailSendResult(EmailSendOutcome.ReportNotFound);
        }

        var reference = report.ReferenceNo ?? "Expense report";
        var sender = SenderName();
        var content = new ReportEmailContent(
            ManualKind,
            $"{reference} - Expense Report (R47) of {report.EmployeeName}",
            $"""
            Good day,

            {sender} sent you the Expense Report (R47) {reference} of {report.EmployeeName} from ERFWeb.
            Status: {StatusText(report.Status)}.

            The R47 PDF is attached. Sign in to ERFWeb to view the report and its receipts.

            This is an automated message; please do not reply.
            """);
        key ??= $"auto:r47:{reportId:N}:{clock.UtcNow.ToUnixTimeSeconds() / (long)AutoKeyWindow.TotalSeconds}";
        return await SendCoreAsync(report, content, key, includeSender: true, ct);
    }

    public async Task<EmailSendResult> SendAboutReportAsync(Guid reportId, ReportEmailContent content, string idempotencyKey, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (EmailRules.IdempotencyKeyError(idempotencyKey) is { } keyError)
        {
            return new EmailSendResult(EmailSendOutcome.InvalidRequest, Errors: new Dictionary<string, string[]> { ["idempotencyKey"] = [keyError] });
        }

        var report = await FindReportAsync(reportId, ct);
        return report is null
            ? new EmailSendResult(EmailSendOutcome.ReportNotFound)
            : await SendCoreAsync(report, content, idempotencyKey, includeSender: false, ct);
    }

    public async Task<EmailSendResult> SendToUsersAboutReportAsync(
        Guid reportId, IReadOnlyList<long> userIds, ReportEmailContent content, string idempotencyKey, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentNullException.ThrowIfNull(content);
        if (EmailRules.IdempotencyKeyError(idempotencyKey) is { } keyError)
        {
            return new EmailSendResult(EmailSendOutcome.InvalidRequest, Errors: new Dictionary<string, string[]> { ["idempotencyKey"] = [keyError] });
        }

        var report = await FindReportAsync(reportId, ct);
        if (report is null)
        {
            return new EmailSendResult(EmailSendOutcome.ReportNotFound);
        }

        if (report.Status == ReportStatus.New)
        {
            return new EmailSendResult(EmailSendOutcome.ReportNotFiled);
        }

        return await SendToAddressesAsync(report, content, idempotencyKey, await AddressesAsync(userIds.Distinct().ToList(), ct), ct);
    }

    public async Task<IReadOnlyList<ReportEmailDeliveryDto>> HistoryAsync(Guid reportId, CancellationToken ct = default) =>
        await (
            from d in db.EmailDeliveries.AsNoTracking()
            where d.ReportId == reportId
            join u in db.Users.AsNoTracking() on d.SenderUserId equals u.Id
            orderby d.CreatedAt descending, d.Id descending
            select new ReportEmailDeliveryDto(d.Id, d.Kind, d.Status, d.RecipientCount, u.FullName, d.CreatedAt, d.CompletedAt))
            .Take(HistoryLimit)
            .ToListAsync(ct);

    private async Task<EmailSendResult> SendCoreAsync(ReportRow report, ReportEmailContent content, string key, bool includeSender, CancellationToken ct)
    {
        if (report.Status == ReportStatus.New)
        {
            return new EmailSendResult(EmailSendOutcome.ReportNotFiled);
        }

        return await SendToAddressesAsync(report, content, key, await RecipientAddressesAsync(report, includeSender, ct), ct);
    }

    private async Task<EmailSendResult> SendToAddressesAsync(ReportRow report, ReportEmailContent content, string key, IReadOnlyList<string> addresses, CancellationToken ct)
    {
        if (addresses.Count == 0)
        {
            return new EmailSendResult(EmailSendOutcome.NoRecipients);
        }

        if (!gateway.IsConfigured)
        {
            return new EmailSendResult(EmailSendOutcome.NotConfigured);
        }

        var (deliveryId, early) = await ClaimAsync(key, report.Id, content.Kind, addresses.Count, ct);
        return early ?? await DeliverAsync(deliveryId, report, content, addresses);
    }

    private Task<ReportRow?> FindReportAsync(Guid reportId, CancellationToken ct) =>
        (from r in db.ExpenseReports.AsNoTracking()
         where r.Id == reportId
         join u in db.Users.AsNoTracking() on r.EmployeeUserId equals u.Id
         select new ReportRow(r.Id, r.ReferenceNo, r.Status, r.EmployeeUserId, u.FullName, r.CurrentCycle))
        .FirstOrDefaultAsync(ct);

    /// <summary>
    /// The claimant first, then the active Level 1 and Level 2 signatories of the current cycle in step
    /// order (steps filed before scope routing have no level; their first two steps stand in), then the
    /// sender for a manual send. Anyone without a usable address is skipped; a shared mailbox gets one copy.
    /// </summary>
    private async Task<IReadOnlyList<string>> RecipientAddressesAsync(ReportRow report, bool includeSender, CancellationToken ct)
    {
        var approverIds = await db.ApprovalSteps.AsNoTracking()
            .Where(s => s.ReportId == report.Id && s.Cycle == report.CurrentCycle
                && (s.SignatoryLevel == 1 || s.SignatoryLevel == 2 || (s.SignatoryLevel == null && s.StepOrder <= 2)))
            .OrderBy(s => s.StepOrder)
            .Select(s => s.ApproverUserId)
            .ToListAsync(ct);

        var ordered = new List<long> { report.EmployeeUserId };
        ordered.AddRange(approverIds);
        if (includeSender)
        {
            ordered.Add(user.UserId);
        }

        return await AddressesAsync(ordered.Distinct().ToList(), ct);
    }

    /// <summary>The usable addresses of the active users among <paramref name="ids"/>, in that order; a shared mailbox gets one copy.</summary>
    private async Task<IReadOnlyList<string>> AddressesAsync(IReadOnlyList<long> ids, CancellationToken ct)
    {
        var emailById = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id) && u.IsActive)
            .ToDictionaryAsync(u => u.Id, u => u.Email, ct);
        return EmailRules.DistinctAddresses(ids.Where(emailById.ContainsKey).Select(id => emailById[id]));
    }

    /// <summary>Wins the (sender, key) pair for this request, or says what an earlier request with the key already did.</summary>
    private async Task<(long DeliveryId, EmailSendResult? Early)> ClaimAsync(string key, Guid reportId, string kind, int recipientCount, CancellationToken ct)
    {
        var existing = await FindAsync(key, ct);
        if (existing is null)
        {
            var row = new EmailDelivery
            {
                SenderUserId = user.UserId,
                IdempotencyKey = key,
                ReportId = reportId,
                Kind = kind,
                RecipientCount = recipientCount,
                Status = Sending,
                Attempts = 1,
                CreatedAt = clock.UtcNow,
            };
            db.EmailDeliveries.Add(row);
            try
            {
                await db.SaveChangesAsync(ct);
                return (row.Id, null);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // A concurrent retry with the same key claimed it first; answer with what that one is doing.
                db.Entry(row).State = EntityState.Detached;
                existing = await FindAsync(key, ct);
                if (existing is null)
                {
                    return (0, new EmailSendResult(EmailSendOutcome.InProgress));
                }
            }
        }

        if (existing.ReportId != reportId || existing.Kind != kind)
        {
            return (0, new EmailSendResult(EmailSendOutcome.KeyReused));
        }

        switch (existing.Status)
        {
            case Sent:
                return (0, new EmailSendResult(EmailSendOutcome.Sent, existing.RecipientCount, Replayed: true));
            case Unconfirmed:
                return (0, new EmailSendResult(EmailSendOutcome.GatewayUnconfirmed));
            case Failed:
                // The server refused the earlier attempt, so nothing went out: this retry may send it.
                var claimed = await db.EmailDeliveries
                    .Where(x => x.Id == existing.Id && x.Status == Failed)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, Sending)
                        .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                        .SetProperty(x => x.RecipientCount, recipientCount)
                        .SetProperty(x => x.SmtpStatusCode, (int?)null)
                        .SetProperty(x => x.CompletedAt, (DateTimeOffset?)null), ct);
                return claimed == 1 ? (existing.Id, null) : (0, new EmailSendResult(EmailSendOutcome.InProgress));
            default:
                return (0, new EmailSendResult(EmailSendOutcome.InProgress));
        }
    }

    private async Task<EmailSendResult> DeliverAsync(long deliveryId, ReportRow report, ReportEmailContent content, IReadOnlyList<string> addresses)
    {
        // From here on: CancellationToken.None. The claim is taken, so the send and its record must finish.
        EmailGatewayResult result;
        try
        {
            // Rendered now, so the attachment carries the signatures and status as of this event.
            var document = await loader.LoadAsync(report.Id, CancellationToken.None);
            var pdf = renderer.Render(document, clock.UtcNow);
            var fileName = $"ExpenseReport-{(report.ReferenceNo is { Length: > 0 } r ? r : report.Id.ToString("N"))}.pdf";
            var message = new EmailMessage(addresses, content.Subject, content.Body, [new EmailAttachment(fileName, "application/pdf", pdf)]);
            result = await gateway.SendAsync(message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Unknown failure while rendering or inside the mail client: whether anything left is unknown,
            // so do not resend under this key.
            logger.LogError(ex, "Email delivery {DeliveryId} failed unexpectedly.", deliveryId);
            result = new EmailGatewayResult(EmailGatewayStatus.Unreachable);
        }

        var (status, outcome) = result.Status switch
        {
            EmailGatewayStatus.Accepted => (Sent, EmailSendOutcome.Sent),
            EmailGatewayStatus.Rejected => (Failed, EmailSendOutcome.GatewayFailed),
            _ => (Unconfirmed, EmailSendOutcome.GatewayUnconfirmed),
        };

        var now = clock.UtcNow;
        await db.EmailDeliveries
            .Where(x => x.Id == deliveryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.SmtpStatusCode, result.SmtpStatusCode)
                .SetProperty(x => x.CompletedAt, now), CancellationToken.None);

        // Spec 9.9: every email sent is in the append-only log. What and how many, never the addresses or text.
        await audit.WriteAsync(
            new WorkflowAuditEntry(
                $"Email{status}",
                report.Id,
                user.UserId,
                JsonSerializer.Serialize(new { deliveryId, kind = content.Kind, recipientCount = addresses.Count, smtpStatusCode = result.SmtpStatusCode })),
            CancellationToken.None);

        return new EmailSendResult(outcome, addresses.Count);
    }

    private Task<EmailDelivery?> FindAsync(string key, CancellationToken ct) =>
        db.EmailDeliveries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SenderUserId == user.UserId && x.IdempotencyKey == key, ct);

    private string SenderName() => string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;

    private static string StatusText(ReportStatus status) => status switch
    {
        ReportStatus.ForApproval => "For approval",
        _ => status.ToString(),
    };

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };

    private sealed record ReportRow(Guid Id, string? ReferenceNo, ReportStatus Status, long EmployeeUserId, string EmployeeName, int CurrentCycle);
}
