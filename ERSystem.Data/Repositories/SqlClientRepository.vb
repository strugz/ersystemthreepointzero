Imports System.Data
Imports System.Data.SqlClient
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class SqlClientRepository
        Implements IClientRepository

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        Public Function LoadClient() As DataTable Implements IClientRepository.LoadClient
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("Select a.ID,a.clientName from tblClient as a order by a.clientName", conn)
                    cmd.CommandType = CommandType.Text
                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadClientToGrid(ClientCodeName As String) As DataTable Implements IClientRepository.LoadClientToGrid
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadClientToGrid", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@ClientCodeName", SqlDbType.VarChar).Value = ClientCodeName
                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadSearchClient(ClientName As String) As String Implements IClientRepository.LoadSearchClient
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("Select * from tblClient as a where a.clientName = '" & ClientName & "'", conn)
                    cmd.CommandType = CommandType.Text
                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt.Rows.Count.ToString()
        End Function

        Public Function LoadHistory(Details As String, DataToLoad As String) As DataTable Implements IClientRepository.LoadHistory
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadClientData", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@ClientInstrumentSerialService", SqlDbType.VarChar).Value = Details
                    cmd.Parameters.Add("@ClientDataToLoad", SqlDbType.VarChar).Value = DataToLoad
                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

    End Class
End Namespace