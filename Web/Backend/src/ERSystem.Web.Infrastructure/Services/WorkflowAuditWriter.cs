using System.Data;
using System.Data.Common;
using ERSystem.Web.Application.Common;

namespace ERSystem.Web.Infrastructure.Services;

public sealed class WorkflowAuditWriter : IWorkflowAuditWriter
{
    public async Task WriteAsync(
        DbConnection connection, DbTransaction transaction, WorkflowAuditEntry entry, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.tbWebWorkflowAudit
                (ReportID, ActorUserID, EventType, PreviousState, NewState, Remarks, OccurredAtUtc, CorrelationID)
            VALUES
                (@ReportID, @ActorUserID, @EventType, @PreviousState, @NewState, @Remarks, @OccurredAtUtc, @CorrelationID);
            """;
        Add(command, "@ReportID", DbType.AnsiString, entry.ReportId, 50);
        Add(command, "@ActorUserID", DbType.Int32, entry.ActorUserId);
        Add(command, "@EventType", DbType.AnsiString, entry.EventType, 40);
        Add(command, "@PreviousState", DbType.AnsiString, entry.PreviousState, 100);
        Add(command, "@NewState", DbType.AnsiString, entry.NewState, 100);
        Add(command, "@Remarks", DbType.String, entry.Remarks, 1000);
        Add(command, "@OccurredAtUtc", DbType.DateTime2, entry.OccurredAtUtc);
        Add(command, "@CorrelationID", DbType.Guid, entry.CorrelationId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Add(DbCommand command, string name, DbType type, object? value, int? size = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        if (size.HasValue) parameter.Size = size.Value;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
