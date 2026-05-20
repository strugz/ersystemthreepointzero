Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface ICashAdvanceService
        Function GetAll() As List(Of CashAdvanceDto)
        Function GetByReportId(reportId As String) As List(Of CashAdvanceDto)
    End Interface
End Namespace
