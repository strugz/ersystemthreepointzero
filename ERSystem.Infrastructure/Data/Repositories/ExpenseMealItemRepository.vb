Option Strict On

Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ExpenseMealItemRepository
        Implements IExpenseMealItemRepository

        Public Function Create(mealItem As ExpenseMealItemDto, dbContext As AppDbContext) As ExpenseMealItemDto Implements IExpenseMealItemRepository.Create
            If mealItem Is Nothing Then
                Throw New ArgumentNullException("mealItem")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim model As ExpenseMealItemModel = ToModel(mealItem)
            dbContext.ExpenseMealItems.Add(model)
            dbContext.SaveChanges()
            Return ToDto(model)
        End Function

        Public Function UpsertByExpenseId(mealItem As ExpenseMealItemDto, dbContext As AppDbContext) As ExpenseMealItemDto Implements IExpenseMealItemRepository.UpsertByExpenseId
            If mealItem Is Nothing Then
                Throw New ArgumentNullException("mealItem")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            If Not mealItem.ExpenseID.HasValue Then
                Throw New ArgumentException("Expense ID is required.", "mealItem")
            End If

            Dim expenseId As Long = mealItem.ExpenseID.Value
            Dim existing As ExpenseMealItemModel = dbContext.ExpenseMealItems.FirstOrDefault(Function(item) item.ExpenseID.HasValue AndAlso item.ExpenseID.Value = expenseId)

            If existing Is Nothing Then
                Return Create(mealItem, dbContext)
            End If

            existing.Meal = mealItem.Meal
            existing.PaidFor = mealItem.PaidFor
            existing.PaidEmp = mealItem.PaidEmp
            dbContext.SaveChanges()
            Return ToDto(existing)
        End Function

        Private Shared Function ToModel(mealItem As ExpenseMealItemDto) As ExpenseMealItemModel
            Return New ExpenseMealItemModel With {
                .Meal = mealItem.Meal,
                .PaidFor = mealItem.PaidFor,
                .PaidEmp = mealItem.PaidEmp,
                .ExpenseID = mealItem.ExpenseID
            }
        End Function

        Private Shared Function ToDto(mealItem As ExpenseMealItemModel) As ExpenseMealItemDto
            Return New ExpenseMealItemDto With {
                .id = mealItem.id,
                .Meal = mealItem.Meal,
                .PaidFor = mealItem.PaidFor,
                .PaidEmp = mealItem.PaidEmp,
                .ExpenseID = mealItem.ExpenseID
            }
        End Function
    End Class
End Namespace
