Option Strict On

Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ExpenseTransportationItemRepository
        Implements IExpenseTransportationItemRepository

        Public Function Create(transportationItem As ExpenseTransportationItemDto, dbContext As AppDbContext) As ExpenseTransportationItemDto Implements IExpenseTransportationItemRepository.Create
            If transportationItem Is Nothing Then
                Throw New ArgumentNullException("transportationItem")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim model As ExpenseTransportationItemModel = ToModel(transportationItem)
            dbContext.ExpenseTransportationItems.Add(model)
            dbContext.SaveChanges()
            Return ToDto(model)
        End Function

        Public Function UpsertByExpenseId(transportationItem As ExpenseTransportationItemDto, dbContext As AppDbContext) As ExpenseTransportationItemDto Implements IExpenseTransportationItemRepository.UpsertByExpenseId
            If transportationItem Is Nothing Then
                Throw New ArgumentNullException("transportationItem")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            If Not transportationItem.expense_id.HasValue Then
                Throw New ArgumentException("Expense ID is required.", "transportationItem")
            End If

            Dim expenseId As Long = transportationItem.expense_id.Value
            Dim existing As ExpenseTransportationItemModel = dbContext.ExpenseTransportationItems.FirstOrDefault(Function(item) item.expense_id.HasValue AndAlso item.expense_id.Value = expenseId)

            If existing Is Nothing Then
                Return Create(transportationItem, dbContext)
            End If

            existing.FareID = transportationItem.FareID
            existing.FareFrom = transportationItem.FareFrom
            existing.FareTo = transportationItem.FareTo
            dbContext.SaveChanges()
            Return ToDto(existing)
        End Function

        Private Shared Function ToModel(transportationItem As ExpenseTransportationItemDto) As ExpenseTransportationItemModel
            Return New ExpenseTransportationItemModel With {
                .expense_id = transportationItem.expense_id,
                .FareID = transportationItem.FareID,
                .FareFrom = transportationItem.FareFrom,
                .FareTo = transportationItem.FareTo
            }
        End Function

        Private Shared Function ToDto(transportationItem As ExpenseTransportationItemModel) As ExpenseTransportationItemDto
            Return New ExpenseTransportationItemDto With {
                .id = transportationItem.id,
                .expense_id = transportationItem.expense_id,
                .FareID = transportationItem.FareID,
                .FareFrom = transportationItem.FareFrom,
                .FareTo = transportationItem.FareTo
            }
        End Function
    End Class
End Namespace
