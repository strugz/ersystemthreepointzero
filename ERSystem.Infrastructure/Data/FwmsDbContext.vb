Imports System.Data.Entity
Imports ERSystem.Domain

Public Class FwmsDbContext
    Inherits DbContext

    Public Sub New()
        MyBase.New("name=FwmsDb")
    End Sub

    Public Property ContactMasters As DbSet(Of ContactMasterModel)
    Public Property TradeMasters As DbSet(Of TradeMasterModel)
    Public Property AccountMasters As DbSet(Of AccountMasterModel)

    Protected Overrides Sub OnModelCreating(modelBuilder As DbModelBuilder)
        MyBase.OnModelCreating(modelBuilder)

        modelBuilder.Entity(Of ContactMasterModel)().ToTable("CNTMST")
        modelBuilder.Entity(Of TradeMasterModel)().ToTable("TRDMST")
        modelBuilder.Entity(Of AccountMasterModel)().ToTable("ACCMST_")
    End Sub
End Class
