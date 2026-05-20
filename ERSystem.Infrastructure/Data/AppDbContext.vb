Imports System.Data.Entity
Imports ERSystem.Domain

Public Class AppDbContext
    Inherits DbContext

    Public Sub New()
        MyBase.New("name=AppDbContext")
    End Sub

    Public Property ReportsDetails As DbSet(Of ReportDetailModel)
    Public Property CashAdvances As DbSet(Of CashAdvanceModel)
    Protected Overrides Sub OnModelCreating(modelBuilder As DbModelBuilder)
        MyBase.OnModelCreating(modelBuilder)

        modelBuilder.Entity(Of ReportDetailModel)().ToTable("ReportDetails")
        modelBuilder.Entity(Of CashAdvanceModel)().ToTable("CashAdvances")
    End Sub
End Class
