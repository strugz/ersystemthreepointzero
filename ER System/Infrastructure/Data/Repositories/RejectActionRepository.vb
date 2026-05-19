Option Strict On

Imports System.Data.SqlClient

Namespace Infrastructure.Data.Repositories
    Friend Class RejectActionRepository
        Private ReadOnly _settingsProvider As Infrastructure.Configuration.RegistryConnectionSettingsProvider
        Private ReadOnly _connectionFactory As Infrastructure.Data.Sql.SqlConnectionFactory

        Public Sub New()
            _settingsProvider = New Infrastructure.Configuration.RegistryConnectionSettingsProvider()
            _connectionFactory = New Infrastructure.Data.Sql.SqlConnectionFactory()
        End Sub

        Public Sub RejectFiledReport(reportId As String, rejectNote As String)
            Dim settings As Infrastructure.Configuration.ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
            Using command As New SqlCommand("[sp2_LoadUserReportDetailsCancel]", connection)
                command.Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportId
                command.Parameters.AddWithValue("@reportCancelNote", rejectNote).SqlDbType = SqlDbType.VarChar
                command.CommandType = CommandType.StoredProcedure
                command.ExecuteNonQuery()
            End Using
            End Using
        End Sub
    End Class
End Namespace
