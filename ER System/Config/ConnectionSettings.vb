Imports System.Data.SqlClient

Namespace Infrastructure.Configuration
    Public Class ConnectionSettings
        Public Const MicrosoftAccessDatabaseType As String = "Microsoft Access"
        Public Const LegacySqlServerDatabaseType As String = "Miscrosoft SQL Server"
        Public Const SqlServerDatabaseType As String = "Microsoft SQL Server"
        Public Const WindowsAuthentication As String = "Windows Authentication"

        Public Property DatabaseType As String
        Public Property Authentication As String
        Public Property ServerName As String
        Public Property DatabaseName As String
        Public Property UserName As String
        Public Property Password As String

        Public ReadOnly Property IsSqlServer As Boolean
            Get
                Return String.Equals(DatabaseType, LegacySqlServerDatabaseType, StringComparison.OrdinalIgnoreCase) OrElse
                    String.Equals(DatabaseType, SqlServerDatabaseType, StringComparison.OrdinalIgnoreCase)
            End Get
        End Property

        Public ReadOnly Property UsesWindowsAuthentication As Boolean
            Get
                Return String.Equals(Authentication, WindowsAuthentication, StringComparison.OrdinalIgnoreCase)
            End Get
        End Property

        Public Function BuildSqlConnectionString() As String
            Return BuildSqlConnectionString(DatabaseName, False)
        End Function

        Public Function BuildSqlConnectionString(ByVal databaseOverride As String, ByVal forceSqlAuthentication As Boolean) As String
            Dim builder As New SqlConnectionStringBuilder()

            builder.DataSource = ServerName
            builder.InitialCatalog = databaseOverride

            If UsesWindowsAuthentication AndAlso Not forceSqlAuthentication Then
                builder.IntegratedSecurity = True
            Else
                builder.IntegratedSecurity = False
                builder.UserID = UserName
                builder.Password = Password
            End If

            Return builder.ConnectionString
        End Function
    End Class
End Namespace
