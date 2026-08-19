Option Strict On

Imports System.Data.SqlClient
Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data.Sql

Namespace Global.ERSystem.Infrastructure.Data
    Public Class RejectActionRepository
        Implements IRejectActionRepository

        Private ReadOnly _settingsProvider As RegistryConnectionSettingsProvider
        Private ReadOnly _connectionFactory As SqlConnectionFactory

        Public Sub New()
            Me.New(New RegistryConnectionSettingsProvider(), New SqlConnectionFactory())
        End Sub

        Public Sub New(settingsProvider As RegistryConnectionSettingsProvider, connectionFactory As SqlConnectionFactory)
            If settingsProvider Is Nothing Then
                Throw New ArgumentNullException("settingsProvider")
            End If

            If connectionFactory Is Nothing Then
                Throw New ArgumentNullException("connectionFactory")
            End If

            _settingsProvider = settingsProvider
            _connectionFactory = connectionFactory
        End Sub

        Public Sub RejectFiledReport(reportId As String, rejectNote As String, managerUserId As String) Implements IRejectActionRepository.RejectFiledReport
            Dim settings As ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
                Using command As New SqlCommand("[sp2_LoadUserReportDetailsCancel]", connection)
                    command.CommandType = CommandType.StoredProcedure
                    command.Parameters.Add("@reportID", SqlDbType.VarChar, 50).Value = reportId
                    command.Parameters.Add("@reportCancelNote", SqlDbType.VarChar).Value = If(rejectNote, String.Empty)
                    command.Parameters.Add("@SignID", SqlDbType.Int).Value = Convert.ToInt32(managerUserId)
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class
End Namespace
