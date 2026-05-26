Option Strict On

Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IExpenseTransportationItemRepository
        Function Create(transportationItem As ExpenseTransportationItemDto, dbContext As AppDbContext) As ExpenseTransportationItemDto
        Function UpsertByExpenseId(transportationItem As ExpenseTransportationItemDto, dbContext As AppDbContext) As ExpenseTransportationItemDto
    End Interface
End Namespace
