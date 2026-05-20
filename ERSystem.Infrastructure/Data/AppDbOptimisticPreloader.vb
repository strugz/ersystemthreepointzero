Imports System.Data.Entity
Imports System.Threading.Tasks

Namespace Global.ERSystem.Infrastructure.Data
    Friend NotInheritable Class AppDbOptimisticPreloader
        Private Sub New()
        End Sub

        Friend Shared Sub Start()
            If Not AppDbSessionCache.IsWarmupRequired() Then
                Return
            End If

            AppDbSessionCache.BeginWarmup()

            Task.Run(
                Sub()
                    Try
                        Using dbContext As New AppDbContext()
                            dbContext.Configuration.AutoDetectChangesEnabled = False

                            Dim reportDetails = dbContext.ReportsDetails.AsNoTracking().ToList()
                            Dim cashAdvances = dbContext.CashAdvances.AsNoTracking().ToList()

                            AppDbSessionCache.SetData(reportDetails, cashAdvances)
                        End Using
                    Catch ex As Exception
                        AppDbSessionCache.MarkWarmupFailed()
                    End Try
                End Sub)
        End Sub
    End Class
End Namespace
