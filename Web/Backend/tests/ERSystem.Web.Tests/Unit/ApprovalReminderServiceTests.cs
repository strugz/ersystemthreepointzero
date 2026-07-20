using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.ApprovalReminders;
using ERSystem.Web.Domain.ApprovalReminders;

namespace ERSystem.Web.Tests.Unit;

public sealed class ApprovalReminderServiceTests
{
    [Theory]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(9, 3)]
    public void Schedule_uses_three_day_calendar_boundaries(int elapsedDays, int expectedReminder)
    {
        var timeZone = TimeZoneInfo.CreateCustomTimeZone("Test Manila", TimeSpan.FromHours(8), "Test Manila", "Test Manila");
        var activeAt = new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc);
        var now = activeAt.AddDays(elapsedDays);

        var reminder = ApprovalReminderSchedule.GetDueReminderNumber(activeAt, now, timeZone, 3, 3);

        Assert.Equal(expectedReminder, reminder);
    }

    [Fact]
    public async Task Due_candidate_claims_and_processes_each_channel_independently()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var emailSender = new FakeEmailSender(ReminderSendResult.Success);
        var smsSender = new FakeSmsSender(ReminderSendResult.Success);
        var service = CreateService(repository, emailSender, smsSender, new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc));

        var summary = await service.RunAsync(CancellationToken.None);

        Assert.Equal(1, summary.DueCandidates);
        Assert.Equal(2, summary.Sent);
        Assert.Equal(1, summary.Queued);
        Assert.Equal(2, emailSender.Messages.Count);
        Assert.Single(smsSender.Messages);
        Assert.Equal(3, repository.Claims.Count);
        Assert.All(repository.Claims, claim => Assert.Equal(1, claim.ReminderNumber));
        Assert.Contains(repository.Completions, item => item.Status == ReminderDeliveryStatus.Queued);
    }

    [Fact]
    public async Task Missed_runs_claim_only_the_latest_due_occurrence()
    {
        var repository = new FakeRepository([CreateCandidate()]);
        var service = CreateService(
            repository,
            new FakeEmailSender(ReminderSendResult.Success),
            new FakeSmsSender(ReminderSendResult.Success),
            new DateTime(2026, 7, 26, 8, 0, 0, DateTimeKind.Utc));

        await service.RunAsync(CancellationToken.None);

        Assert.All(repository.Claims, claim => Assert.Equal(3, claim.ReminderNumber));
        Assert.DoesNotContain(repository.Claims, claim => claim.ReminderNumber is 1 or 2);
    }

    [Fact]
    public async Task Missing_email_is_skipped_without_blocking_sms()
    {
        var repository = new FakeRepository([CreateCandidate() with
        {
            ManagerNotificationEmail = null,
            EmployeeNotificationEmail = ""
        }]);
        var smsSender = new FakeSmsSender(ReminderSendResult.Success);
        var service = CreateService(
            repository,
            new FakeEmailSender(ReminderSendResult.Success),
            smsSender,
            new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc));

        var summary = await service.RunAsync(CancellationToken.None);

        Assert.Equal(2, summary.Skipped);
        Assert.Equal(1, summary.Queued);
        Assert.Single(smsSender.Messages);
    }

    [Fact]
    public async Task Existing_claims_do_not_send_duplicate_messages()
    {
        var repository = new FakeRepository([CreateCandidate()]) { AllowClaims = false };
        var emailSender = new FakeEmailSender(ReminderSendResult.Success);
        var smsSender = new FakeSmsSender(ReminderSendResult.Success);
        var service = CreateService(repository, emailSender, smsSender, new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc));

        var summary = await service.RunAsync(CancellationToken.None);

        Assert.Equal(3, summary.AlreadyClaimed);
        Assert.Empty(emailSender.Messages);
        Assert.Empty(smsSender.Messages);
    }

    private static ApprovalReminderService CreateService(
        FakeRepository repository,
        FakeEmailSender emailSender,
        FakeSmsSender smsSender,
        DateTime nowUtc)
    {
        var settings = new ApprovalReminderSettings(
            true,
            true,
            "UTC",
            new TimeOnly(8, 0),
            3,
            3,
            "https://er.example.test");
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
        "john@example.test",
        12,
        "MCRUZ",
        "Maria Cruz",
        "maria@example.test",
        "ERF-2026-00421",
        new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc));

    private sealed class FakeClock(DateTime nowUtc) : IClock
    {
        public DateTime UtcNow => nowUtc;
    }

    private sealed class FakeEmailSender(ReminderSendResult result) : IEmailReminderSender
    {
        public List<(string Recipient, ReminderEmail Message)> Messages { get; } = [];

        public Task<ReminderSendResult> SendAsync(
            string recipientAddress,
            ReminderEmail message,
            CancellationToken cancellationToken)
        {
            Messages.Add((recipientAddress, message));
            return Task.FromResult(result);
        }
    }

    private sealed class FakeSmsSender(ReminderSendResult result) : ISmsReminderSender
    {
        public List<string> Messages { get; } = [];

        public Task<ReminderSendResult> QueueAsync(
            ApprovalReminderCandidate candidate,
            string message,
            CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.FromResult(result);
        }
    }

    private sealed class FakeRepository(IReadOnlyList<ApprovalReminderCandidate> candidates)
        : IApprovalReminderRepository
    {
        private long _nextId;

        public bool AllowClaims { get; set; } = true;
        public List<(int ReminderNumber, ReminderChannel Channel, ReminderAudience Audience)> Claims { get; } = [];
        public List<(long Id, ReminderDeliveryStatus Status, string? FailureCode)> Completions { get; } = [];

        public Task<IReadOnlyList<ApprovalReminderCandidate>> GetActionableApprovalsAsync(
            CancellationToken cancellationToken) => Task.FromResult(candidates);

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
