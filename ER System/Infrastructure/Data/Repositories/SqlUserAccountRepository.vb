Option Strict On

Imports System.Data
Imports System.Data.SqlClient
Imports ER_System.Application.Repositories
Imports ER_System.Domain.Entities
Imports ER_System.Infrastructure.Data.Sql

Namespace Infrastructure.Data.Repositories
    Public Class SqlUserAccountRepository
        Implements IUserAccountRepository

        Private ReadOnly _connectionFactory As SqlConnectionFactory

        Public Sub New(ByVal connectionFactory As SqlConnectionFactory)
            _connectionFactory = connectionFactory
        End Sub

        Public Function Authenticate(ByVal username As String, ByVal encryptedPassword As String) As DataTable Implements IUserAccountRepository.Authenticate
            Dim dtLoginUser As New DataTable()

            Using connection As SqlConnection = _connectionFactory.CreateCurrentConnection()
                connection.Open()

                Using command As New SqlCommand("sp2_LoginUser", connection)
                    command.CommandType = CommandType.StoredProcedure
                    command.Parameters.Add("@username", SqlDbType.VarChar).Value = username
                    command.Parameters.Add("@password", SqlDbType.VarChar).Value = encryptedPassword

                    Using reader As SqlDataReader = command.ExecuteReader()
                        dtLoginUser.Load(reader)
                    End Using
                End Using
            End Using

            Return dtLoginUser
        End Function

        Public Function GetByUserId(ByVal userId As String) As UserAccount Implements IUserAccountRepository.GetByUserId
            Using connection As SqlConnection = _connectionFactory.CreateCurrentConnection()
                connection.Open()

                Using command As New SqlCommand("SELECT UserID, [Status] FROM tbUserRegistration WHERE UserID = @userId", connection)
                    command.CommandType = CommandType.Text
                    command.Parameters.Add("@userId", SqlDbType.VarChar).Value = userId

                    Using reader As SqlDataReader = command.ExecuteReader()
                        If Not reader.Read() Then
                            Return Nothing
                        End If

                        Return New UserAccount With {
                            .UserId = Convert.ToString(reader("UserID")),
                            .Status = Convert.ToString(reader("Status"))
                        }
                    End Using
                End Using
            End Using
        End Function

        Public Sub UpdateLoginStatus(ByVal userId As String, ByVal loginStatus As String) Implements IUserAccountRepository.UpdateLoginStatus
            Using connection As SqlConnection = _connectionFactory.CreateCurrentConnection()
                connection.Open()

                Using command As New SqlCommand("UPDATE tbUserRegistration SET [Status] = @loginStatus WHERE UserID = @userId", connection)
                    command.CommandType = CommandType.Text
                    command.Parameters.Add("@loginStatus", SqlDbType.VarChar).Value = loginStatus
                    command.Parameters.Add("@userId", SqlDbType.VarChar).Value = userId
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class
End Namespace
