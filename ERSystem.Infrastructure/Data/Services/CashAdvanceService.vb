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

        Public Function Create(cashAdvance As CreateCashAdvanceDto) As CashAdvanceDto Implements ICashAdvanceService.Create
            Return _repository.Create(cashAdvance)
        End Function

        Public Sub UpdateByReportId(reportId As String, cashAdvance As UpdateCashAdvanceDto) Implements ICashAdvanceService.UpdateByReportId
            _repository.UpdateByReportId(reportId, cashAdvance)
        End Sub
    End Class
End Namespace
