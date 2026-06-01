Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface ITransactionRepository
        Function GetFWMSBySRNumber(srNumber As String) As FwmsTransactionDto
    End Interface
End Namespace
