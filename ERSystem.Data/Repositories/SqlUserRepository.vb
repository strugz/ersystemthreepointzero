Imports System.Data
Imports System.Data.SqlClient
Imports ERSystem.Core.Domain.Entities
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class SqlUserRepository
        Implements IUserRepository

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        Public Function GetByUsernameAndPassword(username As String, encodedPassword As String) As UserAccount Implements IUserRepository.GetByUsernameAndPassword
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp_Login", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@username", SqlDbType.VarChar).Value = username
                    cmd.Parameters.Add("@password", SqlDbType.VarChar).Value = encodedPassword

                    conn.Open()
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Return MapToUserAccount(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Function GetById(userId As String) As UserAccount Implements IUserRepository.GetById
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_LoadUserAccountByDept]", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@UserID", SqlDbType.VarChar).Value = userId

                    conn.Open()
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Return MapToUserAccount(reader) ' Adjust columns if sp2_LoadUserAccountByDept differs from sp_Login
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Sub AddUserAccount(user As UserAccount) Implements IUserRepository.AddUserAccount
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp_AddUserAccount]", conn)
                    cmd.CommandType = CommandType.StoredProcedure

                    cmd.Parameters.Add("@UserID", SqlDbType.VarChar).Value = user.UserID
                    cmd.Parameters.Add("@Fullname", SqlDbType.VarChar).Value = user.Fullname
                    cmd.Parameters.Add("@Position", SqlDbType.VarChar).Value = user.Position
                    cmd.Parameters.Add("@Department", SqlDbType.VarChar).Value = user.DepartmentID
                    cmd.Parameters.Add("@username", SqlDbType.VarChar).Value = user.Username
                    cmd.Parameters.Add("@Password", SqlDbType.VarChar).Value = user.Password
                    cmd.Parameters.Add("@emailAdd", SqlDbType.VarChar).Value = user.EmailAddress
                    cmd.Parameters.Add("@EmailPassword", SqlDbType.VarChar).Value = user.EmailPassword
                    cmd.Parameters.Add("@EmaiTo", SqlDbType.VarChar).Value = user.EmailTo
                    cmd.Parameters.Add("@EmaiBcc", SqlDbType.VarChar).Value = user.EmailBcc
                    cmd.Parameters.Add("@userlevel", SqlDbType.VarChar).Value = user.UserLevel
                    cmd.Parameters.Add("@Approver1", SqlDbType.VarChar).Value = user.Approver1Id
                    cmd.Parameters.Add("@Approver2", SqlDbType.VarChar).Value = user.Approver2Id
                    cmd.Parameters.Add("@TransportationRate", SqlDbType.VarChar).Value = user.TransportationRate.ToString()
                    cmd.Parameters.Add("@BreakFastRate", SqlDbType.VarChar).Value = user.BreakfastRate.ToString()
                    cmd.Parameters.Add("@LunchRate", SqlDbType.VarChar).Value = user.LunchRate.ToString()
                    cmd.Parameters.Add("@DinnerRate", SqlDbType.VarChar).Value = user.DinnerRate.ToString()
                    cmd.Parameters.Add("@OTMeal", SqlDbType.VarChar).Value = user.OTMealRate.ToString()

                    Dim picParam = cmd.Parameters.Add("@Signature", SqlDbType.Image)
                    If user.Signature IsNot Nothing AndAlso user.Signature.Length > 0 Then
                        picParam.Value = user.Signature
                    Else
                        picParam.Value = DBNull.Value
                    End If

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub UpdateUserAccount(user As UserAccount) Implements IUserRepository.UpdateUserAccount
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_UpdateUserAcc]", conn)
                    cmd.CommandType = CommandType.StoredProcedure

                    cmd.Parameters.Add("@UserID", SqlDbType.VarChar).Value = user.UserID
                    cmd.Parameters.Add("@Fullname", SqlDbType.VarChar).Value = user.Fullname
                    cmd.Parameters.Add("@Position", SqlDbType.VarChar).Value = user.Position
                    cmd.Parameters.Add("@Department", SqlDbType.VarChar).Value = user.DepartmentID
                    cmd.Parameters.Add("@username", SqlDbType.VarChar).Value = user.Username
                    cmd.Parameters.Add("@Password", SqlDbType.VarChar).Value = user.Password
                    cmd.Parameters.Add("@EmailTo", SqlDbType.VarChar).Value = user.EmailTo
                    cmd.Parameters.Add("@EmailBcc", SqlDbType.VarChar).Value = user.EmailBcc
                    cmd.Parameters.Add("@userlevel", SqlDbType.VarChar).Value = user.UserLevel
                    cmd.Parameters.Add("@Approver1", SqlDbType.VarChar).Value = user.Approver1Id
                    cmd.Parameters.Add("@Approver2", SqlDbType.VarChar).Value = user.Approver2Id
                    cmd.Parameters.Add("@TransportationRate", SqlDbType.VarChar).Value = user.TransportationRate.ToString()
                    cmd.Parameters.Add("@BreakFastRate", SqlDbType.VarChar).Value = user.BreakfastRate.ToString()
                    cmd.Parameters.Add("@LunchRate", SqlDbType.VarChar).Value = user.LunchRate.ToString()
                    cmd.Parameters.Add("@DinnerRate", SqlDbType.VarChar).Value = user.DinnerRate.ToString()
                    cmd.Parameters.Add("@OTMeal", SqlDbType.VarChar).Value = user.OTMealRate.ToString()

                    Dim picParam = cmd.Parameters.Add("@Signature", SqlDbType.Image)
                    If user.Signature IsNot Nothing AndAlso user.Signature.Length > 0 Then
                        picParam.Value = user.Signature
                    Else
                        picParam.Value = DBNull.Value
                    End If

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub UpdateLoginStatus(userId As String, loginStatus As String) Implements IUserRepository.UpdateLoginStatus
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_UpdateAlreadyLogin", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@userID", SqlDbType.NVarChar).Value = userId
                    cmd.Parameters.Add("@alreadyLogin", SqlDbType.NVarChar).Value = loginStatus

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Function MapToUserAccount(reader As IDataReader) As UserAccount
            Dim user As New UserAccount()

            ' These map to the DB Columns returned by the SQL Scripts (like sp_Login and sp_LoadUserAccount)
            If Not IsDBNull(reader("UserID")) Then user.UserID = reader("UserID").ToString()
            If Not IsDBNull(reader("username")) Then user.Username = reader("username").ToString()
            If Not IsDBNull(reader("Userlevel")) Then user.UserLevel = reader("Userlevel").ToString()
            If Not IsDBNull(reader("DeptID")) Then user.DepartmentID = reader("DeptID").ToString()
            If Not IsDBNull(reader("Fullname")) Then user.Fullname = reader("Fullname").ToString()
            If Not IsDBNull(reader("emp_Dept")) Then user.DepartmentName = reader("emp_Dept").ToString()

            ' Rates 
            If Not IsDBNull(reader("BreakFastRate")) Then user.BreakfastRate = Convert.ToDecimal(reader("BreakFastRate"))
            If Not IsDBNull(reader("LunchRate")) Then user.LunchRate = Convert.ToDecimal(reader("LunchRate"))
            If Not IsDBNull(reader("DinnerRate")) Then user.DinnerRate = Convert.ToDecimal(reader("DinnerRate"))
            If Not IsDBNull(reader("OTMeal")) Then user.OTMealRate = Convert.ToDecimal(reader("OTMeal"))
            If Not IsDBNull(reader("TranspoRate")) Then user.TransportationRate = Convert.ToDecimal(reader("TranspoRate"))

            If Not IsDBNull(reader("Password")) Then user.Password = reader("Password").ToString()
            If Not IsDBNull(reader("Approver1")) Then user.Approver1Id = reader("Approver1").ToString()
            If Not IsDBNull(reader("Approver2")) Then user.Approver2Id = reader("Approver2").ToString()

            Try
                If reader.GetOrdinal("AlreadyLogin") >= 0 AndAlso Not IsDBNull(reader("AlreadyLogin")) Then
                    user.Status = reader("AlreadyLogin").ToString()
                End If
            Catch ex As IndexOutOfRangeException
                ' Column might not exist in all procedures mapped here
            End Try

            Return user
        End Function

    End Class
End Namespace
