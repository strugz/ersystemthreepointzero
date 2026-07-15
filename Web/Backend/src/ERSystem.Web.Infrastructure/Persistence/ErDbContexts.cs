using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERSystem.Web.Infrastructure.Persistence;

public sealed class LegacyErDbContext(DbContextOptions<LegacyErDbContext> options) : DbContext(options)
{
    public DbSet<UserRegistrationEntity> Users => Set<UserRegistrationEntity>();
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<UserAuthorityEntity> UserAuthorities => Set<UserAuthorityEntity>();
    public DbSet<ReportDetailEntity> Reports => Set<ReportDetailEntity>();
    public DbSet<ExpenseDetailEntity> Expenses => Set<ExpenseDetailEntity>();
    public DbSet<CashAdvanceEntity> CashAdvances => Set<CashAdvanceEntity>();
    public DbSet<ReportAuthorityEntity> ReportAuthorities => Set<ReportAuthorityEntity>();
    public DbSet<ReportApprovalTransactionEntity> ApprovalTransactions => Set<ReportApprovalTransactionEntity>();
    public DbSet<ReportFinanceTrackingEntity> FinanceTracking => Set<ReportFinanceTrackingEntity>();
    public DbSet<ScannedReceiptAttachmentEntity> ScannedReceipts => Set<ScannedReceiptAttachmentEntity>();
    public DbSet<WebWorkflowAuditEntity> WorkflowAudits => Set<WebWorkflowAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureLegacy(modelBuilder);
        ConfigureWorkflowAudit(modelBuilder.Entity<WebWorkflowAuditEntity>());
    }

    internal static void ConfigureLegacy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRegistrationEntity>(entity =>
        {
            entity.ToTable("tbUserRegistration");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).HasColumnName("UserID");
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.Password).HasColumnName("Password");
            entity.Property(x => x.FullName).HasColumnName("Fullname").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.UserLevel).HasColumnName("Userlevel").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.DepartmentId).HasColumnName("DeptID");
            entity.Property(x => x.Signature).HasColumnName("Signature");
            entity.Property(x => x.ReportNumberStatus).HasColumnName("ReportNumberStatus");
        });

        modelBuilder.Entity<DepartmentEntity>(entity =>
        {
            entity.ToTable("tblDept");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasColumnName("emp_Dept").HasMaxLength(50).IsUnicode(false);
        });

        modelBuilder.Entity<UserAuthorityEntity>(entity =>
        {
            entity.ToTable("tbUserAuthority");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).HasColumnName("UserID");
            entity.Property(x => x.AuthorityId).HasColumnName("AuthorityID");
            entity.Property(x => x.AuthorityName).HasColumnName("AuthorityName").HasMaxLength(10).IsUnicode(false);
            entity.Property(x => x.Sort).HasColumnName("Sort");
        });

        modelBuilder.Entity<ReportDetailEntity>(entity =>
        {
            entity.ToTable("tbReportDetails");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").HasMaxLength(50).IsUnicode(false).ValueGeneratedNever();
            entity.Property(x => x.ReportDateFrom).HasColumnName("ReportDateFrom").HasColumnType("date");
            entity.Property(x => x.ReportDateTo).HasColumnName("ReportDateTo").HasColumnType("date");
            entity.Property(x => x.ReportDescription).HasColumnName("ReportDescription").IsUnicode(false);
            entity.Property(x => x.UserId).HasColumnName("UserID");
            entity.Property(x => x.ReportEndorseStatus).HasColumnName("ReportEndorseStatus").HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.ReportFileStatus).HasColumnName("ReportFileStatus").HasMaxLength(1).IsUnicode(false);
            entity.Property(x => x.ReportPrintStatus).HasColumnName("ReportPrintStatus").HasMaxLength(1).IsUnicode(false);
            entity.Property(x => x.ReportReturnedForModification).HasColumnName("ReportReturnedForModi").IsUnicode(false);
            entity.Property(x => x.ReportNumberStatus).HasColumnName("ReportNumberStatus");
            entity.Property(x => x.ReportReserveStatus1).HasColumnName("ReportReserveStatus1").HasMaxLength(10).IsUnicode(false);
            entity.Property(x => x.ReportReserveStatus2).HasColumnName("ReportReserveStatus2").HasMaxLength(10).IsUnicode(false);
            entity.Property(x => x.ReportCancelNote).HasColumnName("ReportCancelNote").HasMaxLength(255).IsUnicode(false);
            entity.Property(x => x.ReportAttachment).HasColumnName("ReportAttachment").IsUnicode(false);
            entity.Property(x => x.ReportType).HasColumnName("ReportType").HasColumnType("nchar(50)");
            entity.Property(x => x.ErfReferenceNumber).HasColumnName("ERFReferenceNo").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();
        });

        modelBuilder.Entity<ExpenseDetailEntity>(entity =>
        {
            entity.ToTable("tbExpenseDetails");
            entity.HasNoKey();
            entity.Property(x => x.Id).HasColumnName("ID");
            entity.Property(x => x.TransactionDate).HasColumnName("ExpenseTransDate").HasColumnType("date");
            entity.Property(x => x.Particulars).HasColumnName("ExpenseParticulars").IsUnicode(false);
            entity.Property(x => x.Category).HasColumnName("ExpenseCategory").HasMaxLength(25).IsUnicode(false);
            entity.Property(x => x.Amount).HasColumnName("ExpenseAmount");
            entity.Property(x => x.Remarks).HasColumnName("ExpenseRemarks").IsUnicode(false);
            entity.Property(x => x.TotalAmount).HasColumnName("ExpenseTotalAmount");
            entity.Property(x => x.Location).HasColumnName("ExpenseLocation").HasMaxLength(255).IsUnicode(false);
            entity.Property(x => x.ReportId).HasColumnName("ReportID").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.Sort).HasColumnName("Sort");
        });

        modelBuilder.Entity<CashAdvanceEntity>(entity =>
        {
            entity.ToTable("tbCashAdvance");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.ReportId).HasColumnName("ReportID").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.Amount).HasColumnName("CashAmount");
            entity.Property(x => x.Date).HasColumnName("CashDate").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.ReferenceDocument).HasColumnName("CashRefDoc").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.ReferenceNumber).HasColumnName("CashRefNo").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.RevolvingFund).HasColumnName("RevolvingFund").HasMaxLength(50).IsUnicode(false);
        });

        modelBuilder.Entity<ReportAuthorityEntity>(entity =>
        {
            entity.ToTable("tbReportAuthority");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.ReportId).HasColumnName("ReportID").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.SignId).HasColumnName("SignID");
            entity.Property(x => x.UserId).HasColumnName("UserID");
            entity.Property(x => x.AuthoritySignature).HasColumnName("AuthoritySignature");
        });

        modelBuilder.Entity<ReportApprovalTransactionEntity>(entity =>
        {
            entity.ToTable("tbReportApprovalTransaction");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.ReportId).HasColumnName("ReportID").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.ApprovalCycle).HasColumnName("ApprovalCycle");
            entity.Property(x => x.EmployeeUserId).HasColumnName("EmployeeUserID");
            entity.Property(x => x.ApproverUserId).HasColumnName("ApproverUserID");
            entity.Property(x => x.StepOrder).HasColumnName("StepOrder");
            entity.Property(x => x.Status).HasColumnName("Status").HasMaxLength(20).IsUnicode(false);
            entity.Property(x => x.SubmittedAtUtc).HasColumnName("SubmittedAtUtc");
            entity.Property(x => x.ActionedAtUtc).HasColumnName("ActionedAtUtc");
            entity.Property(x => x.ActionRemarks).HasColumnName("ActionRemarks").HasMaxLength(1000);
            entity.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();
        });

        modelBuilder.Entity<ReportFinanceTrackingEntity>(entity =>
        {
            entity.ToTable("tbReportFinanceTracking");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.ReportId).HasColumnName("ReportID").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.FinanceStatus).HasColumnName("FinanceStatus").HasMaxLength(100).IsUnicode(false);
            entity.Property(x => x.PhysicalReceiptsReceived).HasColumnName("PhysicalReceiptsReceived");
            entity.Property(x => x.PhysicalReceiptsReceivedBy).HasColumnName("PhysicalReceiptsReceivedBy");
            entity.Property(x => x.PhysicalReceiptsReceivedDate).HasColumnName("PhysicalReceiptsReceivedDate");
            entity.Property(x => x.FinanceRemarks).HasColumnName("FinanceRemarks").IsUnicode(false);
            entity.Property(x => x.ScannedReceiptsDeletedDate).HasColumnName("ScannedReceiptsDeletedDate");
            entity.Property(x => x.RowVersion).HasColumnName("RowVersion").IsRowVersion();
        });

        modelBuilder.Entity<ScannedReceiptAttachmentEntity>(entity =>
        {
            entity.ToTable("tbScannedReceiptAttachment");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.ReportId).HasColumnName("ReportID").HasMaxLength(50).IsUnicode(false);
            entity.Property(x => x.OriginalFileName).HasColumnName("OriginalFileName").HasMaxLength(260);
            entity.Property(x => x.ContentType).HasColumnName("ContentType").HasMaxLength(100);
            entity.Property(x => x.FileSizeBytes).HasColumnName("FileSizeBytes");
            entity.Property(x => x.ReceiptContent).HasColumnName("ReceiptContent");
            entity.Property(x => x.CreatedDate).HasColumnName("CreatedDate");
        });
    }

    internal static void ConfigureWorkflowAudit(EntityTypeBuilder<WebWorkflowAuditEntity> entity)
    {
        entity.ToTable("tbWebWorkflowAudit");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        entity.Property(x => x.ReportId).HasColumnName("ReportID").HasMaxLength(50).IsUnicode(false);
        entity.Property(x => x.ActorUserId).HasColumnName("ActorUserID");
        entity.Property(x => x.EventType).HasColumnName("EventType").HasMaxLength(40).IsUnicode(false);
        entity.Property(x => x.PreviousState).HasColumnName("PreviousState").HasMaxLength(100).IsUnicode(false);
        entity.Property(x => x.NewState).HasColumnName("NewState").HasMaxLength(100).IsUnicode(false);
        entity.Property(x => x.Remarks).HasColumnName("Remarks").HasMaxLength(1000);
        entity.Property(x => x.OccurredAtUtc).HasColumnName("OccurredAtUtc");
        entity.Property(x => x.CorrelationId).HasColumnName("CorrelationID");
    }
}

public sealed class WebWorkflowDbContext(DbContextOptions<WebWorkflowDbContext> options) : DbContext(options)
{
    public DbSet<WebLoginSecurityEntity> LoginSecurity => Set<WebLoginSecurityEntity>();
    public DbSet<WebWorkflowAuditEntity> WorkflowAudits => Set<WebWorkflowAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebLoginSecurityEntity>(entity =>
        {
            entity.ToTable("tbWebLoginSecurity");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasColumnName("UserID").ValueGeneratedNever();
            entity.Property(x => x.FailedAttemptCount).HasColumnName("FailedAttemptCount");
            entity.Property(x => x.FirstFailedAttemptUtc).HasColumnName("FirstFailedAttemptUtc");
            entity.Property(x => x.LockoutEndUtc).HasColumnName("LockoutEndUtc");
            entity.Property(x => x.LastSuccessfulLoginUtc).HasColumnName("LastSuccessfulLoginUtc");
        });
        LegacyErDbContext.ConfigureWorkflowAudit(modelBuilder.Entity<WebWorkflowAuditEntity>());
    }
}
