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
    public DbSet<User> Users => Set<User>();
    public DbSet<ApprovalRoute> ApprovalRoutes => Set<ApprovalRoute>();
    public DbSet<DepartmentSignatory> DepartmentSignatories => Set<DepartmentSignatory>();
    public DbSet<EmployeeRate> EmployeeRates => Set<EmployeeRate>();
    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();
    public DbSet<CashAdvanceLine> CashAdvanceLines => Set<CashAdvanceLine>();
    public DbSet<ExpenseLine> ExpenseLines => Set<ExpenseLine>();
    public DbSet<ExpenseMealItem> ExpenseMealItems => Set<ExpenseMealItem>();
    public DbSet<ExpenseTransportItem> ExpenseTransportItems => Set<ExpenseTransportItem>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<FinanceTracking> FinanceTrackings => Set<FinanceTracking>();
    public DbSet<ReceiptAttachment> ReceiptAttachments => Set<ReceiptAttachment>();
    public DbSet<ReminderDelivery> ReminderDeliveries => Set<ReminderDelivery>();
    public DbSet<LoginSecurity> LoginSecurities => Set<LoginSecurity>();
    public DbSet<WorkflowAudit> WorkflowAudits => Set<WorkflowAudit>();
    public DbSet<RecordHistory> RecordHistories => Set<RecordHistory>();
    public DbSet<ReferenceSeries> ReferenceSeries => Set<ReferenceSeries>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Store enums as readable text (e.g. 'ForApproval') rather than native PG enums.
        builder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<ReportType>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<ReportStatus>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<ApprovalStatus>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<FinanceStatus>().HaveConversion<string>().HaveMaxLength(24);
        builder.Properties<ExpenseType>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<ExpenseCategory>().HaveConversion<string>().HaveMaxLength(24);

        // Money defaults to numeric(14,2) across the model.
        builder.Properties<decimal>().HavePrecision(14, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Department>(e => e.HasIndex(x => x.Code).IsUnique());

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId);
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
            e.HasOne<User>().WithMany().HasForeignKey(x => x.EmployeeUserId).OnDelete(DeleteBehavior.Restrict);
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
            e.HasOne<ExpenseLine>().WithMany().HasForeignKey(x => x.ExpenseLineId).OnDelete(DeleteBehavior.Cascade));

        b.Entity<ApprovalStep>(e =>
        {
            e.HasIndex(x => new { x.ReportId, x.Cycle, x.StepOrder }).IsUnique();
            e.HasIndex(x => new { x.ReportId, x.Status });
            e.Property(x => x.Cycle).HasDefaultValue(1);
            e.Property(x => x.Status).HasDefaultValue(ApprovalStatus.Pending);
            e.HasOne<ExpenseReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
            AddConcurrencyToken(e);
        });

        b.Entity<FinanceTracking>(e =>
        {
            e.ToTable("finance_tracking");
            e.HasKey(x => x.ReportId);
            e.Property(x => x.ReportId).ValueGeneratedNever();
            e.Property(x => x.Status).HasDefaultValue(FinanceStatus.Pending);
            e.HasOne<ExpenseReport>().WithOne().HasForeignKey<FinanceTracking>(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
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
