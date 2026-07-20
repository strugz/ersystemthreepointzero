using System.Data;
using ERSystem.Web.Application.Features.ApprovalReminders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ERSystem.Web.Infrastructure.Reminders;

public sealed class StoredProcedureSmsReminderSender(
    SqlConnectionStringBuilder connectionString,
    ILogger<StoredProcedureSmsReminderSender> logger)
    : ISmsReminderSender
{
    public async Task<ReminderSendResult> QueueAsync(
        ApprovalReminderCandidate candidate,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand("dbo.sp_Notify", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add("@ReportID", SqlDbType.VarChar, 100).Value = candidate.ReportId;
            command.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = "REMINDER";
            command.Parameters.Add("@ReminderApproverUsername", SqlDbType.VarChar, 50).Value = candidate.ManagerUsername;
            command.Parameters.Add("@ReminderMessage", SqlDbType.VarChar, 500).Value = message;
            await command.ExecuteNonQueryAsync(cancellationToken);
            return ReminderSendResult.Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SqlException)
        {
            logger.LogWarning("Approval reminder SMS gateway request failed with code SMS_QUEUE_SQL_FAILED");
            return ReminderSendResult.Failed("SMS_QUEUE_SQL_FAILED");
        }
    }
}
