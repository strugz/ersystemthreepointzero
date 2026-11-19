using ERFWeb.Application.Features.Notifications;
using ERFWeb.Infrastructure.Testing;
using Xunit;

namespace ERFWeb.Tests.Notifications;

public class TestModeRecipientsTests
{
    [Fact]
    public void Sms_GoesToTheTestNumberOnly_MarkedTest()
    {
        var sent = TestModeRecipients.Redirect(new SmsGatewayMessage("ERFWeb", ["+639170000001", "+639170000002"], "Report filed."), "+639170000009");

        Assert.Equal(["+639170000009"], sent.Recipients);
        Assert.Equal("[TEST] Report filed.", sent.Message);
        Assert.Equal("ERFWeb", sent.Sender);
    }

    [Fact]
    public void Email_GoesToTheTestAddressOnly_SayingHowManyItWasMeantFor()
    {
        var sent = TestModeRecipients.Redirect(
            new EmailMessage(["a@example.com", "b@example.com"], "R47 filed", "Body.", []), "test@example.com");

        Assert.Equal(["test@example.com"], sent.To);
        Assert.Equal("[TEST] R47 filed", sent.Subject);
        Assert.StartsWith("Test mode: this email was meant for 2 recipient(s).", sent.Body);
        Assert.EndsWith("Body.", sent.Body);
    }

    [Fact]
    public void NotTestMode_IsNotTest()
    {
        Assert.False(new AppModeOptions().IsTest);
        Assert.True(new AppModeOptions { Mode = "test" }.IsTest);
    }
}
