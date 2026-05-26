Imports System.Data.Entity
Imports ERSystem.Domain

Public Class AppDbContext
    Inherits DbContext

    Public Sub New()
        MyBase.New("name=AppDbContext")
    End Sub

    Public Property ReportsDetails As DbSet(Of ReportDetailModel)
    Public Property CashAdvances As DbSet(Of CashAdvanceModel)
    Public Property ReportFinanceTrackings As DbSet(Of ReportFinanceTrackingModel)
    Public Property UserRegistrations As DbSet(Of UserRegistrationModel)
    Public Property EmployeeRates As DbSet(Of EmployeeRateModel)
    Public Property UserAuthorities As DbSet(Of UserAuthorityModel)
    Public Property Departments As DbSet(Of DepartmentModel)
    Public Property ExpenseDetails As DbSet(Of ExpenseDetailModel)
    Public Property ExpenseMealItems As DbSet(Of ExpenseMealItemModel)
    Public Property ExpenseTransportationItems As DbSet(Of ExpenseTransportationItemModel)
    Public Property ExpenseNotifications As DbSet(Of ExpenseNotificationModel)

    Protected Overrides Sub OnModelCreating(modelBuilder As DbModelBuilder)
        MyBase.OnModelCreating(modelBuilder)

        modelBuilder.Entity(Of ReportDetailModel)().ToTable("tbReportDetails")
        modelBuilder.Entity(Of CashAdvanceModel)().ToTable("tbCashAdvance")
        modelBuilder.Entity(Of ReportFinanceTrackingModel)().ToTable("tbReportFinanceTracking")
        modelBuilder.Entity(Of UserRegistrationModel)().ToTable("tbUserRegistration")
        modelBuilder.Entity(Of EmployeeRateModel)().ToTable("tblEmpRate")
        modelBuilder.Entity(Of UserAuthorityModel)().ToTable("tbUserAuthority")
        modelBuilder.Entity(Of DepartmentModel)().ToTable("tblDept")
        modelBuilder.Entity(Of ExpenseDetailModel)().ToTable("tbExpenseDetails")
        modelBuilder.Entity(Of ExpenseMealItemModel)().ToTable("tbExpenseMealItem")
        modelBuilder.Entity(Of ExpenseTransportationItemModel)().ToTable("tbExpenseTransportationItem")
        modelBuilder.Entity(Of ExpenseNotificationModel)().ToTable("tbNotification")
    End Sub
End Class
