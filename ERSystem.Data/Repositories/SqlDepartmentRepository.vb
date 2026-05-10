Imports System.Data
Imports System.Data.SqlClient
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class SqlDepartmentRepository
        Implements IDepartmentRepository

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        Public Function LoadingDepartment() As DataTable Implements IDepartmentRepository.LoadingDepartment
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadDepartment", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

    End Class
End Namespace