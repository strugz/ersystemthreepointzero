Option Strict On

Imports System.Data.SqlClient

Namespace Infrastructure.Data.Repositories
    Friend Class UserAccountRepository
        Private ReadOnly _settingsProvider As Infrastructure.Configuration.RegistryConnectionSettingsProvider
        Private ReadOnly _connectionFactory As Infrastructure.Data.Sql.SqlConnectionFactory

        Public Sub New()
            _settingsProvider = New Infrastructure.Configuration.RegistryConnectionSettingsProvider()
            _connectionFactory = New Infrastructure.Data.Sql.SqlConnectionFactory()
        End Sub

        Public Function LoginUserAccount(userName As String, password As String) As DataTable
            Dim settings As Infrastructure.Configuration.ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
            Using command As New SqlCommand("sp2_LoginUser", connection)
            Using table As New DataTable()
                command.Parameters.Clear()
                command.Parameters.Add("@username", SqlDbType.VarChar).Value = userName
                command.Parameters.Add("@password", SqlDbType.VarChar).Value = password
                command.CommandType = CommandType.StoredProcedure
                table.Load(command.ExecuteReader())
                Return table.Copy()
            End Using
            End Using
            End Using
        End Function

        Public Function HasAdminAccounts() As Boolean
            Dim settings As Infrastructure.Configuration.ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
            Using command As New SqlCommand("sp2_LoadUserAccountAdmin", connection)
            Using table As New DataTable()
                command.CommandType = CommandType.StoredProcedure
                table.Load(command.ExecuteReader())
                Return table.Rows.Count <> 0
            End Using
            End Using
            End Using
        End Function
    End Class
End Namespace
