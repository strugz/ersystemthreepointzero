Imports ERSystem.Core.Domain.Entities

Namespace Application.Interfaces
    Public Interface IExpenseService
        Sub AddExpense(expense As Expense)
        Sub UpdateExpense(expense As Expense)
    End Interface
End Namespace
