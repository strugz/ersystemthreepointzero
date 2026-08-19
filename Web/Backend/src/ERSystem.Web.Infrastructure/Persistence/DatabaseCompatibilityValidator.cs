using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERSystem.Web.Infrastructure.Persistence;

public sealed class DatabaseCompatibilityValidator(
    SqlConnectionStringBuilder connectionString,
    ILogger<DatabaseCompatibilityValidator> logger) : IHostedService
{
    private const int MinimumSqlServerMajorVersion = 10;
    private const int RequiredCompatibilityLevel = 100;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')),
                   CONVERT(nvarchar(128), SERVERPROPERTY('Edition')),
                   CONVERT(int, compatibility_level),
                   CASE WHEN OBJECT_ID(N'dbo.tbWebLoginSecurity', N'U') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN OBJECT_ID(N'dbo.tbWebWorkflowAudit', N'U') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN OBJECT_ID(N'dbo.tbReportApprovalTransaction', N'U') IS NULL THEN 0 ELSE 1 END,
                   CASE WHEN OBJECT_ID(N'dbo.sp2_RefileER', N'P') IS NULL THEN 0 ELSE 1 END
            FROM sys.databases
            WHERE name = DB_NAME();
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Unable to read SQL Server compatibility information.");

        var productVersion = reader.IsDBNull(0) ? null : reader.GetString(0);
        if (!Version.TryParse(productVersion, out var parsedVersion))
            throw new InvalidOperationException(
                "Unable to determine the SQL Server version. ER System Web requires SQL Server 2008 (10.x) or later.");

        var majorVersion = parsedVersion.Major;
        var edition = reader.IsDBNull(1) ? "Unknown edition" : reader.GetString(1);
        if (reader.IsDBNull(2))
            throw new InvalidOperationException("Unable to determine the database compatibility level.");

        var compatibilityLevel = reader.GetInt32(2);
        if (majorVersion < MinimumSqlServerMajorVersion)
            throw new InvalidOperationException(
                $"ER System Web requires SQL Server 2008 (10.x) or later. Connected server version: {productVersion}.");
        if (compatibilityLevel != RequiredCompatibilityLevel)
            throw new InvalidOperationException(
                $"ER System Web v1 requires database compatibility level {RequiredCompatibilityLevel}.");
        if (reader.GetInt32(3) == 0 || reader.GetInt32(4) == 0 || reader.GetInt32(5) == 0 || reader.GetInt32(6) == 0)
            throw new InvalidOperationException(
                "ER System Web database objects are missing. Run the required scripts in Database date order before starting the API.");

        if (majorVersion < 15)
            logger.LogWarning(
                "Running ER System Web in legacy SQL Server compatibility mode on version {ProductVersion}",
                productVersion);

        logger.LogInformation(
            "Validated SQL Server {MajorVersion} ({Edition}) at compatibility level {CompatibilityLevel}",
            majorVersion, edition, compatibilityLevel);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
