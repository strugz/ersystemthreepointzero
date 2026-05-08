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
    Public Sub DBConnection()
        Dim settingsProvider As New RegistryConnectionSettingsProvider(TripleDes)
        Dim settings As ConnectionSettings = settingsProvider.Load()

        Select Case settings.DatabaseType
            Case ConnectionSettings.MicrosoftAccessDatabaseType
            Case ConnectionSettings.LegacySqlServerDatabaseType, ConnectionSettings.SqlServerDatabaseType
                Dim connectionFactory As New SqlConnectionFactory(settingsProvider)

                SQLConnection = connectionFactory.CreateCurrentConnection()
                ActiveDBType = "MSSQL"
                cnString = SQLConnection.ConnectionString

                Try
                    SQLConnection.Open()
                    IsConnected = True
                Catch ex As Exception
                    IsConnected = False
                End Try
            Case "MYSQL"
            Case "Odbc"
        End Select
    End Sub

    Public Sub ConnectionPreviousER()
        Dim settingsProvider As New RegistryConnectionSettingsProvider(TripleDes)
        Dim connectionFactory As New SqlConnectionFactory(settingsProvider)

        conn = connectionFactory.CreatePreviousExpenseReportConnection()
        connectString = conn.ConnectionString
        conn.Open()
    End Sub
End Module
