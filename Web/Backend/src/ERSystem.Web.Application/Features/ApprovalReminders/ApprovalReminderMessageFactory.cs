namespace ERSystem.Web.Application.Features.ApprovalReminders;

public sealed class ApprovalReminderMessageFactory(ApprovalReminderSettings settings)
{
    private const int SmsMaximumLength = 320;

    public ApprovalActivationMessages CreateActivation(ApprovalReminderCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var reference = GetReference(candidate);
        var employeeFullName = SanitizeInline(candidate.EmployeeFullName);
        var managerFullName = SanitizeInline(candidate.ManagerFullName);

        var managerEmail = new ReminderEmail(
            $"[ER System] Approval required - ERF {reference}",
            $"Hello {managerFullName},{Environment.NewLine}{Environment.NewLine}" +
            $"ERF {reference}, filed by {employeeFullName}, is now awaiting your approval." +
            $"{Environment.NewLine}{Environment.NewLine}" +
            $"Please review the report in ER System.{Environment.NewLine}{Environment.NewLine}" +
            "ER System");

        var employeeEmail = new ReminderEmail(
            $"[ER System] Your ERF {reference} was filed",
            $"Hello {employeeFullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your ERF {reference} was filed successfully and is now awaiting approval from " +
            $"{managerFullName}.{Environment.NewLine}{Environment.NewLine}" +
            "ER System");

        return new ApprovalActivationMessages(managerEmail, employeeEmail);
    }

    public ApprovalReminderMessages CreateReminder(
        ApprovalReminderCandidate candidate,
        int elapsedCalendarDays)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedCalendarDays);

        var reference = GetReference(candidate);
        var employeeUsername = SanitizeInline(candidate.EmployeeUsername);
        var managerUsername = SanitizeInline(candidate.ManagerUsername);
        var employeeFullName = SanitizeInline(candidate.EmployeeFullName);
        var managerFullName = SanitizeInline(candidate.ManagerFullName);

        var sms = SanitizeSms(
            $"Reminder: ERF {reference} for {employeeUsername} is still awaiting approval from {managerUsername} " +
            $"after {elapsedCalendarDays} days. Please review or follow up.");

        var managerEmail = new ReminderEmail(
            $"[ER System] Approval reminder - ERF {reference}",
            $"Hello {managerFullName},{Environment.NewLine}{Environment.NewLine}" +
            $"ERF {reference}, filed by {employeeFullName}, has been waiting for your approval for " +
            $"{elapsedCalendarDays} calendar days.{Environment.NewLine}{Environment.NewLine}" +
            $"Please review the report and either approve or return it in ER System." +
            $"{Environment.NewLine}{Environment.NewLine}" +
            $"This reminder will repeat every {settings.ReminderDayOfWeek} until the report is actioned." +
            $"{Environment.NewLine}{Environment.NewLine}" +
            "ER System");

        var employeeEmail = new ReminderEmail(
            $"[ER System] Your ERF {reference} is awaiting approval",
            $"Hello {employeeFullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your ERF {reference} has been waiting for approval from {managerFullName} for " +
            $"{elapsedCalendarDays} calendar days.{Environment.NewLine}{Environment.NewLine}" +
            "A reminder was also sent to the manager. You may follow up with the manager if needed." +
            $"{Environment.NewLine}{Environment.NewLine}ER System");

        return new ApprovalReminderMessages(sms, managerEmail, employeeEmail);
    }

    public static string SanitizeSms(string value)
    {
        var sanitized = SanitizeInline(value);
        return sanitized.Length <= SmsMaximumLength ? sanitized : sanitized[..SmsMaximumLength];
    }

    private static string GetReference(ApprovalReminderCandidate candidate) =>
        SanitizeInline(string.IsNullOrWhiteSpace(candidate.ErfReferenceNumber)
            ? candidate.ReportId
            : candidate.ErfReferenceNumber);

    private static string SanitizeInline(string? value) =>
        (value ?? string.Empty)
            .Replace('|', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
}
