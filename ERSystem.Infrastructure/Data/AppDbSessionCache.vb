Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Friend NotInheritable Class AppDbSessionCache
        Private Sub New()
        End Sub

        Private Shared ReadOnly _syncRoot As New Object()
        Private Shared _isWarming As Boolean
        Private Shared _isReady As Boolean
        Private Shared _reportDetails As List(Of ReportDetailModel)
        Private Shared _cashAdvances As List(Of CashAdvanceModel)

        Friend Shared ReadOnly Property IsReady As Boolean
            Get
                SyncLock _syncRoot
                    Return _isReady
                End SyncLock
            End Get
        End Property

        Friend Shared Sub BeginWarmup()
            SyncLock _syncRoot
                If _isWarming OrElse _isReady Then
                    Return
                End If

                _isWarming = True
            End SyncLock
        End Sub

        Friend Shared Function IsWarmupRequired() As Boolean
            SyncLock _syncRoot
                Return (Not _isWarming) AndAlso (Not _isReady)
            End SyncLock
        End Function

        Friend Shared Sub SetData(reportDetails As List(Of ReportDetailModel), cashAdvances As List(Of CashAdvanceModel))
            SyncLock _syncRoot
                _reportDetails = reportDetails
                _cashAdvances = cashAdvances
                _isReady = True
                _isWarming = False
            End SyncLock
        End Sub

        Friend Shared Sub MarkWarmupFailed()
            SyncLock _syncRoot
                _isWarming = False
            End SyncLock
        End Sub

        Friend Shared Sub Clear()
            SyncLock _syncRoot
                _reportDetails = Nothing
                _cashAdvances = Nothing
                _isReady = False
                _isWarming = False
            End SyncLock
        End Sub
    End Class
End Namespace
