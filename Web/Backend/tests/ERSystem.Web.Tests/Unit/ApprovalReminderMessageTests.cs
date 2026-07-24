using ERSystem.Web.Application.Features.ApprovalReminders;

namespace ERSystem.Web.Tests.Unit;

public sealed class ApprovalReminderMessageTests
{
    private static readonly ApprovalReminderSettings Settings = new(
        EmailEnabled: true,
        SmsEnabled: true,
        ActivationPollIntervalSeconds: 60,
        TimeZoneId: "UTC",
        RunAtLocalTime: new TimeOnly(8, 0),
        InitialDelayDays: 3,
        ReminderDayOfWeek: DayOfWeek.Wednesday,
        ManagerPortalBaseUrl: "https://er.example.test");

    [Fact]
    public void Creates_the_reviewed_email_and_sms_messages()
    {
        var messages = new ApprovalReminderMessageFactory(Settings).CreateReminder(CreateCandidate(), 3);

        Assert.Equal(
            "Reminder: ERF ERF-2026-00421 for JSMITH is still awaiting approval from MCRUZ after 3 days. Please review or follow up.",
            messages.SmsMessage);
        Assert.Equal("[ER System] Approval reminder - ERF ERF-2026-00421", messages.ManagerEmail.Subject);
        Assert.Contains("Hello Maria Cruz,", messages.ManagerEmail.Body, StringComparison.Ordinal);
        Assert.Contains("ERF ERF-2026-00421, filed by John Smith", messages.ManagerEmail.Body, StringComparison.Ordinal);
        Assert.Contains("https://er.example.test/manager/reports/RPT-421", messages.ManagerEmail.Body, StringComparison.Ordinal);
        Assert.Contains("repeat every Wednesday", messages.ManagerEmail.Body, StringComparison.Ordinal);
        Assert.Equal("[ER System] Your ERF ERF-2026-00421 is awaiting approval", messages.EmployeeEmail.Subject);
        Assert.Contains("A reminder was also sent to the manager.", messages.EmployeeEmail.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Creates_activation_emails_without_an_sms_message()
    {
        var messages = new ApprovalReminderMessageFactory(Settings).CreateActivation(CreateCandidate());

        Assert.Equal("[ER System] Approval required - ERF ERF-2026-00421", messages.ManagerEmail.Subject);
        Assert.Contains("is now awaiting your approval", messages.ManagerEmail.Body, StringComparison.Ordinal);
        Assert.Contains("https://er.example.test/manager/reports/RPT-421", messages.ManagerEmail.Body, StringComparison.Ordinal);
        Assert.Equal("[ER System] Your ERF ERF-2026-00421 was filed", messages.EmployeeEmail.Subject);
        Assert.Contains(
            "was filed successfully and is now awaiting approval from Maria Cruz",
            messages.EmployeeEmail.Body,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Falls_back_to_report_id_and_sanitizes_the_completed_sms()
    {
        var candidate = CreateCandidate() with
        {
            ErfReferenceNumber = null,
            ReportId = "RPT|421\r\n",
            EmployeeUsername = new string('E', 400)
        };

        var messages = new ApprovalReminderMessageFactory(Settings).CreateReminder(candidate, 6);

        Assert.StartsWith("Reminder: ERF RPT 421", messages.SmsMessage, StringComparison.Ordinal);
        Assert.DoesNotContain('|', messages.SmsMessage);
        Assert.DoesNotContain('\r', messages.SmsMessage);
        Assert.DoesNotContain('\n', messages.SmsMessage);
        Assert.True(messages.SmsMessage.Length <= 320);
        Assert.DoesNotContain("expense", messages.SmsMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static ApprovalReminderCandidate CreateCandidate() => new(
        ApprovalTransactionId: 10,
        ReportId: "RPT-421",
        ApprovalCycle: 2,
        EmployeeUserId: 11,
        EmployeeUsername: "JSMITH",
        EmployeeFullName: "John Smith",
        EmployeeNotificationEmail: "john@example.test",
        ManagerUserId: 12,
        ManagerUsername: "MCRUZ",
        ManagerFullName: "Maria Cruz",
        ManagerNotificationEmail: "maria@example.test",
        ErfReferenceNumber: "ERF-2026-00421",
        ActiveAtUtc: new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc));
}
