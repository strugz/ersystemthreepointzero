Imports System.Data.SqlClient
Imports ER_System.Infrastructure.Configuration

Namespace Infrastructure.Data.Sql
    Public Class SqlConnectionFactory
        Private ReadOnly _settingsProvider As RegistryConnectionSettingsProvider

        Public Sub New(ByVal settingsProvider As RegistryConnectionSettingsProvider)
            _settingsProvider = settingsProvider
        End Sub

        Public Function CreateCurrentConnection() As SqlConnection
            Dim settings As ConnectionSettings = _settingsProvider.Load()
            Return New SqlConnection(settings.BuildSqlConnectionString())
        End Function

        Public Function CreatePreviousExpenseReportConnection() As SqlConnection
            Return CreateCurrentConnection()
        End Function
    End Class
End Namespace
