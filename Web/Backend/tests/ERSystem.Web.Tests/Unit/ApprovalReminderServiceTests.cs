using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.ApprovalReminders;
using ERSystem.Web.Domain.ApprovalReminders;

namespace ERSystem.Web.Tests.Unit;

public sealed class ApprovalReminderServiceTests
{
    [Theory]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(8, 1)]
    [InlineData(9, 2)]
    [InlineData(16, 3)]
    public void Schedule_uses_day_three_then_the_latest_wednesday(
        int elapsedDays,
        int expectedReminder)
    {
        var activeAt = new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
        var now = activeAt.AddDays(elapsedDays);

        var reminder = ApprovalReminderSchedule.GetLatestDueReminderNumber(
            activeAt,
            now,
            TimeZoneInfo.Utc,
            3,
            DayOfWeek.Wednesday);

        Assert.Equal(expectedReminder, reminder);
    }

    [Fact]
    public void Schedule_does_not_double_send_when_day_three_is_wednesday()
    {
        var activeAt = new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc);

        var dayThreeReminder = ApprovalReminderSchedule.GetLatestDueReminderNumber(
            activeAt,
            new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc),
            TimeZoneInfo.Utc,
            3,
            DayOfWeek.Wednesday);
        var followingWednesdayReminder = ApprovalReminderSchedule.GetLatestDueReminderNumber(
            activeAt,
            new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc),
            TimeZoneInfo.Utc,
            3,
            DayOfWeek.Wednesday);

        Assert.Equal(1, dayThreeReminder);
        Assert.Equal(2, followingWednesdayReminder);
    }

    [Fact]
    public void Schedule_uses_manila_calendar_dates_instead_of_elapsed_hours()
    {
        var manilaTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Test Manila",
            TimeSpan.FromHours(8),
            "Test Manila",
            "Test Manila");
        var activeAtUtc = new DateTime(2026, 7, 20, 16, 30, 0, DateTimeKind.Utc);

        var beforeLocalDayThree = ApprovalReminderSchedule.GetLatestDueReminderNumber(
            activeAtUtc,
            new DateTime(2026, 7, 23, 15, 59, 0, DateTimeKind.Utc),
            manilaTimeZone,
            3,
            DayOfWeek.Wednesday);
        var onLocalDayThree = ApprovalReminderSchedule.GetLatestDueReminderNumber(
            activeAtUtc,
            new DateTime(2026, 7, 23, 16, 0, 0, DateTimeKind.Utc),
            manilaTimeZone,
            3,
            DayOfWeek.Wednesday);

        Assert.Equal(0, beforeLocalDayThree);
        Assert.Equal(1, onLocalDayThree);
    }

    [Fact]
    public async Task Activation_scan_emails_manager_and_employee_without_sms()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var emailSender = new FakeEmailSender(ReminderSendResult.Success);
        var smsSender = new FakeSmsSender(ReminderSendResult.Success);
        var service = CreateService(
            repository,
            emailSender,
            smsSender,
            new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc));

        var summary = await service.RunActivationNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, summary.CandidatesFound);
        Assert.Equal(1, summary.DueCandidates);
        Assert.Equal(2, summary.EmailSent);
        Assert.Equal(2, repository.Claims.Count);
        Assert.All(repository.Claims, claim => Assert.Equal(0, claim.ReminderNumber));
        Assert.All(repository.Claims, claim => Assert.Equal(ReminderChannel.Email, claim.Channel));
        Assert.Equal(2, emailSender.Messages.Count);
        Assert.Contains(
            emailSender.Messages,
            sent => sent.EmployeeUserId == 11 && sent.Audience == ReminderAudience.Manager);
        Assert.Contains(
            emailSender.Messages,
            sent => sent.EmployeeUserId == 11 && sent.Audience == ReminderAudience.Employee);
        Assert.Empty(smsSender.Messages);
    }

    [Fact]
    public async Task Disabled_email_claims_activation_as_skipped()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var emailSender = new FakeEmailSender(ReminderSendResult.Success);
        var service = CreateService(
            repository,
            emailSender,
            new FakeSmsSender(ReminderSendResult.Success),
            new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc),
            emailEnabled: false,
            smsEnabled: false);

        var summary = await service.RunActivationNotificationsAsync(CancellationToken.None);

        Assert.Equal(2, summary.Skipped);
        Assert.Empty(emailSender.Messages);
        Assert.Equal(2, repository.Completions.Count);
        Assert.All(repository.Completions, item =>
        {
            Assert.Equal(ReminderDeliveryStatus.Skipped, item.Status);
            Assert.Equal("EMAIL_DISABLED", item.FailureCode);
        });
    }

    [Fact]
    public async Task Expired_activation_is_skipped_so_day_three_is_the_only_message()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var emailSender = new FakeEmailSender(ReminderSendResult.Success);
        var service = CreateService(
            repository,
            emailSender,
            new FakeSmsSender(ReminderSendResult.Success),
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc));

        var summary = await service.RunActivationNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, summary.DueCandidates);
        Assert.Equal(2, summary.Skipped);
        Assert.Empty(emailSender.Messages);
        Assert.All(
            repository.Completions,
            item => Assert.Equal("ACTIVATION_EXPIRED", item.FailureCode));
    }

    [Fact]
    public async Task Scheduled_due_candidate_processes_each_channel_independently()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var emailSender = new FakeEmailSender(ReminderSendResult.Success);
        var smsSender = new FakeSmsSender(ReminderSendResult.Success);
        var service = CreateService(
            repository,
            emailSender,
            smsSender,
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc));

        var summary = await service.RunScheduledRemindersAsync(CancellationToken.None);

        Assert.Equal(1, summary.DueCandidates);
        Assert.Equal(2, summary.EmailSent);
        Assert.Equal(1, summary.SmsSent);
        Assert.Equal(2, emailSender.Messages.Count);
        var sms = Assert.Single(smsSender.Messages);
        Assert.Equal("MCRUZ", sms.ReceiverUsername);
        Assert.Equal("JSMITH", sms.SenderUsername);
        Assert.Equal(3, repository.Claims.Count);
        Assert.All(repository.Claims, claim => Assert.Equal(1, claim.ReminderNumber));
        Assert.Equal(3, repository.Completions.Count(item => item.Status == ReminderDeliveryStatus.Sent));
    }

    [Fact]
    public async Task Missed_runs_claim_only_the_latest_due_occurrence()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var service = CreateService(
            repository,
            new FakeEmailSender(ReminderSendResult.Success),
            new FakeSmsSender(ReminderSendResult.Success),
            new DateTime(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc));

        await service.RunScheduledRemindersAsync(CancellationToken.None);

        Assert.All(repository.Claims, claim => Assert.Equal(3, claim.ReminderNumber));
        Assert.DoesNotContain(repository.Claims, claim => claim.ReminderNumber is 1 or 2);
    }

    [Fact]
    public async Task Disabled_scheduled_channels_still_report_real_database_counts()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var service = CreateService(
            repository,
            new FakeEmailSender(ReminderSendResult.Success),
            new FakeSmsSender(ReminderSendResult.Success),
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc),
            emailEnabled: false,
            smsEnabled: false);

        var summary = await service.RunScheduledRemindersAsync(CancellationToken.None);

        Assert.Equal(1, repository.QueryCount);
        Assert.Equal(1, summary.CandidatesFound);
        Assert.Equal(1, summary.DueCandidates);
        Assert.Empty(repository.Claims);
    }

    [Fact]
    public async Task Manager_address_failure_does_not_block_employee_email_or_scheduled_sms()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var emailSender = new FakeEmailSender((_, audience) =>
            audience == ReminderAudience.Manager
                ? ReminderSendResult.Skipped("MANAGER_EMAIL_ADDRESS_MISSING")
                : ReminderSendResult.Success);
        var smsSender = new FakeSmsSender(ReminderSendResult.Success);
        var service = CreateService(
            repository,
            emailSender,
            smsSender,
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc));

        var summary = await service.RunScheduledRemindersAsync(CancellationToken.None);

        Assert.Equal(1, summary.Skipped);
        Assert.Equal(1, summary.EmailSent);
        Assert.Equal(1, summary.SmsSent);
        Assert.Equal(2, emailSender.Messages.Count);
        Assert.Single(smsSender.Messages);
    }

    [Fact]
    public async Task Existing_claims_do_not_send_duplicate_scheduled_messages()
    {
        var repository = new FakeRepository([CreateCandidate()]) { AllowClaims = false };
        var emailSender = new FakeEmailSender(ReminderSendResult.Success);
        var smsSender = new FakeSmsSender(ReminderSendResult.Success);
        var service = CreateService(
            repository,
            emailSender,
            smsSender,
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc));

        var summary = await service.RunScheduledRemindersAsync(CancellationToken.None);

        Assert.Equal(3, summary.AlreadyClaimed);
        Assert.Empty(emailSender.Messages);
        Assert.Empty(smsSender.Messages);
    }

    [Fact]
    public async Task Matching_employee_and_manager_username_still_claims_only_manager_sms()
    {
        var candidate = CreateCandidate() with { ManagerUsername = "jsmith" };
        var repository = new FakeRepository([candidate]);
        var smsSender = new FakeSmsSender(ReminderSendResult.Success);
        var service = CreateService(
            repository,
            new FakeEmailSender(ReminderSendResult.Success),
            smsSender,
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc),
            emailEnabled: false);

        var summary = await service.RunScheduledRemindersAsync(CancellationToken.None);

        Assert.Single(smsSender.Messages);
        Assert.Equal(1, summary.SmsSent);
        var claim = Assert.Single(repository.Claims);
        Assert.Equal(ReminderAudience.Manager, claim.Audience);
    }

    [Fact]
    public async Task Manager_sms_failure_marks_manager_claim_failed()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var smsSender = new FakeSmsSender(
            ReminderSendResult.Failed("SMS_API_CONNECTION_FAILED"));
        var service = CreateService(
            repository,
            new FakeEmailSender(ReminderSendResult.Success),
            smsSender,
            new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc),
            emailEnabled: false);

        var summary = await service.RunScheduledRemindersAsync(CancellationToken.None);

        Assert.Single(smsSender.Messages);
        Assert.Equal(0, summary.SmsSent);
        Assert.Equal(1, summary.Failed);
    }

    private static ApprovalReminderService CreateService(
        FakeRepository repository,
        FakeEmailSender emailSender,
        FakeSmsSender smsSender,
        DateTime nowUtc,
        bool emailEnabled = true,
        bool smsEnabled = true)
    {
        var settings = new ApprovalReminderSettings(
            emailEnabled,
            smsEnabled,
            60,
            "UTC",
            new TimeOnly(8, 0),
            3,
            DayOfWeek.Wednesday);
        return new ApprovalReminderService(
            repository,
            emailSender,
            smsSender,
            new ApprovalReminderMessageFactory(settings),
            settings,
            new FakeClock(nowUtc));
    }

    private static ApprovalReminderCandidate CreateCandidate() => new(
        10,
        "RPT-421",
        2,
        11,
        "JSMITH",
        "John Smith",
        12,
        "MCRUZ",
        "Maria Cruz",
        "ERF-2026-00421",
        new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc));

    private sealed class FakeClock(DateTime nowUtc) : IClock
    {
        public DateTime UtcNow => nowUtc;
    }

    private sealed class FakeEmailSender : IEmailReminderSender
    {
        private readonly Func<int, ReminderAudience, ReminderSendResult> _resultFactory;

        public FakeEmailSender(ReminderSendResult result)
            : this((_, _) => result)
        {
        }

        public FakeEmailSender(Func<int, ReminderAudience, ReminderSendResult> resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public List<(int EmployeeUserId, ReminderAudience Audience, ReminderEmail Message)> Messages { get; } = [];

        public Task<ReminderSendResult> SendAsync(
            int employeeUserId,
            ReminderAudience audience,
            ReminderEmail message,
            CancellationToken cancellationToken)
        {
            Messages.Add((employeeUserId, audience, message));
            return Task.FromResult(_resultFactory(employeeUserId, audience));
        }
    }

    private sealed class FakeSmsSender : ISmsReminderSender
    {
        private readonly Func<string, ReminderSendResult> _resultFactory;

        public FakeSmsSender(ReminderSendResult result)
            : this(_ => result)
        {
        }

        public FakeSmsSender(Func<string, ReminderSendResult> resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public List<(string ReceiverUsername, string SenderUsername, string Message)> Messages { get; } = [];

        public Task<ReminderSendResult> SendAsync(
            string receiverUsername,
            string senderUsername,
            string message,
            CancellationToken cancellationToken)
        {
            Messages.Add((receiverUsername, senderUsername, message));
            return Task.FromResult(_resultFactory(receiverUsername));
        }
    }

    private sealed class FakeRepository(IReadOnlyList<ApprovalReminderCandidate> candidates)
        : IApprovalReminderRepository
    {
        private long _nextId;

        public bool AllowClaims { get; set; } = true;
        public int QueryCount { get; private set; }
        public List<(int ReminderNumber, ReminderChannel Channel, ReminderAudience Audience)> Claims { get; } = [];
        public List<(long Id, ReminderDeliveryStatus Status, string? FailureCode)> Completions { get; } = [];

        public Task<IReadOnlyList<ApprovalReminderCandidate>> GetActionableApprovalsAsync(
            CancellationToken cancellationToken)
        {
            QueryCount++;
            return Task.FromResult(candidates);
        }

        public Task<long?> TryClaimAsync(
            ApprovalReminderCandidate candidate,
            int reminderNumber,
            ReminderChannel channel,
            ReminderAudience audience,
            int? recipientUserId,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            Claims.Add((reminderNumber, channel, audience));
            return Task.FromResult<long?>(AllowClaims ? ++_nextId : null);
        }

        public Task CompleteAsync(
            long deliveryId,
            ReminderDeliveryStatus status,
            string? failureCode,
            CancellationToken cancellationToken)
        {
            Completions.Add((deliveryId, status, failureCode));
            return Task.CompletedTask;
        }
    }
}
