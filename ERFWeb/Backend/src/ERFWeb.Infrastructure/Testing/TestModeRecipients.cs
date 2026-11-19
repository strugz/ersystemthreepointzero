using ERFWeb.Application.Features.Notifications;
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure.Testing;

/// <summary>
/// On a test install (<c>App:Mode</c> Test) every SMS and email goes to one account instead of its real
/// recipients: the user named by <c>App:TestRecipient</c>. With no such active user (or no mobile number or email
/// on it) nothing is sent, so a test server never texts or emails real people.
/// </summary>
public sealed class TestModeRecipients(ErfDbContext db, IOptions<AppModeOptions> options)
{
    public const string Prefix = "[TEST] ";

    public bool IsTest => options.Value.IsTest;

    /// <summary>The test account's mobile number and email, or nulls when it is not set up.</summary>
    public async Task<(string? MobileNo, string? Email)> ContactAsync(CancellationToken ct)
    {
        var username = options.Value.TestRecipient?.Trim();
        if (string.IsNullOrEmpty(username))
        {
            return (null, null);
        }

        var contact = await db.Users.AsNoTracking()
            .Where(u => u.Username == username && u.IsActive)
            .Select(u => new { u.MobileNo, u.Email })
            .FirstOrDefaultAsync(ct);
        return (Blank(contact?.MobileNo), Blank(contact?.Email));
    }

    /// <summary>The SMS addressed to <paramref name="mobileNo"/> only, marked as a test.</summary>
    public static SmsGatewayMessage Redirect(SmsGatewayMessage message, string mobileNo) =>
        message with { Recipients = [mobileNo], Message = Prefix + message.Message };

    /// <summary>The email addressed to <paramref name="email"/> only, marked as a test and saying how many it was meant for.</summary>
    public static EmailMessage Redirect(EmailMessage message, string email) =>
        message with
        {
            To = [email],
            Subject = Prefix + message.Subject,
            Body = $"Test mode: this email was meant for {message.To.Count} recipient(s).\n\n{message.Body}",
        };

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Sends every SMS to the test account on a test install; passes through otherwise.</summary>
public sealed class TestModeSmsGateway(LasiusSmsGateway inner, TestModeRecipients test) : ISmsGateway
{
    public bool IsConfigured => inner.IsConfigured;

    public async Task<SmsGatewayResult> SendAsync(SmsGatewayMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!test.IsTest)
        {
            return await inner.SendAsync(message, ct);
        }

        var (mobileNo, _) = await test.ContactAsync(ct);
        return mobileNo is null
            ? new SmsGatewayResult(SmsGatewayStatus.Rejected)
            : await inner.SendAsync(TestModeRecipients.Redirect(message, mobileNo), ct);
    }
}

/// <summary>Sends every email to the test account on a test install; passes through otherwise.</summary>
public sealed class TestModeEmailGateway(MailKitEmailGateway inner, TestModeRecipients test) : IEmailGateway
{
    public bool IsConfigured => inner.IsConfigured;

    public async Task<EmailGatewayResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!test.IsTest)
        {
            return await inner.SendAsync(message, ct);
        }

        var (_, email) = await test.ContactAsync(ct);
        return email is null
            ? new EmailGatewayResult(EmailGatewayStatus.Rejected)
            : await inner.SendAsync(TestModeRecipients.Redirect(message, email), ct);
    }
}
