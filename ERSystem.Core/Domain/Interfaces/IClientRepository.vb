Imports System.Data

Namespace Domain.Interfaces
    Public Interface IClientRepository
        Function LoadClient() As DataTable
        Function LoadClientToGrid(ClientCodeName As String) As DataTable
        Function LoadSearchClient(ClientName As String) As String
        Function LoadHistory(Details As String, DataToLoad As String) As DataTable
    End Interface
End Namespace