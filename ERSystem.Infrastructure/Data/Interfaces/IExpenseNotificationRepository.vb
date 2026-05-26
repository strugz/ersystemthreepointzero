Option Strict On

Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IExpenseNotificationRepository
        Function Create(notification As ExpenseNotificationDto, dbContext As AppDbContext) As ExpenseNotificationDto
        Sub DeleteByExpenseId(expenseId As Long, dbContext As AppDbContext)
    End Interface
End Namespace
