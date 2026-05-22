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

        Public Function IsReportApprovedDone(reportId As String) As Boolean
            If String.IsNullOrWhiteSpace(reportId) Then
                Return False
            End If

            Dim settings As Infrastructure.Configuration.ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
            Using command As New SqlCommand("SELECT COUNT(1) FROM tbReportDetails WHERE ID = @ReportID AND ReportFileStatus = '0' AND ReportPrintStatus = '0'", connection)
                command.CommandType = CommandType.Text
                command.Parameters.Add("@ReportID", SqlDbType.VarChar, 50).Value = reportId
                Return Convert.ToInt32(command.ExecuteScalar()) > 0
            End Using
            End Using
        End Function
    End Class
End Namespace
