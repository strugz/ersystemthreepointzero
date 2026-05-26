Option Strict On

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
