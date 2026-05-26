Option Strict On

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IExpenseIdCounterRepository
        Function GetNextExpenseId(dbContext As AppDbContext) As Long
    End Interface
End Namespace
