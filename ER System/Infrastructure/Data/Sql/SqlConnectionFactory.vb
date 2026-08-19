Option Strict On

Imports System.Data.SqlClient

Namespace Infrastructure.Data.Sql
    Friend Class SqlConnectionFactory
        Public Function CreateOpenConnection(settings As Infrastructure.Configuration.ConnectionSettings) As SqlConnection
            If settings Is Nothing Then
                Throw New ArgumentNullException(NameOf(settings))
            End If

            Dim connection As New SqlConnection(CreateConnectionString(settings))
            connection.Open()
            Return connection
        End Function

        Public Function CreatePreviousExpenseOpenConnection(settings As Infrastructure.Configuration.ConnectionSettings) As SqlConnection
            If settings Is Nothing Then
                Throw New ArgumentNullException(NameOf(settings))
            End If

            Dim previousSettings As New Infrastructure.Configuration.ConnectionSettings With {
                .DbType = settings.DbType,
                .Authentication = settings.Authentication,
                .ServerName = settings.ServerName,
                .Database = [Shared].Utilities.RegistryKeys.PreviousExpenseDatabase,
                .UserName = settings.UserName,
                .Password = settings.Password
            }

            Return CreateOpenConnection(previousSettings)
        End Function

        Public Function CreateConnectionString(settings As Infrastructure.Configuration.ConnectionSettings) As String
            If settings Is Nothing Then
                Throw New ArgumentNullException(NameOf(settings))
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
