Imports ERSystem.Domain.Dtos.CashAdvance

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface ICashAdvanceRepository
        Function GetAll() As List(Of CashAdvanceDto)
        Function GetByReportId(reportId As String) As List(Of CashAdvanceDto)
    End Interface
End Namespace
