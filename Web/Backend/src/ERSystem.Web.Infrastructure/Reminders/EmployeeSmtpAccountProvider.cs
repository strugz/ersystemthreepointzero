using System.Data;
using System.Security.Cryptography;
using ERSystem.Web.Application.Features.ApprovalReminders;
using ERSystem.Web.Infrastructure.Security;
using Microsoft.Data.SqlClient;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed record EncryptedEmployeeSmtpAccount(
    string? EncryptedEmailAddress,
    string? EncryptedPassword,
    string? ManagerEmailAddress);

public sealed record EmployeeSmtpAccount(
    string SenderAddress,
    string Password,
    string? ManagerEmailAddress);

public sealed record EmployeeSmtpAccountResolution(
    EmployeeSmtpAccount? Account,
    ReminderSendResult? Failure)
{
    public static EmployeeSmtpAccountResolution Found(EmployeeSmtpAccount account) =>
        new(account, null);

    public static EmployeeSmtpAccountResolution Unavailable(ReminderSendResult failure) =>
        new(null, failure);
}

public interface IEmployeeSmtpAccountStore
{
    Task<EncryptedEmployeeSmtpAccount?> FindAsync(
        int employeeUserId,
        CancellationToken cancellationToken);
}

public interface IEmployeeSmtpAccountProvider
{
    Task<EmployeeSmtpAccountResolution> ResolveAsync(
        int employeeUserId,
        CancellationToken cancellationToken);
}

public sealed class SqlEmployeeSmtpAccountStore(SqlConnectionStringBuilder connectionString)
    : IEmployeeSmtpAccountStore
{
    public async Task<EncryptedEmployeeSmtpAccount?> FindAsync(
        int employeeUserId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EmailAdd, EmailPass, EmailTo
            FROM dbo.tbUserRegistration
            WHERE UserID = @EmployeeUserID;
            """;

        await using var connection = new SqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@EmployeeUserID", SqlDbType.Int).Value = employeeUserId;
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SingleRow,
            cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new EncryptedEmployeeSmtpAccount(
            GetNullableString(reader, 0),
            GetNullableString(reader, 1),
            GetNullableString(reader, 2));
    }

    private static string? GetNullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).Trim();
}

public sealed class EmployeeSmtpAccountProvider(
    IEmployeeSmtpAccountStore store,
    LegacyPasswordCipher cipher) : IEmployeeSmtpAccountProvider
{
    private readonly Dictionary<int, EmployeeSmtpAccountResolution> _cache = [];

    public async Task<EmployeeSmtpAccountResolution> ResolveAsync(
        int employeeUserId,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(employeeUserId, out var cached))
        {
            return cached;
        }

        var encrypted = await store.FindAsync(employeeUserId, cancellationToken);
        var resolution = Resolve(encrypted);
        _cache.Add(employeeUserId, resolution);
        return resolution;
    }

    private EmployeeSmtpAccountResolution Resolve(EncryptedEmployeeSmtpAccount? encrypted)
    {
        if (encrypted is null)
        {
            return EmployeeSmtpAccountResolution.Unavailable(
                ReminderSendResult.Failed("SMTP_EMPLOYEE_ACCOUNT_NOT_FOUND"));
        }

        if (string.IsNullOrWhiteSpace(encrypted.EncryptedEmailAddress))
        {
            return EmployeeSmtpAccountResolution.Unavailable(
                ReminderSendResult.Skipped("SMTP_SENDER_ADDRESS_MISSING"));
        }

        if (string.IsNullOrWhiteSpace(encrypted.EncryptedPassword))
        {
            return EmployeeSmtpAccountResolution.Unavailable(
                ReminderSendResult.Skipped("SMTP_PASSWORD_MISSING"));
        }

        try
        {
            var senderAddress = cipher.Decrypt(encrypted.EncryptedEmailAddress);
            var password = cipher.Decrypt(encrypted.EncryptedPassword);
            if (string.IsNullOrWhiteSpace(senderAddress))
            {
                return EmployeeSmtpAccountResolution.Unavailable(
                    ReminderSendResult.Skipped("SMTP_SENDER_ADDRESS_MISSING"));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return EmployeeSmtpAccountResolution.Unavailable(
                    ReminderSendResult.Skipped("SMTP_PASSWORD_MISSING"));
            }

            return EmployeeSmtpAccountResolution.Found(
                new EmployeeSmtpAccount(
                    senderAddress,
                    password,
                    encrypted.ManagerEmailAddress));
        }
        catch (Exception exception) when (
            exception is FormatException or CryptographicException or ArgumentException)
        {
            return EmployeeSmtpAccountResolution.Unavailable(
                ReminderSendResult.Failed("LEGACY_EMAIL_DECRYPTION_FAILED"));
        }
    }
}
