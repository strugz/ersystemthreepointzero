using ERSystem.Web.Infrastructure.Reminders;
using Microsoft.Data.SqlClient;

namespace ERSystem.Web.Tests.Integration;

public sealed class ApprovalReminderRepositoryTests
{
    [Fact]
    public async Task Actionable_query_executes_against_an_opted_in_compatibility_100_database()
    {
        var connectionString = Environment.GetEnvironmentVariable("ER_SYSTEM_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT CONVERT(int, compatibility_level) FROM sys.databases WHERE name = DB_NAME();",
            connection);
        var compatibilityLevel = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(100, compatibilityLevel);

        await using var activationConstraintCommand = new SqlCommand(
            """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM sys.check_constraints
                WHERE parent_object_id = OBJECT_ID(N'dbo.tbReportApprovalReminderDelivery')
                  AND name = N'CK_tbReportApprovalReminderDelivery_ReminderNumber'
                  AND is_disabled = 0
                  AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                          UPPER(definition), N' ', N''), N'(', N''), N')', N''), N'[', N''), N']', N'')
                      LIKE N'%REMINDERNUMBER>=0%'
            ) THEN 1 ELSE 0 END;
            """,
            connection);
        Assert.Equal(1, Convert.ToInt32(await activationConstraintCommand.ExecuteScalarAsync()));

        var repository = new ApprovalReminderRepository(builder);
        var candidates = await repository.GetActionableApprovalsAsync(CancellationToken.None);

        Assert.All(candidates, candidate =>
        {
            Assert.True(candidate.ApprovalTransactionId > 0);
            Assert.False(string.IsNullOrWhiteSpace(candidate.ReportId));
            Assert.False(string.IsNullOrWhiteSpace(candidate.ManagerUsername));
        });
    }
}
