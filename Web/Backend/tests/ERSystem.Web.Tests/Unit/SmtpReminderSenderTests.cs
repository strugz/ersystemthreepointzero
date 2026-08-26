using ERSystem.Web.Application.Features.ApprovalReminders;
using ERSystem.Web.Infrastructure.Reminders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ERSystem.Web.Tests.Unit;

public sealed class SmtpReminderSenderTests
{
    [Fact]
    public async Task Employee_audience_fails_when_configured_notification_email_is_invalid()
    {
        var sender = CreateSender(new EmployeeSmtpAccount(
            "employee@example.test",
            "mailbox-password",
            "manager@example.test",
            "not a mailbox address <<"));

        var result = await sender.SendAsync(
            42,
            ReminderAudience.Employee,
            new ReminderEmail("Subject", "Body"),
            CancellationToken.None);

        Assert.Equal(ReminderSendOutcome.Failed, result.Outcome);
        Assert.Equal("NOTIFICATION_EMAIL_INVALID", result.FailureCode);
    }

    [Fact]
    public async Task Manager_audience_skips_when_manager_address_is_missing_regardless_of_notification_email()
    {
        var sender = CreateSender(new EmployeeSmtpAccount(
            "employee@example.test",
            "mailbox-password",
            null,
            "reminder@example.test"));

        var result = await sender.SendAsync(
            42,
            ReminderAudience.Manager,
            new ReminderEmail("Subject", "Body"),
            CancellationToken.None);

        Assert.Equal(ReminderSendOutcome.Skipped, result.Outcome);
        Assert.Equal("MANAGER_EMAIL_ADDRESS_MISSING", result.FailureCode);
    }

    private static SmtpReminderSender CreateSender(EmployeeSmtpAccount account) =>
        new(
            new FakeAccountProvider(account),
            Options.Create(new SmtpReminderOptions { Host = "smtp.example.test" }),
            NullLogger<SmtpReminderSender>.Instance);

    private sealed class FakeAccountProvider(EmployeeSmtpAccount account) : IEmployeeSmtpAccountProvider
    {
        public Task<EmployeeSmtpAccountResolution> ResolveAsync(
            int employeeUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(EmployeeSmtpAccountResolution.Found(account));
    }
}
