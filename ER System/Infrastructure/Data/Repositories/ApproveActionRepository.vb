Option Strict On

Imports System.Data.SqlClient

Namespace Infrastructure.Data.Repositories
    Friend Class ApproveActionRepository
        Private ReadOnly _settingsProvider As Infrastructure.Configuration.RegistryConnectionSettingsProvider
        Private ReadOnly _connectionFactory As Infrastructure.Data.Sql.SqlConnectionFactory

        Public Sub New()
            _settingsProvider = New Infrastructure.Configuration.RegistryConnectionSettingsProvider()
            _connectionFactory = New Infrastructure.Data.Sql.SqlConnectionFactory()
        End Sub

        Public Sub UpdateFileStatus(userIdToApprover As String, reportIdToApprove As String, loginUserId As String)
            Dim settings As Infrastructure.Configuration.ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
            Using command As New SqlCommand("sp2_UpdateReportNumberStatus '" & userIdToApprover & "','" & reportIdToApprove & "','" & loginUserId & "'", connection)
                command.CommandType = CommandType.Text
                command.ExecuteNonQuery()
            End Using
            End Using
        End Sub
    End Class
End Namespace
