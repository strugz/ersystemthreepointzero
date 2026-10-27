using ERFWeb.Infrastructure.Notifications;
using Xunit;

namespace ERFWeb.Tests.Notifications;

public class ReminderOptionsTests
{
    [Theory]
    [InlineData("08:00", 8, 0)]
    [InlineData("17:30", 17, 30)]
    public void RunAt_AsHoursAndMinutes_IsRead(string value, int hour, int minute)
    {
        Assert.True(new ReminderOptions { RunAt = value }.TryGetRunAt(out var runAt));
        Assert.Equal(new TimeOnly(hour, minute), runAt);
    }

    [Theory]
    [InlineData("8:00")]
    [InlineData("8am")]
    [InlineData("")]
    public void RunAt_NotHoursAndMinutes_IsRefused(string value)
    {
        Assert.False(new ReminderOptions { RunAt = value }.TryGetRunAt(out _));
    }
}
