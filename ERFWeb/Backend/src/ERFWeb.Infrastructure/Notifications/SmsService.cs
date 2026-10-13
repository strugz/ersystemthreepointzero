using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Domain.Notifications;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERFWeb.Infrastructure.Notifications;

/// <summary>
/// Sends a notification SMS through Lasius, at most once per idempotency key, to a user's text group and,
/// when the SMS is about a report, to the report's Level 1 and Level 2 signatories as well.
/// <para>
/// Before calling the gateway the service claims the (sender, key) pair with an insert that the unique
/// index lets only one request win. A retry of a sent message replays "sent"; a retry while it is still
/// sending gets "in progress"; only a message the gateway definitely refused may be sent again under the
/// same key. When the gateway did not answer, the SMS may already be on its way, so it is never resent
/// under that key.
/// </para>
/// <para>
/// Once claimed, the gateway call and the bookkeeping after it ignore the request's cancellation token:
/// a user leaving the page must not abandon a message half sent.
/// </para>
/// </summary>
public sealed class SmsService(
    ErfDbContext db,
    ICurrentUser user,
    IClock clock,
    ISmsGateway gateway,
    IWorkflowAuditWriter audit,
    ILogger<SmsService> logger) : ISmsService
{
    private const string Sending = "Sending";
    private const string Sent = "Sent";
    private const string Failed = "Failed";
    private const string Unconfirmed = "Unconfirmed";

    /// <summary>Without a client key, the same message to the same person within this window counts as a retry.</summary>
    private static readonly TimeSpan AutoKeyWindow = TimeSpan.FromMinutes(10);

    public async Task<SmsSendResult> SendAsync(SendSmsRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        var errors = new Dictionary<string, string[]>();
        if (request.ContactId <= 0)
        {
            errors["contactId"] = ["Choose who to send the SMS to."];
        }

        if (SmsRules.MessageError(request.Message) is { } messageError)
        {
            errors["message"] = [messageError];
        }

        if (SmsRules.IdempotencyKeyError(key) is { } keyError)
        {
            errors["idempotencyKey"] = [keyError];
        }

        if (errors.Count > 0)
        {
            return new SmsSendResult(SmsSendOutcome.InvalidRequest, Errors: errors);
        }

        var message = request.Message!.Trim();
        var claimant = await db.Users.AsNoTracking()
            .Where(u => u.Id == request.ContactId && u.IsActive)
            .Select(u => new { u.Id, u.MobileNo })
            .FirstOrDefaultAsync(ct);
        if (claimant is null)
        {
            return new SmsSendResult(SmsSendOutcome.UserNotFound);
        }

        // The claimant first, then (for a report) its Level 1 and Level 2 signatories. Anyone without a
        // mobile number is skipped; a number shared by two people is texted once.
        var numbers = SmsRules.RecipientNumbers(claimant.MobileNo).ToList();
        if (request.ReportId is { } reportId)
        {
            var report = await db.ExpenseReports.AsNoTracking()
                .Where(r => r.Id == reportId)
                .Select(r => new { r.EmployeeUserId, r.CurrentCycle })
                .FirstOrDefaultAsync(ct);
            if (report is null)
            {
                return new SmsSendResult(SmsSendOutcome.ReportNotFound);
            }

            if (report.EmployeeUserId != claimant.Id)
            {
                return new SmsSendResult(SmsSendOutcome.InvalidRequest, Errors: new Dictionary<string, string[]>
                {
                    ["contactId"] = ["The SMS must be addressed to the report's claimant."],
                });
            }

            foreach (var mobileNo in await SignatoryMobileNumbersAsync(reportId, report.CurrentCycle, ct))
            {
                numbers.AddRange(SmsRules.RecipientNumbers(mobileNo).Where(n => !numbers.Contains(n)));
            }
        }

        if (numbers.Count == 0)
        {
            return new SmsSendResult(SmsSendOutcome.NoTextGroup);
        }

        if (!gateway.IsConfigured)
        {
            return new SmsSendResult(SmsSendOutcome.NotConfigured);
        }

        var messageHash = Sha256Hex(message);
        key ??= AutoKey(claimant.Id, request.ReportId, messageHash, clock.UtcNow);

        var (deliveryId, early) = await ClaimAsync(key, claimant.Id, request.ReportId, messageHash, numbers.Count, ct);
        if (early is not null)
        {
            return early;
        }

        return await DeliverAsync(deliveryId, claimant.Id, request.ReportId, numbers, message);
    }

    public async Task<SmsSendResult> SendAboutReportAsync(Guid reportId, string message, string idempotencyKey, CancellationToken ct = default)
    {
        var claimantId = await db.ExpenseReports.AsNoTracking()
            .Where(r => r.Id == reportId)
            .Select(r => (long?)r.EmployeeUserId)
            .FirstOrDefaultAsync(ct);
        return claimantId is { } id
            ? await SendAsync(new SendSmsRequest(id, message, idempotencyKey, reportId), ct)
            : new SmsSendResult(SmsSendOutcome.ReportNotFound);
    }

    /// <summary>
    /// Mobile numbers of the active Level 1 and Level 2 signatories on the report's current approval cycle,
    /// in step order. Steps filed before scope routing have no level; their first two steps stand in.
    /// </summary>
    private async Task<IReadOnlyList<string?>> SignatoryMobileNumbersAsync(Guid reportId, int cycle, CancellationToken ct)
    {
        var approverIds = (await db.ApprovalSteps.AsNoTracking()
                .Where(s => s.ReportId == reportId && s.Cycle == cycle
                    && (s.SignatoryLevel == 1 || s.SignatoryLevel == 2 || (s.SignatoryLevel == null && s.StepOrder <= 2)))
                .OrderBy(s => s.StepOrder)
                .Select(s => s.ApproverUserId)
                .ToListAsync(ct))
            .Distinct()
            .ToList();
        if (approverIds.Count == 0)
        {
            return [];
        }

        var mobileById = await db.Users.AsNoTracking()
            .Where(u => approverIds.Contains(u.Id) && u.IsActive)
            .ToDictionaryAsync(u => u.Id, u => u.MobileNo, ct);
        return approverIds.Where(mobileById.ContainsKey).Select(id => mobileById[id]).ToList();
    }

    /// <summary>Wins the (sender, key) pair for this request, or says what an earlier request with the key already did.</summary>
    private async Task<(long DeliveryId, SmsSendResult? Early)> ClaimAsync(
        string key, long recipientUserId, Guid? reportId, string messageHash, int recipientCount, CancellationToken ct)
    {
        var existing = await FindAsync(key, ct);
        if (existing is null)
        {
            var row = new SmsDelivery
            {
                SenderUserId = user.UserId,
                IdempotencyKey = key,
                RecipientUserId = recipientUserId,
                ReportId = reportId,
                RecipientCount = recipientCount,
                MessageSha256 = messageHash,
                Status = Sending,
                Attempts = 1,
                CreatedAt = clock.UtcNow,
            };
            db.SmsDeliveries.Add(row);
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
                    return (0, new SmsSendResult(SmsSendOutcome.InProgress));
                }
            }
        }

        if (existing.RecipientUserId != recipientUserId || existing.ReportId != reportId || existing.MessageSha256 != messageHash)
        {
            return (0, new SmsSendResult(SmsSendOutcome.KeyReused));
        }

        switch (existing.Status)
        {
            case Sent:
                return (0, new SmsSendResult(SmsSendOutcome.Sent, existing.RecipientCount, Replayed: true));
            case Unconfirmed:
                return (0, new SmsSendResult(SmsSendOutcome.GatewayUnconfirmed));
            case Failed:
                // Lasius refused the earlier attempt, so nothing went out: this retry may send it. The
                // conditional update lets only one of several simultaneous retries through.
                var claimed = await db.SmsDeliveries
                    .Where(x => x.Id == existing.Id && x.Status == Failed)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, Sending)
                        .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                        .SetProperty(x => x.RecipientCount, recipientCount)
                        .SetProperty(x => x.GatewayHttpStatus, (int?)null)
                        .SetProperty(x => x.CompletedAt, (DateTimeOffset?)null), ct);
                return claimed == 1 ? (existing.Id, null) : (0, new SmsSendResult(SmsSendOutcome.InProgress));
            default:
                return (0, new SmsSendResult(SmsSendOutcome.InProgress));
        }
    }

    private async Task<SmsSendResult> DeliverAsync(long deliveryId, long recipientUserId, Guid? reportId, IReadOnlyList<string> numbers, string message)
    {
        // From here on: CancellationToken.None. The claim is taken, so the send and its record must finish.
        var sender = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
        SmsGatewayResult result;
        try
        {
            result = await gateway.SendAsync(new SmsGatewayMessage(sender, numbers, message), CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Unknown failure inside the gateway client: whether anything left is unknown, so do not resend.
            logger.LogError(ex, "SMS delivery {DeliveryId} failed unexpectedly.", deliveryId);
            result = new SmsGatewayResult(SmsGatewayStatus.Unreachable);
        }

        var (status, outcome) = result.Status switch
        {
            SmsGatewayStatus.Accepted => (Sent, SmsSendOutcome.Sent),
            SmsGatewayStatus.Rejected => (Failed, SmsSendOutcome.GatewayFailed),
            _ => (Unconfirmed, SmsSendOutcome.GatewayUnconfirmed),
        };

        var now = clock.UtcNow;
        await db.SmsDeliveries
            .Where(x => x.Id == deliveryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.GatewayHttpStatus, result.HttpStatus)
                .SetProperty(x => x.CompletedAt, now), CancellationToken.None);

        // Spec 9.9: every SMS sent is in the append-only log. Who and how many, never the text or numbers.
        await audit.WriteAsync(
            new WorkflowAuditEntry(
                $"Sms{status}",
                reportId,
                user.UserId,
                JsonSerializer.Serialize(new { deliveryId, recipientUserId, recipientCount = numbers.Count, gatewayHttpStatus = result.HttpStatus })),
            CancellationToken.None);

        return new SmsSendResult(outcome, numbers.Count);
    }

    private Task<SmsDelivery?> FindAsync(string key, CancellationToken ct) =>
        db.SmsDeliveries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SenderUserId == user.UserId && x.IdempotencyKey == key, ct);

    /// <summary>A key for clients that send none: same recipient, report and message in the same 10-minute window.</summary>
    private static string AutoKey(long recipientUserId, Guid? reportId, string messageHash, DateTimeOffset now)
    {
        var window = now.ToUnixTimeSeconds() / (long)AutoKeyWindow.TotalSeconds;
        return "auto:" + Sha256Hex($"{recipientUserId}|{reportId}|{messageHash}|{window}")[..40];
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };
}
