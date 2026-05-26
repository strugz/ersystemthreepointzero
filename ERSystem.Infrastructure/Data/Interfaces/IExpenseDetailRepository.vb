Option Strict On

Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IExpenseDetailRepository
        Function GetAll() As List(Of ExpenseDetailDto)
        Function GetById(expenseId As Integer) As ExpenseDetailDto
        Function Create(expense As CreateExpenseDetailDto) As ExpenseDetailDto
        Function Create(expense As CreateExpenseDetailDto, dbContext As AppDbContext) As ExpenseDetailDto
        Function CountActiveByReportId(reportId As String, dbContext As AppDbContext) As Integer
        Sub Update(expense As UpdateExpenseDetailDto)
    End Interface
End Namespace
