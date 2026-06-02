Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface ITransactionRepository
        Function GetFWMSBySRNumber(srNumber As String) As FwmsTransactionDto
        Function GetFWMSByTransactionDateRange(dateFrom As Date, dateTo As Date, userInitial As String) As List(Of FwmsTransactionDto)
    End Interface
End Namespace
