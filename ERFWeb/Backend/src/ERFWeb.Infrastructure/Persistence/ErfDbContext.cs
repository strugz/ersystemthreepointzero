using ERFWeb.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERFWeb.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context for ERFWeb. Owns the PostgreSQL schema (via migrations).
/// snake_case naming is applied by <c>UseSnakeCaseNamingConvention()</c> in DI.
/// </summary>
public class ErfDbContext(DbContextOptions<ErfDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<CostCenterBudget> CostCenterBudgets => Set<CostCenterBudget>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<FareMode> FareModes => Set<FareMode>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRoleGrant> UserRoleGrants => Set<UserRoleGrant>();
    public DbSet<ApprovalRoute> ApprovalRoutes => Set<ApprovalRoute>();
    public DbSet<DepartmentSignatory> DepartmentSignatories => Set<DepartmentSignatory>();
    public DbSet<EmployeeRate> EmployeeRates => Set<EmployeeRate>();
    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();
    public DbSet<CashAdvanceLine> CashAdvanceLines => Set<CashAdvanceLine>();
    public DbSet<ExpenseLine> ExpenseLines => Set<ExpenseLine>();
    public DbSet<ExpenseMealItem> ExpenseMealItems => Set<ExpenseMealItem>();
    public DbSet<ExpenseTransportItem> ExpenseTransportItems => Set<ExpenseTransportItem>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();
    public DbSet<FinanceTracking> FinanceTrackings => Set<FinanceTracking>();
    public DbSet<ReceiptAttachment> ReceiptAttachments => Set<ReceiptAttachment>();
    public DbSet<VerificationRequest> VerificationRequests => Set<VerificationRequest>();
    public DbSet<ReminderDelivery> ReminderDeliveries => Set<ReminderDelivery>();
    public DbSet<SmsDelivery> SmsDeliveries => Set<SmsDelivery>();
    public DbSet<EmailDelivery> EmailDeliveries => Set<EmailDelivery>();
    public DbSet<LoginSecurity> LoginSecurities => Set<LoginSecurity>();
    public DbSet<WorkflowAudit> WorkflowAudits => Set<WorkflowAudit>();
    public DbSet<RecordHistory> RecordHistories => Set<RecordHistory>();
    public DbSet<ReferenceSeries> ReferenceSeries => Set<ReferenceSeries>();
    public DbSet<ScopeAssignment> ScopeAssignments => Set<ScopeAssignment>();
    public DbSet<ScopeAssignmentMember> ScopeAssignmentMembers => Set<ScopeAssignmentMember>();
    public DbSet<RateProfile> RateProfiles => Set<RateProfile>();
    public DbSet<RateProfileLine> RateProfileLines => Set<RateProfileLine>();
    public DbSet<RateProfileAssignment> RateProfileAssignments => Set<RateProfileAssignment>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Store enums as readable text (e.g. 'ForApproval') rather than native PG enums.
        builder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<RoleGrantStatus>().HaveConversion<string>().HaveMaxLength(24);
        builder.Properties<ScopeAssignmentType>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<RateType>().HaveConversion<string>().HaveMaxLength(24);
        builder.Properties<RateTargetType>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<ReportType>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<ReportStatus>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<ApprovalStatus>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<FinanceStatus>().HaveConversion<string>().HaveMaxLength(24);
        builder.Properties<VerificationStatus>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<VerificationFinding>().HaveConversion<string>().HaveMaxLength(24);
        builder.Properties<QueryDisposition>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<ExpenseType>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<ExpenseCategory>().HaveConversion<string>().HaveMaxLength(24);

        // Money defaults to numeric(14,2) across the model.
        builder.Properties<decimal>().HavePrecision(14, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Department>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            AddConcurrencyToken(e);
        });

        b.Entity<CostCenter>(e =>
        {
            e.ToTable("cost_centers");
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId);
            AddConcurrencyToken(e);
        });

        b.Entity<CostCenterBudget>(e =>
        {
            e.ToTable("cost_center_budgets");
            e.HasKey(x => new { x.CostCenterId, x.BudgetYear });
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne<CostCenter>().WithMany().HasForeignKey(x => x.CostCenterId);
        });

        b.Entity<Currency>(e =>
        {
            e.ToTable("currencies");
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(3).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            AddConcurrencyToken(e);
        });

        b.Entity<Site>(e =>
        {
            e.ToTable("sites");
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            AddConcurrencyToken(e);
        });

        b.Entity<FareMode>(e =>
        {
            e.ToTable("fare_modes");
            e.Property(x => x.Code).HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(60);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            AddConcurrencyToken(e);
        });

        b.Entity<AppSetting>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(64).ValueGeneratedNever();
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.MustChangePassword).HasDefaultValue(false);
            e.Property(x => x.JobGrade).HasMaxLength(32);
            e.Property(x => x.CrmEmployeeId).HasMaxLength(50);
            e.HasIndex(x => x.CrmEmployeeId).IsUnique();
            e.Property(x => x.R47ReviewedBy).HasColumnName("r47_reviewed_by").HasMaxLength(60);
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId);
            e.HasOne<CostCenter>().WithMany().HasForeignKey(x => x.CostCenterId);
            AddConcurrencyToken(e);
        });

        b.Entity<UserRoleGrant>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => new { x.UserId, x.Role });
            e.Property(x => x.GrantedAt).HasDefaultValueSql("now()");
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.Role, x.Status });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CountersignedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<ApprovalRoute>(e =>
        {
            e.HasIndex(x => new { x.EmployeeUserId, x.StepOrder }).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.EmployeeUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DepartmentSignatory>(e =>
        {
            e.HasKey(x => x.DepartmentId);
            e.Property(x => x.DepartmentId).ValueGeneratedNever();
            e.HasOne<Department>().WithOne().HasForeignKey<DepartmentSignatory>(x => x.DepartmentId);
        });

        b.Entity<EmployeeRate>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).ValueGeneratedNever();
            e.HasOne<User>().WithOne().HasForeignKey<EmployeeRate>(x => x.UserId);
        });

        b.Entity<ExpenseReport>(e =>
        {
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Status).HasDefaultValue(ReportStatus.New);
            e.Property(x => x.CurrentCycle).HasDefaultValue(1);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => x.ReferenceNo).IsUnique();
            e.HasIndex(x => x.EmployeeUserId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DepartmentId);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.EmployeeUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<CashAdvanceLine>(e =>
        {
            e.ToTable("cash_advance_lines");
            e.HasIndex(x => new { x.ReportId, x.SortOrder });
            e.Property(x => x.ReferenceDocument).HasMaxLength(100);
            e.Property(x => x.ReferenceNo).HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            AddConcurrencyToken(e);
        });

        b.Entity<ExpenseLine>(e =>
        {
            e.HasIndex(x => new { x.ReportId, x.SortOrder });
            e.Property(x => x.CrmActivityIds).HasColumnType("varchar(50)[]");
            e.HasIndex(x => x.CrmActivityIds).HasMethod("gin");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.Multiplier).HasDefaultValue(1);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            AddConcurrencyToken(e);
        });

        b.Entity<ExpenseMealItem>(e =>
            e.HasOne<ExpenseLine>().WithMany().HasForeignKey(x => x.ExpenseLineId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<ExpenseTransportItem>(e =>
        {
            e.HasOne<ExpenseLine>().WithMany().HasForeignKey(x => x.ExpenseLineId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<FareMode>().WithMany().HasForeignKey(x => x.FareModeId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.FareModeDetail).HasMaxLength(60);
        });

        b.Entity<ApprovalStep>(e =>
        {
            e.HasIndex(x => new { x.ReportId, x.Cycle, x.StepOrder }).IsUnique();
            e.HasIndex(x => new { x.ReportId, x.Status });
            e.Property(x => x.Cycle).HasDefaultValue(1);
            e.Property(x => x.Status).HasDefaultValue(ApprovalStatus.Pending);
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ActedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApprovalDelegation>().WithMany().HasForeignKey(x => x.DelegationId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<ApprovalDelegation>(e =>
        {
            e.ToTable("approval_delegations", t =>
            {
                t.HasCheckConstraint("ck_approval_delegations_dates", "ends_on >= starts_on");
                t.HasCheckConstraint("ck_approval_delegations_not_self", "delegate_user_id <> signatory_user_id");
            });
            e.HasIndex(x => x.SignatoryUserId);
            e.HasIndex(x => x.DelegateUserId);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.SignatoryUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.DelegateUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<FinanceTracking>(e =>
        {
            e.ToTable("finance_tracking");
            e.HasKey(x => x.ReportId);
            e.Property(x => x.ReportId).ValueGeneratedNever();
            e.Property(x => x.Status).HasDefaultValue(FinanceStatus.Pending);
            e.HasOne<ExpenseReport>().WithOne().HasForeignKey<FinanceTracking>(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ClearedByUserId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<VerificationRequest>(e =>
        {
            e.ToTable("verification_requests");
            e.HasIndex(x => x.ReportId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.VerifierUserId);
            e.HasIndex(x => x.QueryToUserId);

            // One unresolved request per line; a closed one may be followed by a new request.
            e.HasIndex(x => x.ExpenseLineId).IsUnique().HasFilter("status <> 'Closed'");
            e.Property(x => x.Status).HasDefaultValue(VerificationStatus.Open);
            e.Property(x => x.Reason).HasMaxLength(2000);
            e.Property(x => x.FindingNote).HasMaxLength(2000);
            e.Property(x => x.QueryNote).HasMaxLength(2000);
            e.Property(x => x.DispositionNote).HasMaxLength(2000);
            e.Property(x => x.RequestedAt).HasDefaultValueSql("now()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ExpenseLine>().WithMany().HasForeignKey(x => x.ExpenseLineId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.VerifierUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.QueryToUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.QueriedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<ReceiptAttachment>(e =>
        {
            e.HasIndex(x => x.ReportId);
            e.Property(x => x.UploadedAt).HasDefaultValueSql("now()");
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ReminderDelivery>(e =>
        {
            e.HasIndex(x => new { x.ReportId, x.StepId, x.ReminderNumber, x.Channel, x.Audience }).IsUnique();
            e.Property(x => x.Channel).HasMaxLength(16);
            e.Property(x => x.Audience).HasMaxLength(16);
            e.Property(x => x.Status).HasMaxLength(16);
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SmsDelivery>(e =>
        {
            e.ToTable("sms_deliveries");
            e.HasIndex(x => new { x.SenderUserId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => x.RecipientUserId);
            e.HasIndex(x => x.ReportId);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.MessageSha256).HasMaxLength(64).IsFixedLength();
            e.Property(x => x.Status).HasMaxLength(16);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<EmailDelivery>(e =>
        {
            e.ToTable("email_deliveries");
            e.HasIndex(x => new { x.SenderUserId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.ReportId, x.CreatedAt });
            e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            e.Property(x => x.Kind).HasMaxLength(40);
            e.Property(x => x.Status).HasMaxLength(16);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LoginSecurity>(e =>
        {
            e.ToTable("login_security");
            e.HasKey(x => x.Username);
            e.Property(x => x.Username).HasMaxLength(64).ValueGeneratedNever();
        });

        b.Entity<WorkflowAudit>(e =>
        {
            e.ToTable("workflow_audit");
            e.Property(x => x.Event).HasMaxLength(64);
            e.Property(x => x.OccurredAt).HasDefaultValueSql("now()");
            e.Property(x => x.Detail).HasColumnType("jsonb");
            e.HasIndex(x => x.ReportId);
        });

        b.Entity<ReferenceSeries>(e =>
        {
            e.ToTable("reference_series");
            e.HasKey(x => x.SeriesKey);
            e.Property(x => x.SeriesKey).HasMaxLength(32).ValueGeneratedNever();
            e.Property(x => x.NextValue).HasDefaultValue(1);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            AddConcurrencyToken(e);
        });

        b.Entity<ScopeAssignment>(e =>
        {
            e.ToTable("scope_assignments");
            e.Property(x => x.EffectiveFrom).HasDefaultValueSql("current_date");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => x.HolderUserId);
            e.HasIndex(x => new { x.DepartmentId, x.SignatoryLevel });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.HolderUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId);
            AddConcurrencyToken(e);
        });

        b.Entity<ScopeAssignmentMember>(e =>
        {
            e.ToTable("scope_assignment_members");
            e.HasKey(x => new { x.ScopeAssignmentId, x.UserId });
            e.HasIndex(x => x.UserId);
            e.HasOne<ScopeAssignment>().WithMany().HasForeignKey(x => x.ScopeAssignmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RateProfile>(e =>
        {
            e.ToTable("rate_profiles");
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<RateProfileLine>(e =>
        {
            e.ToTable("rate_profile_lines");
            e.Property(x => x.SubCategory).HasMaxLength(32);
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("PHP");
            e.Property(x => x.RequiresReceipt).HasDefaultValue(true);
            e.Property(x => x.ChangeReason).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.RateProfileId, x.Category, x.SubCategory, x.EffectiveFrom });
            e.HasOne<RateProfile>().WithMany().HasForeignKey(x => x.RateProfileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<RateProfileAssignment>(e =>
        {
            e.ToTable("rate_profile_assignments");
            e.Property(x => x.JobGrade).HasMaxLength(32);
            e.Property(x => x.EffectiveFrom).HasDefaultValueSql("current_date");
            e.Property(x => x.AssignedAt).HasDefaultValueSql("now()");
            e.Property(x => x.ChangeReason).HasMaxLength(500);
            e.HasIndex(x => new { x.TargetType, x.UserId, x.JobGrade, x.DepartmentId });
            e.HasOne<RateProfile>().WithMany().HasForeignKey(x => x.RateProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId);
            AddConcurrencyToken(e);
        });

        b.Entity<RecordHistory>(e =>
        {
            e.ToTable("record_history");
            e.Property(x => x.TableName).HasMaxLength(64);
            e.Property(x => x.RowPk).HasMaxLength(64);
            e.Property(x => x.Operation).HasMaxLength(1);
            e.Property(x => x.ChangedAt).HasDefaultValueSql("now()");
            e.Property(x => x.OldData).HasColumnType("jsonb");
            e.Property(x => x.NewData).HasColumnType("jsonb");
            e.HasIndex(x => new { x.TableName, x.RowPk });
        });
    }

    /// <summary>
    /// Maps the PostgreSQL system column <c>xmin</c> as an optimistic-concurrency
    /// token. Npgsql recognises the <c>xid</c> system column and does not emit it
    /// as a real column in migrations.
    /// </summary>
    private static void AddConcurrencyToken(EntityTypeBuilder builder) =>
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
}
