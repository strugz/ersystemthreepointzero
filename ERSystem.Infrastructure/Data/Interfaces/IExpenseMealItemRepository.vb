Option Strict On

Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IExpenseMealItemRepository
        Function Create(mealItem As ExpenseMealItemDto, dbContext As AppDbContext) As ExpenseMealItemDto
        Function UpsertByExpenseId(mealItem As ExpenseMealItemDto, dbContext As AppDbContext) As ExpenseMealItemDto
    End Interface
End Namespace
