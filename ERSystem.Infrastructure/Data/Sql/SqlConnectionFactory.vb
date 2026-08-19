Option Strict On

Imports System.Data.SqlClient
Imports ERSystem.Infrastructure.Configuration

Namespace Global.ERSystem.Infrastructure.Data.Sql
    Public Class SqlConnectionFactory
        Public Function CreateOpenConnection(settings As ConnectionSettings) As SqlConnection
            If settings Is Nothing Then
                Throw New ArgumentNullException("settings")
            End If

            Dim connection As New SqlConnection(CreateConnectionString(settings))
            connection.Open()
            Return connection
        End Function

        Public Function CreateConnectionString(settings As ConnectionSettings) As String
            If settings Is Nothing Then
                Throw New ArgumentNullException("settings")
            End If

            Dim builder As New SqlConnectionStringBuilder With {
                .DataSource = settings.ServerName,
                .InitialCatalog = settings.Database,
                .IntegratedSecurity = settings.UsesWindowsAuthentication
            }

            If Not settings.UsesWindowsAuthentication Then
                builder.UserID = settings.UserName
                builder.Password = settings.Password
            End If

            Return builder.ConnectionString
        End Function
    End Class
End Namespace
