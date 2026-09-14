#:project ../src/ERFWeb.Infrastructure/ERFWeb.Infrastructure.csproj
#:property PublishAot=false
#:property EnableAotAnalyzer=false

// Emits the PostgreSQL DDL for the current EF Core model (plus the row-history triggers)
// without needing dotnet-ef or a live database. Usage (from ERFWeb/Backend):
//   dotnet run tools/GenerateSchemaScript.cs > ../Database/0001_initial_schema.sql
using ERFWeb.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<ErfDbContext>()
    .UseNpgsql("Host=localhost;Database=erfweb;Username=postgres;Password=postgres")
    .UseSnakeCaseNamingConvention()
    .Options;

using var db = new ErfDbContext(options);

Console.WriteLine("-- ERFWeb initial schema (PostgreSQL). Generated from the EF Core model by tools/GenerateSchemaScript.cs.");
Console.WriteLine($"-- Generated: {DateTimeOffset.UtcNow:u}");
Console.WriteLine("-- Apply once to an empty database. Enums are stored as text; concurrency uses the xmin system column.");
Console.WriteLine();
Console.WriteLine("BEGIN;");
Console.WriteLine();
Console.WriteLine(db.Database.GenerateCreateScript().TrimEnd());
Console.WriteLine();
Console.WriteLine("-- Row-history trigger function and triggers (high-churn tables only)");
Console.WriteLine(HistorySql.CreateFunction.TrimEnd());
Console.WriteLine();
Console.WriteLine(HistorySql.CreateTriggers.TrimEnd());
Console.WriteLine();
Console.WriteLine("COMMIT;");
