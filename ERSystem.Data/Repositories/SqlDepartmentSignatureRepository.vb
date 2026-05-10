Imports System.Data
Imports System.Data.SqlClient
Imports ERSystem.Core.Domain.Entities
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class SqlDepartmentSignatureRepository
        Implements IDepartmentSignatureRepository

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        Public Sub AddDepartmentSignature(deptSign As DepartmentSignature) Implements IDepartmentSignatureRepository.AddDepartmentSignature
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_AddDeptSign", conn)
                    cmd.CommandText = "EXEC sp2_AddDeptSign @deptID,@review,@endorse,@approve,@UserID"
                    cmd.CommandType = CommandType.Text

                    cmd.Parameters.Add("@deptID", SqlDbType.VarChar).Value = deptSign.DepartmentID
                    cmd.Parameters.Add("@review", SqlDbType.VarChar).Value = deptSign.Reviewer
                    cmd.Parameters.Add("@endorse", SqlDbType.VarChar).Value = deptSign.Endorser
                    cmd.Parameters.Add("@approve", SqlDbType.VarChar).Value = deptSign.Approver
                    cmd.Parameters.Add("@UserID", SqlDbType.VarChar).Value = deptSign.UserID

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub UpdateDepartmentSignature(deptSign As DepartmentSignature) Implements IDepartmentSignatureRepository.UpdateDepartmentSignature
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_UpdateDeptSign]", conn)
                    cmd.CommandText = "EXEC [sp2_UpdateDeptSign] @UserID,@deptID,@review,@endorse,@approve"
                    cmd.CommandType = CommandType.Text

                    cmd.Parameters.Add("@UserID", SqlDbType.VarChar).Value = deptSign.UserID
                    cmd.Parameters.Add("@deptID", SqlDbType.VarChar).Value = deptSign.DepartmentID
                    cmd.Parameters.Add("@review", SqlDbType.VarChar).Value = deptSign.Reviewer
                    cmd.Parameters.Add("@endorse", SqlDbType.VarChar).Value = deptSign.Endorser
                    cmd.Parameters.Add("@approve", SqlDbType.VarChar).Value = deptSign.Approver

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

    End Class
End Namespace