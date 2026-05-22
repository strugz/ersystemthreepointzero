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

    Protected Overrides Sub OnModelCreating(modelBuilder As DbModelBuilder)
        MyBase.OnModelCreating(modelBuilder)

        modelBuilder.Entity(Of ReportDetailModel)().ToTable("tbReportDetails")
        modelBuilder.Entity(Of CashAdvanceModel)().ToTable("tbCashAdvance")
        modelBuilder.Entity(Of ReportFinanceTrackingModel)().ToTable("tbReportFinanceTracking")
        modelBuilder.Entity(Of UserRegistrationModel)().ToTable("tbUserRegistration")
    End Sub
End Class
