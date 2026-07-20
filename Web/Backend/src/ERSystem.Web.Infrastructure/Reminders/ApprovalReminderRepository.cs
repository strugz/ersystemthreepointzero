using System.Data;
using ERSystem.Web.Application.Features.ApprovalReminders;
using Microsoft.Data.SqlClient;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class ApprovalReminderRepository(SqlConnectionStringBuilder connectionString)
    : IApprovalReminderRepository
{
    public async Task<IReadOnlyList<ApprovalReminderCandidate>> GetActionableApprovalsAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT currentStep.ID,
                   currentStep.ReportID,
                   currentStep.ApprovalCycle,
                   currentStep.EmployeeUserID,
                   LTRIM(RTRIM(ISNULL(employee.username, ''))),
                   LTRIM(RTRIM(ISNULL(employee.Fullname, employee.username))),
                   employee.NotificationEmail,
                   currentStep.ApproverUserID,
                   LTRIM(RTRIM(ISNULL(manager.username, ''))),
                   LTRIM(RTRIM(ISNULL(manager.Fullname, manager.username))),
                   manager.NotificationEmail,
                   report.ERFReferenceNo,
                   ISNULL(previousStep.ActionedAtUtc, currentStep.SubmittedAtUtc)
            FROM dbo.tbReportApprovalTransaction currentStep
            INNER JOIN dbo.tbReportDetails report ON report.ID = currentStep.ReportID
            INNER JOIN dbo.tbUserRegistration employee ON employee.UserID = currentStep.EmployeeUserID
            INNER JOIN dbo.tbUserRegistration manager ON manager.UserID = currentStep.ApproverUserID
            OUTER APPLY
            (
                SELECT TOP 1 earlier.ActionedAtUtc
                FROM dbo.tbReportApprovalTransaction earlier
                WHERE earlier.ReportID = currentStep.ReportID
                  AND earlier.ApprovalCycle = currentStep.ApprovalCycle
                  AND earlier.StepOrder < currentStep.StepOrder
                  AND earlier.Status = 'Approved'
                ORDER BY earlier.StepOrder DESC
            ) previousStep
            WHERE currentStep.Status = 'Pending'
              AND report.ReportFileStatus = '1'
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.tbReportApprovalTransaction earlierStep
                  WHERE earlierStep.ReportID = currentStep.ReportID
                    AND earlierStep.ApprovalCycle = currentStep.ApprovalCycle
                    AND earlierStep.StepOrder < currentStep.StepOrder
                    AND earlierStep.Status <> 'Approved'
              )
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.tbReportApprovalTransaction laterCycle
                  WHERE laterCycle.ReportID = currentStep.ReportID
                    AND laterCycle.ApprovalCycle > currentStep.ApprovalCycle
              )
            ORDER BY currentStep.ID;
            """;

        var candidates = new List<ApprovalReminderCandidate>();
        await using var connection = new SqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            candidates.Add(new ApprovalReminderCandidate(
                reader.GetInt64(0),
                reader.GetString(1).Trim(),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetString(4),
                reader.GetString(5),
                GetNullableString(reader, 6),
                reader.GetInt32(7),
                reader.GetString(8),
                reader.GetString(9),
                GetNullableString(reader, 10),
                GetNullableString(reader, 11),
                DateTime.SpecifyKind(reader.GetDateTime(12), DateTimeKind.Utc)));
        }

        return candidates;
    }

    public async Task<long?> TryClaimAsync(
        ApprovalReminderCandidate candidate,
        int reminderNumber,
        ReminderChannel channel,
        ReminderAudience audience,
        int? recipientUserId,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            IF EXISTS
            (
                SELECT 1
                FROM dbo.tbReportApprovalTransaction currentStep WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN dbo.tbReportDetails report WITH (UPDLOCK, HOLDLOCK)
                    ON report.ID = currentStep.ReportID
                WHERE currentStep.ID = @ApprovalTransactionID
                  AND currentStep.ReportID = @ReportID
                  AND currentStep.ApprovalCycle = @ApprovalCycle
                  AND currentStep.EmployeeUserID = @EmployeeUserID
                  AND currentStep.ApproverUserID = @ApproverUserID
                  AND currentStep.Status = 'Pending'
                  AND report.ReportFileStatus = '1'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.tbReportApprovalTransaction earlierStep
                      WHERE earlierStep.ReportID = currentStep.ReportID
                        AND earlierStep.ApprovalCycle = currentStep.ApprovalCycle
                        AND earlierStep.StepOrder < currentStep.StepOrder
                        AND earlierStep.Status <> 'Approved'
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.tbReportApprovalTransaction laterCycle
                      WHERE laterCycle.ReportID = currentStep.ReportID
                        AND laterCycle.ApprovalCycle > currentStep.ApprovalCycle
                  )
            )
            AND NOT EXISTS
            (
                SELECT 1
                FROM dbo.tbReportApprovalReminderDelivery WITH (UPDLOCK, HOLDLOCK)
                WHERE ApprovalTransactionID = @ApprovalTransactionID
                  AND ReminderNumber = @ReminderNumber
                  AND Channel = @Channel
                  AND Audience = @Audience
            )
            BEGIN
                INSERT INTO dbo.tbReportApprovalReminderDelivery
                (
                    ApprovalTransactionID, ReportID, ApprovalCycle, ReminderNumber,
                    Channel, Audience, RecipientUserID, DeliveryStatus, FailureCode,
                    CorrelationID, CreatedAtUtc, AttemptedAtUtc
                )
                VALUES
                (
                    @ApprovalTransactionID, @ReportID, @ApprovalCycle, @ReminderNumber,
                    @Channel, @Audience, @RecipientUserID, 'Attempting', NULL,
                    @CorrelationID, GETUTCDATE(), GETUTCDATE()
                );

                SELECT CONVERT(bigint, SCOPE_IDENTITY());
            END;
            """;

        await using var connection = new SqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await using var command = new SqlCommand(sql, connection, transaction);
        AddClaimParameters(command, candidate, reminderNumber, channel, audience, recipientUserId, correlationId);

        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result is null or DBNull ? null : Convert.ToInt64(result);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return null;
        }
    }

    public async Task CompleteAsync(
        long deliveryId,
        ReminderDeliveryStatus status,
        string? failureCode,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.tbReportApprovalReminderDelivery
            SET DeliveryStatus = @DeliveryStatus,
                FailureCode = @FailureCode,
                CompletedAtUtc = GETUTCDATE()
            WHERE ID = @DeliveryID
              AND DeliveryStatus = 'Attempting';
            """;

        await using var connection = new SqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@DeliveryID", SqlDbType.BigInt).Value = deliveryId;
        command.Parameters.Add("@DeliveryStatus", SqlDbType.VarChar, 20).Value = status.ToString();
        command.Parameters.Add("@FailureCode", SqlDbType.VarChar, 100).Value =
            string.IsNullOrWhiteSpace(failureCode) ? DBNull.Value : failureCode;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddClaimParameters(
        SqlCommand command,
        ApprovalReminderCandidate candidate,
        int reminderNumber,
        ReminderChannel channel,
        ReminderAudience audience,
        int? recipientUserId,
        Guid correlationId)
    {
        command.Parameters.Add("@ApprovalTransactionID", SqlDbType.BigInt).Value = candidate.ApprovalTransactionId;
        command.Parameters.Add("@ReportID", SqlDbType.VarChar, 50).Value = candidate.ReportId;
        command.Parameters.Add("@ApprovalCycle", SqlDbType.Int).Value = candidate.ApprovalCycle;
        command.Parameters.Add("@EmployeeUserID", SqlDbType.Int).Value = candidate.EmployeeUserId;
        command.Parameters.Add("@ApproverUserID", SqlDbType.Int).Value = candidate.ManagerUserId;
        command.Parameters.Add("@ReminderNumber", SqlDbType.Int).Value = reminderNumber;
        command.Parameters.Add("@Channel", SqlDbType.VarChar, 20).Value = channel.ToString();
        command.Parameters.Add("@Audience", SqlDbType.VarChar, 30).Value = audience.ToString();
        command.Parameters.Add("@RecipientUserID", SqlDbType.Int).Value = recipientUserId.HasValue
            ? recipientUserId.Value
            : DBNull.Value;
        command.Parameters.Add("@CorrelationID", SqlDbType.UniqueIdentifier).Value = correlationId;
    }

    private static string? GetNullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).Trim();
}
