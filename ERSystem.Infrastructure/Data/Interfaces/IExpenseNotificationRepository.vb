Option Strict On

Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IExpenseNotificationRepository
        Function Create(notification As ExpenseNotificationDto, dbContext As AppDbContext) As ExpenseNotificationDto
    End Interface
End Namespace
