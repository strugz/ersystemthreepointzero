using ERSystem.Web.Application.Common;
using ERSystem.Web.Application.Features.Authentication;
using ERSystem.Web.Application.Features.FinanceReceipts;
using ERSystem.Web.Application.Features.ManagerApprovals;
using ERSystem.Web.Infrastructure.Authentication;
using ERSystem.Web.Infrastructure.Persistence;
using ERSystem.Web.Infrastructure.Security;
using ERSystem.Web.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERSystem.Web.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ErDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:ErDatabase must be configured.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:ErDatabase must not be empty.");
        void ConfigureSql(DbContextOptionsBuilder options) => options.UseSqlServer(connectionString, sql =>
        {
            sql.UseCompatibilityLevel(100);
        });

        services.AddOptions<LegacyAuthenticationOptions>()
            .Bind(configuration.GetSection(LegacyAuthenticationOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.EncryptionKey),
                "LegacyAuthentication:EncryptionKey must be configured.")
            .ValidateOnStart();
        services.AddDbContextFactory<LegacyErDbContext>(ConfigureSql);
        services.AddDbContextFactory<WebWorkflowDbContext>(ConfigureSql);
        services.AddSingleton(new SqlConnectionStringBuilder(connectionString));
        services.AddHostedService<DatabaseCompatibilityValidator>();
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IRowVersionCodec, RowVersionCodec>();
        services.AddScoped<IWorkflowAuditWriter, WorkflowAuditWriter>();
        services.AddScoped<IReportAuthorizationService, ReportAuthorizationService>();
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        services.AddSingleton<LegacyPasswordCipher>();
        services.AddScoped<IAuthenticationService, LegacyAuthenticationService>();
        services.AddScoped<IManagerApprovalService, ManagerApprovalService>();
        services.AddScoped<IFinanceReceiptService, FinanceReceiptService>();
        return services;
    }
}
