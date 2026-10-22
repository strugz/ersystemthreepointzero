using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Domain.Notifications;
using ERFWeb.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ERFWeb.Tests.Notifications;

public class EmailApprovalNotifierTests
{
    private static readonly Guid ReportId = Guid.Parse("3f2b8c1e-6a4d-4f0e-9b7a-2c1d5e6f7a8b");

    [Fact]
    public async Task Filing_EmailsOncePerCycleAndStillTexts()
    {
        var (email, sms) = (new FakeEmail(), new FakeSms());
        await Create(email, sms).NotifyReportFiledAsync(Filed(cycle: 2, isRefile: true));

        var call = Assert.Single(email.Calls);
        Assert.Equal(ReportId, call.ReportId);
        Assert.Equal(EmailApprovalNotifier.FiledKind, call.Content.Kind);
        Assert.Equal("ERF-2026-000001 re-filed by Jay Bryan Abaoag - for approval", call.Content.Subject);
        Assert.Contains("waiting for Test Manager One's approval", call.Content.Body);
        Assert.Equal("filed:3f2b8c1e6a4d4f0e9b7a2c1d5e6f7a8b:2", call.Key);
        Assert.Null(EmailRules.IdempotencyKeyError(call.Key));
        Assert.Single(sms.Calls);
    }

    [Fact]
    public async Task Return_QuotesTheRemarks()
    {
        var email = new FakeEmail();
        await Create(email, new FakeSms()).NotifyWorkflowEventAsync(Decision(ApprovalAuditEvents.Returned, step: 1, note: "Attach the toll receipt."));

        var call = Assert.Single(email.Calls);
        Assert.Equal("ERF-2026-000001 returned by Test Manager One", call.Content.Subject);
        Assert.Contains("Remarks: Attach the toll receipt.", call.Content.Body);
        Assert.Equal("ReportReturned:3f2b8c1e6a4d4f0e9b7a2c1d5e6f7a8b:1:1", call.Key);
    }

    [Theory]
    [InlineData(ApprovalAuditEvents.Approved)]
    [InlineData(ApprovalAuditEvents.FullyApproved)]
    [InlineData(ApprovalAuditEvents.Rejected)]
    public void ApprovalDecisions_Email(string kind) =>
        Assert.Equal(kind, EmailApprovalNotifier.ComposeWorkflowEmail(Decision(kind, step: 2))?.Content.Kind);

    [Theory]
    [InlineData(ApprovalAuditEvents.Overridden)]
    [InlineData(ApprovalAuditEvents.LineAdjusted)]
    [InlineData(ApprovalAuditEvents.Cleared)]
    public void OtherEvents_DoNotEmail(string kind) =>
        Assert.Null(EmailApprovalNotifier.ComposeWorkflowEmail(Decision(kind, step: 1)));

    [Fact]
    public void EventsWithoutACycle_DoNotEmail() =>
        Assert.Null(EmailApprovalNotifier.ComposeWorkflowEmail(
            new WorkflowEventNotification(ApprovalAuditEvents.Approved, ReportId, "ERF-2026-000001", "Jay Bryan Abaoag", [], "Approved")));

    [Fact]
    public async Task Notifier_NeverThrowsWhenTheEmailFails() =>
        await Create(new FakeEmail(throws: true), new FakeSms()).NotifyWorkflowEventAsync(Decision(ApprovalAuditEvents.FullyApproved, step: 2));

    [Fact]
    public void SmtpOptions_NeedAHostAndASender()
    {
        Assert.False(new SmtpOptions().IsConfigured);
        Assert.False(new SmtpOptions { Host = "smtp.example.test" }.IsConfigured);
        Assert.False(new SmtpOptions { Host = "smtp.example.test", FromAddress = "erf@example.test", Security = "Tls13" }.IsConfigured);
        Assert.True(new SmtpOptions { Host = "smtp.example.test", FromAddress = "erf@example.test", Security = "sslonconnect" }.IsConfigured);
    }

    [Fact]
    public async Task Gateway_ReportsANotSentWhenTheServerCannotBeReached()
    {
        var options = Options.Create(new SmtpOptions { Host = "127.0.0.1", Port = 1, Security = "None", FromAddress = "erf@example.test", TimeoutSeconds = 5 });
        var gateway = new MailKitEmailGateway(options, NullLogger<MailKitEmailGateway>.Instance);

        var result = await gateway.SendAsync(new EmailMessage(["jay@example.test"], "Subject", "Body", []));

        Assert.Equal(EmailGatewayStatus.Rejected, result.Status);
    }

    [Fact]
    public void Gateway_SendsAsTheUserOnBehalfOfTheMailbox()
    {
        using var mime = Gateway().Compose(new EmailMessage(["l1@example.test"], "Subject", "Body", [], new EmailSender("Jay Bryan Abaoag", "jay@example.test")));

        var from = Assert.IsType<MimeKit.MailboxAddress>(Assert.Single(mime.From));
        Assert.Equal(("Jay Bryan Abaoag", "jay@example.test"), (from.Name, from.Address));
        Assert.Equal("jay@example.test", Assert.IsType<MimeKit.MailboxAddress>(Assert.Single(mime.ReplyTo)).Address);
        Assert.Equal("erf@example.test", mime.Sender?.Address);
    }

    [Fact]
    public void Gateway_SendsFromTheMailboxWhenTheUserHasNoAddress()
    {
        using var mime = Gateway().Compose(new EmailMessage(["l1@example.test"], "Subject", "Body", []));

        Assert.Equal("erf@example.test", Assert.IsType<MimeKit.MailboxAddress>(Assert.Single(mime.From)).Address);
        Assert.Null(mime.Sender);
        Assert.Empty(mime.ReplyTo);
    }

    private static MailKitEmailGateway Gateway() =>
        new(Options.Create(new SmtpOptions { Host = "smtp.example.test", FromAddress = "erf@example.test" }), NullLogger<MailKitEmailGateway>.Instance);

    private static EmailApprovalNotifier Create(IReportEmailService email, ISmsService sms) =>
        new(
            new SmsApprovalNotifier(new LoggingApprovalNotifier(NullLogger<LoggingApprovalNotifier>.Instance), sms, NullLogger<SmsApprovalNotifier>.Instance),
            email,
            NullLogger<EmailApprovalNotifier>.Instance);

    private static ReportFiledNotification Filed(int cycle, bool isRefile) =>
        new(ReportId, "ERF-2026-000001", "Jay Bryan Abaoag", 7, "Test Manager One", "l1@example.test", null, cycle, isRefile);

    private static WorkflowEventNotification Decision(string kind, int step, string? note = null) =>
        new(kind, ReportId, "ERF-2026-000001", "Jay Bryan Abaoag", [], "decision",
            ActorName: "Test Manager One", Note: note, Cycle: 1, StepOrder: step);

    private sealed class FakeEmail(bool throws = false) : IReportEmailService
    {
        public List<(Guid ReportId, ReportEmailContent Content, string Key)> Calls { get; } = [];

        public Task<EmailSendResult> SendR47Async(Guid reportId, SendReportEmailRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<EmailSendResult> SendAboutReportAsync(Guid reportId, ReportEmailContent content, string idempotencyKey, CancellationToken ct = default)
        {
            if (throws)
            {
                throw new InvalidOperationException("SMTP down");
            }

            Calls.Add((reportId, content, idempotencyKey));
            return Task.FromResult(new EmailSendResult(EmailSendOutcome.Sent, 3));
        }

        public Task<IReadOnlyList<ReportEmailDeliveryDto>> HistoryAsync(Guid reportId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSms : ISmsService
    {
        public List<string> Calls { get; } = [];

        public Task<SmsSendResult> SendAsync(SendSmsRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<SmsSendResult> SendAboutReportAsync(Guid reportId, string message, string idempotencyKey, CancellationToken ct = default)
        {
            Calls.Add(idempotencyKey);
            return Task.FromResult(new SmsSendResult(SmsSendOutcome.Sent, 1));
        }
    }
}
