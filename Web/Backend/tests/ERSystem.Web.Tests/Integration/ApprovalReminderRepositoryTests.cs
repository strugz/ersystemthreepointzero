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
