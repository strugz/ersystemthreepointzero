Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class CashAdvanceService
        Implements ICashAdvanceService

        Private ReadOnly _repository As ICashAdvanceRepository

        Public Sub New()
            Me.New(New CashAdvanceRepository())
        End Sub

        Public Sub New(repository As ICashAdvanceRepository)
            _repository = repository
        End Sub

        Public Function GetAll() As List(Of CashAdvanceDto) Implements ICashAdvanceService.GetAll
            Return _repository.GetAll()
        End Function

        Public Function GetByReportId(reportId As String) As List(Of CashAdvanceDto) Implements ICashAdvanceService.GetByReportId
            Return _repository.GetByReportId(reportId)
        End Function
    End Class
End Namespace
