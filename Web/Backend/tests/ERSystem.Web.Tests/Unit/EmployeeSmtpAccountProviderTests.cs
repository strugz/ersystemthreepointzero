using ERSystem.Web.Application.Features.ApprovalReminders;
using ERSystem.Web.Infrastructure.Reminders;
using ERSystem.Web.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace ERSystem.Web.Tests.Unit;

public sealed class EmployeeSmtpAccountProviderTests
{
    [Fact]
    public async Task Provider_decrypts_employee_mailbox_and_preserves_plain_manager_address()
    {
        var cipher = CreateCipher();
        var store = new FakeStore(new EncryptedEmployeeSmtpAccount(
            cipher.Encrypt("employee@example.test"),
            cipher.Encrypt("mailbox-password"),
            "manager@example.test",
            "reminder@example.test"));
        var provider = new EmployeeSmtpAccountProvider(store, cipher);

        var resolution = await provider.ResolveAsync(42, CancellationToken.None);

        Assert.Null(resolution.Failure);
        Assert.Equal("employee@example.test", resolution.Account?.SenderAddress);
        Assert.Equal("mailbox-password", resolution.Account?.Password);
        Assert.Equal("manager@example.test", resolution.Account?.ManagerEmailAddress);
        Assert.Equal("reminder@example.test", resolution.Account?.NotificationEmailAddress);
    }

    [Fact]
    public async Task Provider_preserves_missing_notification_email_as_null()
    {
        var cipher = CreateCipher();
        var store = new FakeStore(new EncryptedEmployeeSmtpAccount(
            cipher.Encrypt("employee@example.test"),
            cipher.Encrypt("mailbox-password"),
            "manager@example.test",
            null));
        var provider = new EmployeeSmtpAccountProvider(store, cipher);

        var resolution = await provider.ResolveAsync(42, CancellationToken.None);

        Assert.Null(resolution.Failure);
        Assert.Null(resolution.Account?.NotificationEmailAddress);
    }

    [Fact]
    public async Task Provider_caches_decrypted_account_for_the_current_scope()
    {
        var cipher = CreateCipher();
        var store = new FakeStore(new EncryptedEmployeeSmtpAccount(
            cipher.Encrypt("employee@example.test"),
            cipher.Encrypt("mailbox-password"),
            "manager@example.test",
            null));
        var provider = new EmployeeSmtpAccountProvider(store, cipher);

        await provider.ResolveAsync(42, CancellationToken.None);
        await provider.ResolveAsync(42, CancellationToken.None);

        Assert.Equal(1, store.CallCount);
        Assert.Equal(42, store.LastEmployeeUserId);
    }

    [Theory]
    [InlineData(null, "password", ReminderSendOutcome.Skipped, "SMTP_SENDER_ADDRESS_MISSING")]
    [InlineData("address", null, ReminderSendOutcome.Skipped, "SMTP_PASSWORD_MISSING")]
    public async Task Provider_returns_sanitized_outcome_for_missing_mailbox_fields(
        string? encryptedAddress,
        string? encryptedPassword,
        ReminderSendOutcome expectedOutcome,
        string expectedCode)
    {
        var provider = new EmployeeSmtpAccountProvider(
            new FakeStore(new EncryptedEmployeeSmtpAccount(
                encryptedAddress,
                encryptedPassword,
                "manager@example.test",
                null)),
            CreateCipher());

        var resolution = await provider.ResolveAsync(42, CancellationToken.None);

        Assert.Null(resolution.Account);
        Assert.Equal(expectedOutcome, resolution.Failure?.Outcome);
        Assert.Equal(expectedCode, resolution.Failure?.FailureCode);
    }

    [Fact]
    public async Task Provider_reports_decryption_failure_without_exposing_ciphertext()
    {
        var provider = new EmployeeSmtpAccountProvider(
            new FakeStore(new EncryptedEmployeeSmtpAccount(
                "not-hex",
                "also-not-hex",
                "manager@example.test",
                null)),
            CreateCipher());

        var resolution = await provider.ResolveAsync(42, CancellationToken.None);

        Assert.Null(resolution.Account);
        Assert.Equal(ReminderSendOutcome.Failed, resolution.Failure?.Outcome);
        Assert.Equal("LEGACY_EMAIL_DECRYPTION_FAILED", resolution.Failure?.FailureCode);
    }

    [Fact]
    public async Task Provider_reports_missing_employee_row()
    {
        var provider = new EmployeeSmtpAccountProvider(new FakeStore(null), CreateCipher());

        var resolution = await provider.ResolveAsync(42, CancellationToken.None);

        Assert.Null(resolution.Account);
        Assert.Equal(ReminderSendOutcome.Failed, resolution.Failure?.Outcome);
        Assert.Equal("SMTP_EMPLOYEE_ACCOUNT_NOT_FOUND", resolution.Failure?.FailureCode);
    }

    private static LegacyPasswordCipher CreateCipher() =>
        new(Options.Create(new LegacyAuthenticationOptions { EncryptionKey = "test-key" }));

    private sealed class FakeStore(EncryptedEmployeeSmtpAccount? account) : IEmployeeSmtpAccountStore
    {
        public int CallCount { get; private set; }
        public int LastEmployeeUserId { get; private set; }

        public Task<EncryptedEmployeeSmtpAccount?> FindAsync(
            int employeeUserId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastEmployeeUserId = employeeUserId;
            return Task.FromResult(account);
        }
    }
}
