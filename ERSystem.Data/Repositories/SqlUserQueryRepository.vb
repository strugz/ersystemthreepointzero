Imports System.Data
Imports System.Data.SqlClient
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class SqlUserQueryRepository
        Implements IUserQueryRepository

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        Public Function LoadDuplicateUser(username As String) As DataTable Implements IUserQueryRepository.LoadDuplicateUser
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadDuplicateUser", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@username", SqlDbType.VarChar).Value = username

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadDuplicateUserID(userid As String) As String Implements IUserQueryRepository.LoadDuplicateUserID
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadDuplicateUserID", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@userid", SqlDbType.VarChar).Value = userid

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using

            If dt.Rows.Count > 0 Then
                Return dt.Rows(0)("UserID").ToString()
            End If
            Return ""
        End Function

        Public Function LoadingOfficersToSign(userid As String) As String Implements IUserQueryRepository.LoadingOfficersToSign
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadOfficersToSign", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@userid", SqlDbType.VarChar).Value = userid

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using

            If dt.Rows.Count > 0 Then Return dt.Rows(0)("UserID").ToString()
            Return ""
        End Function

        Public Function LoadingUserAccountEmail(userid As String, deptID As String) As DataTable Implements IUserQueryRepository.LoadingUserAccountEmail
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadUserAccEmail", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@userid", SqlDbType.Int).Value = userid
                    cmd.Parameters.Add("@deptID", SqlDbType.Int).Value = deptID

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadingUserAccountFiled(deptID As String, SignID As String) As DataTable Implements IUserQueryRepository.LoadingUserAccountFiled
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_LoadUserAccFiled]", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@DeptID", SqlDbType.BigInt).Value = deptID
                    cmd.Parameters.Add("@SignID", SqlDbType.BigInt).Value = SignID

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadingUserAccount(deptID As String) As DataTable Implements IUserQueryRepository.LoadingUserAccount
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_LoadUserAcc]", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@DeptID", SqlDbType.Int).Value = deptID

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadingUserAccountPending(deptID As String) As DataTable Implements IUserQueryRepository.LoadingUserAccountPending
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_LoadUserAccPending]", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@DeptID", SqlDbType.Int).Value = deptID

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadUserAccountAdmin() As DataTable Implements IUserQueryRepository.LoadUserAccountAdmin
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadUserAccountAdmin", conn)
                    cmd.CommandType = CommandType.StoredProcedure

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadingUserAccDept(UserID As String) As DataTable Implements IUserQueryRepository.LoadingUserAccDept
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_LoadUserAccountByDept]", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@UserID", SqlDbType.VarChar).Value = UserID

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoginUserAccount(Username As String, Password As String) As DataTable Implements IUserQueryRepository.LoginUserAccount
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoginUser", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@username", SqlDbType.VarChar).Value = Username
                    cmd.Parameters.Add("@password", SqlDbType.VarChar).Value = Password

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadMaxUserID() As String Implements IUserQueryRepository.LoadMaxUserID
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadUserIDMax", conn)
                    cmd.CommandType = CommandType.StoredProcedure

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using

            If dt.Rows.Count > 0 Then Return dt.Rows(0)("User ID").ToString()
            Return ""
        End Function

    End Class
End Namespace