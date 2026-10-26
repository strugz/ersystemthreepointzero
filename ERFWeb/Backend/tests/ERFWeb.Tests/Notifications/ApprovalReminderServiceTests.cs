using ERFWeb.Domain;
using ERFWeb.Domain.Notifications;
using ERFWeb.Infrastructure.Notifications;
using Xunit;

namespace ERFWeb.Tests.Notifications;

public class ApprovalReminderServiceTests
{
    private static StepRow Step(string? reference = "ERF-2026-000142") =>
        new(7, Guid.NewGuid(), 1, 1, 20, ApprovalStatus.Pending, null, reference, 10, DateTimeOffset.UtcNow);

    [Fact]
    public void ApproverReminder_NamesTheReportAndTheWait()
    {
        var plan = new ReminderPlan(Step(), 1, ApprovalReminderService.ApproverAudience, [20], 3);

        var text = ApprovalReminderService.Compose(plan, "Juan Dela Cruz", "Maria Santos");

        Assert.Equal("Reminder: ERF-2026-000142 is waiting for your approval", text.Subject);
        Assert.Equal("Reminder: ERF-2026-000142 of Juan Dela Cruz has waited 3 working days for your approval in ERFWeb.", text.Sms);
    }

    [Fact]
    public void Level2Escalation_NamesTheLevel1Approver()
    {
        var plan = new ReminderPlan(Step(), 1, ApprovalReminderService.Level2Audience, [30], 1);

        var text = ApprovalReminderService.Compose(plan, "Juan Dela Cruz", "Maria Santos");

        Assert.Contains("1 working day for Maria Santos's Level 1 approval", text.Sms, StringComparison.Ordinal);
        Assert.Contains("Level 2 signatory", text.EmailBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Texts_WithLongNames_StillFitTheSmsLimit()
    {
        var longName = new string('N', 60);
        foreach (var audience in new[] { ApprovalReminderService.ApproverAudience, ApprovalReminderService.Level2Audience })
        {
            var text = ApprovalReminderService.Compose(new ReminderPlan(Step(), 4, audience, [20], 120), longName, longName);
            Assert.Null(SmsRules.MessageError(text.Sms));
        }
    }

    [Fact]
    public void UnnumberedReport_IsStillDescribed()
    {
        var text = ApprovalReminderService.Compose(new ReminderPlan(Step(null), 1, ApprovalReminderService.ApproverAudience, [20], 3), "A", "B");

        Assert.StartsWith("Reminder: An expense report of A", text.Sms, StringComparison.Ordinal);
    }
}
