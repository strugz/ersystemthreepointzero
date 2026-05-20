Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports ERSystem.Infrastructure

Namespace Infrastructure
    <TestClass>
    Public Class AppDbContextTests
        <TestMethod>
        Public Sub Constructor_ExposesExpectedDbSets()
            Using dbContext As New AppDbContext()
                Assert.IsNotNull(dbContext.ReportsDetails)
                Assert.IsNotNull(dbContext.CashAdvances)
            End Using
        End Sub
    End Class
End Namespace
