using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Domain.Notifications;
using ERFWeb.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ERFWeb.Tests.Notifications;

public class SmsApprovalNotifierTests
{
    private static readonly Guid ReportId = Guid.Parse("3f2b8c1e-6a4d-4f0e-9b7a-2c1d5e6f7a8b");

    [Fact]
    public async Task Filing_TextsTheReportOncePerCycle()
    {
        var sms = new FakeSms();
        await Create(sms).NotifyReportFiledAsync(Filed(cycle: 1, isRefile: false));

        var call = Assert.Single(sms.Calls);
        Assert.Equal(ReportId, call.ReportId);
        Assert.Equal("ERF-2026-000001 was filed by Jay Bryan Abaoag and is waiting for approval in ERFWeb.", call.Message);
        Assert.Equal("filed:3f2b8c1e6a4d4f0e9b7a2c1d5e6f7a8b:1", call.Key);
        Assert.Null(SmsRules.MessageError(call.Message));
        Assert.Null(SmsRules.IdempotencyKeyError(call.Key));
    }

    [Fact]
    public async Task Refiling_IsANewSmsForTheNewCycle()
    {
        var sms = new FakeSms();
        await Create(sms).NotifyReportFiledAsync(Filed(cycle: 2, isRefile: true));

        var call = Assert.Single(sms.Calls);
        Assert.StartsWith("ERF-2026-000001 was re-filed by Jay Bryan Abaoag", call.Message);
        Assert.EndsWith(":2", call.Key);
    }

    [Fact]
    public async Task Filing_NeverThrowsWhenTheSmsFails()
    {
        var notifier = Create(new FakeSms(throws: true));

        await notifier.NotifyReportFiledAsync(Filed(cycle: 1, isRefile: false));
    }

    [Fact]
    public async Task OtherWorkflowEvents_SendNoSms()
    {
        var sms = new FakeSms();
        await Create(sms).NotifyWorkflowEventAsync(
            new WorkflowEventNotification("Approved", ReportId, "ERF-2026-000001", "Jay Bryan Abaoag", [], "Approved by Level 1"));

        Assert.Empty(sms.Calls);
    }

    [Fact]
    public async Task LevelOneApproval_SaysWhoIsNext()
    {
        var sms = new FakeSms();
        await Create(sms).NotifyWorkflowEventAsync(Decision(ApprovalAuditEvents.Approved, step: 1, next: "Test Manager Two"));

        var call = Assert.Single(sms.Calls);
        Assert.Equal("ERF-2026-000001 was approved by Test Manager One and is now waiting for Test Manager Two's approval in ERFWeb.", call.Message);
        Assert.Equal("ReportStepApproved:3f2b8c1e6a4d4f0e9b7a2c1d5e6f7a8b:1:1", call.Key);
    }

    [Fact]
    public void FinalApproval_AsksForTheReceipts()
    {
        var sms = SmsApprovalNotifier.ComposeWorkflowSms(Decision(ApprovalAuditEvents.FullyApproved, step: 2, actor: "Test Manager Two"));

        Assert.NotNull(sms);
        Assert.Equal("ERF-2026-000001 was fully approved by Test Manager Two. Please submit the original receipts to Finance.", sms.Value.Message);
        Assert.Equal("ReportApproved:3f2b8c1e6a4d4f0e9b7a2c1d5e6f7a8b:1:2", sms.Value.Key);
    }

    [Fact]
    public void Return_QuotesTheNote()
    {
        var sms = SmsApprovalNotifier.ComposeWorkflowSms(Decision(ApprovalAuditEvents.Returned, step: 1, note: "Lunch on Oct 5 has no OR."));

        Assert.Equal(
            "ERF-2026-000001 was returned by Test Manager One: \"Lunch on Oct 5 has no OR.\". Please update and re-file it in ERFWeb.",
            sms?.Message);
    }

    [Fact]
    public void Return_ShortensALongNoteToFitOneNotification()
    {
        var sms = SmsApprovalNotifier.ComposeWorkflowSms(Decision(ApprovalAuditEvents.Returned, step: 1, note: new string('x', 1000)));

        Assert.NotNull(sms);
        Assert.Null(SmsRules.MessageError(sms.Value.Message));
        Assert.Contains("...\". Please update and re-file it in ERFWeb.", sms.Value.Message);
        Assert.Null(SmsRules.IdempotencyKeyError(sms.Value.Key));
    }

    [Theory]
    [InlineData(ApprovalAuditEvents.Rejected)]
    [InlineData(ApprovalAuditEvents.Overridden)]
    [InlineData(ApprovalAuditEvents.LineAdjusted)]
    public void OtherDecisions_DoNotText(string kind) =>
        Assert.Null(SmsApprovalNotifier.ComposeWorkflowSms(Decision(kind, step: 1)));

    private static WorkflowEventNotification Decision(string kind, int step, string actor = "Test Manager One", string? note = null, string? next = null) =>
        new(kind, ReportId, "ERF-2026-000001", "Jay Bryan Abaoag", [], "summary", actor, note, next, Cycle: 1, StepOrder: step);

    private static SmsApprovalNotifier Create(ISmsService sms) =>
        new(new LoggingApprovalNotifier(NullLogger<LoggingApprovalNotifier>.Instance), sms, NullLogger<SmsApprovalNotifier>.Instance);

    private static ReportFiledNotification Filed(int cycle, bool isRefile) =>
        new(ReportId, "ERF-2026-000001", "Jay Bryan Abaoag", 7, "Test Manager One", null, "09171234567", cycle, isRefile);

    private sealed class FakeSms(bool throws = false) : ISmsService
    {
        public List<(Guid ReportId, string Message, string Key)> Calls { get; } = [];

        public Task<SmsSendResult> SendAsync(SendSmsRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<SmsSendResult> SendAboutReportAsync(Guid reportId, string message, string idempotencyKey, CancellationToken ct = default)
        {
            if (throws)
            {
                throw new InvalidOperationException("gateway exploded");
            }

            Calls.Add((reportId, message, idempotencyKey));
            return Task.FromResult(new SmsSendResult(SmsSendOutcome.Sent, 3));
        }
    }
}
