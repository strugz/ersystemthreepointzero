using ERFWeb.Application.Common;
using ERFWeb.Application.Features.Administration;
using ERFWeb.Application.Features.Approvals;
using ERFWeb.Application.Features.Authentication;
using ERFWeb.Application.Features.Delegations;
using ERFWeb.Application.Features.Entitlements;
using ERFWeb.Application.Features.Expenses;
using ERFWeb.Application.Features.Lookups;
using ERFWeb.Application.Features.Notifications;
using ERFWeb.Application.Features.Reporting;
using ERFWeb.Application.Features.Verification;
using ERFWeb.Infrastructure.Administration;
using ERFWeb.Infrastructure.Approvals;
using ERFWeb.Infrastructure.Authentication;
using ERFWeb.Infrastructure.Authorization;
using ERFWeb.Infrastructure.Entitlements;
using ERFWeb.Infrastructure.Expenses;
using ERFWeb.Infrastructure.Lookups;
using ERFWeb.Infrastructure.Notifications;
using ERFWeb.Infrastructure.Persistence;
using ERFWeb.Infrastructure.Reporting;
using ERFWeb.Infrastructure.Services;
using ERFWeb.Infrastructure.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ERFWeb.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the EF Core context (Npgsql + snake_case) and infrastructure services.</summary>
    public static IServiceCollection AddErfWebInfrastructure(this IServiceCollection services, string connectionString)
    {
        // EnableRetryOnFailure makes the transaction runner replay a whole unit of work when
        // PostgreSQL reports a transient error — including serialization failures (40001) that two
        // concurrent serializable transactions produce when they both bump the same counter row.
        services.AddDbContext<ErfDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
                .UseSnakeCaseNamingConvention());

        // Cross-cutting
        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        services.AddScoped<IWorkflowAuditWriter, WorkflowAuditWriter>();
        services.AddScoped<IUserAuthenticator, UserAuthenticator>();
        // Logs every workflow event; filing also sends an SMS through Lasius (see SmsApprovalNotifier).
        services.AddScoped<LoggingApprovalNotifier>();
        services.AddScoped<IApprovalNotifier, SmsApprovalNotifier>();

        // Reporting (Phase 1)
        services.AddOptions<R47ReportOptions>();
        services.AddScoped<IExpenseReportDocumentLoader, ExpenseReportDocumentLoader>();
        services.AddSingleton<IExpenseReportPdfRenderer, R47PdfRenderer>();
        services.AddScoped<IReportAccessPolicy, ReportAccessPolicy>();

        // Authoring & filing (Phase 2)
        services.AddOptions<AttachmentOptions>();
        services.AddOptions<ReferenceNumberOptions>();
        services.AddScoped<IReferenceNumberGenerator, ReferenceNumberGenerator>();
        services.AddScoped<ReportEditGate>();
        services.AddScoped<ExpenseReportDetailLoader>();
        services.AddScoped<IExpenseReportService, ExpenseReportService>();
        services.AddScoped<IExpenseLineService, ExpenseLineService>();
        services.AddScoped<ICashAdvanceService, CashAdvanceService>();
        services.AddScoped<IReceiptAttachmentService, ReceiptAttachmentService>();
        services.AddScoped<IReportFilingService, ReportFilingService>();
        services.AddScoped<ILookupService, LookupService>();

        // Administration & accounts (Phase 1)
        services.AddScoped<UserDetailReader>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IRoleGrantService, RoleGrantService>();
        services.AddScoped<IMasterDataService, MasterDataService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IAdminAuditService, AdminAuditService>();

        // Scope & rates, Level 2 only (Phase 2)
        services.AddScoped<EntitlementContext>();
        services.AddScoped<IApprovalRouter, ApprovalRouter>();
        services.AddScoped<IScopeService, ScopeService>();
        services.AddScoped<IRateService, RateService>();
        services.AddScoped<IEntitlementChangeNotifier, LoggingEntitlementChangeNotifier>();

        // Two-level approval & Finance (Phase 5)
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IDelegationLookup, DelegationLookup>();
        services.AddScoped<IDelegationService, DelegationService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<VerificationReader>();
        services.AddScoped<IVerificationService, VerificationService>();

        // Notification SMS through the Lasius gateway; the URL and API key come from appsettings.json.
        services.AddOptions<LasiusOptions>();
        services.AddHttpClient<ISmsGateway, LasiusSmsGateway>((sp, client) =>
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, sp.GetRequiredService<IOptions<LasiusOptions>>().Value.TimeoutSeconds)));
        services.AddScoped<ISmsService, SmsService>();

        return services;
    }
}
