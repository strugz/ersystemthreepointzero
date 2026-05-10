Imports ERSystem.Core.Domain.Entities
Imports ERSystem.Core.Domain.Interfaces
Imports ERSystem.Core.Application.Interfaces

Namespace Application.Services
    Public Class ExpenseService
        Implements IExpenseService

        Private ReadOnly _repository As IExpenseRepository

        Public Sub New(repository As IExpenseRepository)
             _repository = repository
        End Sub

        Public Sub AddExpense(expense As Expense) Implements IExpenseService.AddExpense
            expense.WorkWith = If(String.IsNullOrEmpty(expense.WorkWith), "NONE", expense.WorkWith)
            _repository.AddExpense(expense)
        End Sub

        Public Sub UpdateExpense(expense As Expense) Implements IExpenseService.UpdateExpense
            expense.WorkWith = If(String.IsNullOrEmpty(expense.WorkWith), "NONE", expense.WorkWith)
            _repository.UpdateExpense(expense)
        End Sub
    End Class
End Namespace
