Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface ICashAdvanceRepository
        Function GetAll() As List(Of CashAdvanceDto)
        Function GetByReportId(reportId As String) As List(Of CashAdvanceDto)
        Function Create(cashAdvance As CreateCashAdvanceDto) As CashAdvanceDto
        Function Create(cashAdvance As CreateCashAdvanceDto, dbContext As AppDbContext) As CashAdvanceDto
        Sub UpdateByReportId(reportId As String, cashAdvance As UpdateCashAdvanceDto)
        Sub UpdateByReportId(reportId As String, cashAdvance As UpdateCashAdvanceDto, dbContext As AppDbContext)
    End Interface
End Namespace
