Imports ERSystem.Core.Domain.Entities

Namespace Domain.Interfaces
    Public Interface IExpenseRepository
        Sub AddExpense(expense As Expense)
        Sub UpdateExpense(expense As Expense)
    End Interface
End Namespace
