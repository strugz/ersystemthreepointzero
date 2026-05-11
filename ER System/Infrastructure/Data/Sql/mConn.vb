Imports ER_System.Infrastructure.Configuration
Imports ER_System.Infrastructure.Data.Sql

Module mConn
    Public LocalSQLConnection As SqlClient.SqlConnection
    Public MyConnection As Object
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Public SQLConnection As SqlClient.SqlConnection
    Public cnString As String
    Public connectString As String
    Public ActiveDBType As String
    Public IsConnected As Boolean
    Public ExtDBConnection As Object
    Public objIntegration As Object
    Public currentDate As String
    Private strLogs As String
    Public objRatesSettings As Object
    Public isConnectedPrevious As Boolean
    Public conn As New SqlClient.SqlConnection

    Public Function GetOpenSqlConnection() As SqlClient.SqlConnection
        If SQLConnection Is Nothing OrElse String.IsNullOrWhiteSpace(SQLConnection.ConnectionString) Then
            DBConnection()
        End If

        If SQLConnection Is Nothing Then
            Throw New InvalidOperationException("SQL connection could not be initialized.")
        End If

        If SQLConnection.State <> ConnectionState.Open Then
            SQLConnection.Open()
        End If

        Return SQLConnection
    End Function

    Public Sub DBConnection()
        Dim settingsProvider As New RegistryConnectionSettingsProvider(TripleDes)
        Dim settings As ConnectionSettings = settingsProvider.Load()

        SQLConnection = Nothing
        cnString = String.Empty
        IsConnected = False

        Select Case settings.DatabaseType
            Case ConnectionSettings.MicrosoftAccessDatabaseType
            Case ConnectionSettings.LegacySqlServerDatabaseType, ConnectionSettings.SqlServerDatabaseType
                If String.IsNullOrWhiteSpace(settings.ServerName) OrElse
                    String.IsNullOrWhiteSpace(settings.DatabaseName) OrElse
                    String.IsNullOrWhiteSpace(settings.UserName) OrElse
                    String.IsNullOrWhiteSpace(settings.Password) Then
                    Exit Sub
                End If

                Dim connectionFactory As New SqlConnectionFactory(settingsProvider)

                SQLConnection = connectionFactory.CreateCurrentConnection()
                ActiveDBType = "MSSQL"
                cnString = SQLConnection.ConnectionString

                Try
                    If SQLConnection.State <> ConnectionState.Open Then
                        SQLConnection.Open()
                    End If
                    IsConnected = True
                Catch ex As Exception
                    IsConnected = False
                End Try
            Case "MYSQL"
            Case "Odbc"
        End Select
    End Sub

    Public Sub ConnectionPreviousER()
        conn = GetOpenSqlConnection()
        connectString = conn.ConnectionString
    End Sub
End Module
