Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface ICashAdvanceService
        Function GetAll() As List(Of CashAdvanceDto)
        Function GetByReportId(reportId As String) As List(Of CashAdvanceDto)
        Function Create(cashAdvance As CreateCashAdvanceDto) As CashAdvanceDto
        Sub UpdateByReportId(reportId As String, cashAdvance As UpdateCashAdvanceDto)
    End Interface
End Namespace
