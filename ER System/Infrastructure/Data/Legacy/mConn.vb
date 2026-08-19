Imports System.Security.Cryptography
Imports Microsoft.Win32
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
        Dim settingsProvider As New Infrastructure.Configuration.RegistryConnectionSettingsProvider()
        Dim connectionFactory As New Infrastructure.Data.Sql.SqlConnectionFactory()
        Dim settings As Infrastructure.Configuration.ConnectionSettings = settingsProvider.Load()

        Select Case settings.DbType
            Case [Shared].Utilities.RegistryKeys.DefaultDbType
            Case [Shared].Utilities.RegistryKeys.SqlServerDbType
                ActiveDBType = "MSSQL"
                cnString = connectionFactory.CreateConnectionString(settings)

                Try
                    SQLConnection = connectionFactory.CreateOpenConnection(settings)
                    IsConnected = True
                Catch ex As Exception
                    SQLConnection = New SqlClient.SqlConnection(cnString)
                    IsConnected = False
                End Try
            Case "MYSQL"
            Case "Odbc"
        End Select
    End Sub
    Public Sub ConnectionPreviousER()
        Dim settingsProvider As New Infrastructure.Configuration.RegistryConnectionSettingsProvider()
        Dim connectionFactory As New Infrastructure.Data.Sql.SqlConnectionFactory()
        Dim settings As Infrastructure.Configuration.ConnectionSettings = settingsProvider.Load()

        connectString = connectionFactory.CreateConnectionString(
            New Infrastructure.Configuration.ConnectionSettings With {
                .DbType = settings.DbType,
                .Authentication = settings.Authentication,
                .ServerName = settings.ServerName,
                .Database = [Shared].Utilities.RegistryKeys.PreviousExpenseDatabase,
                .UserName = settings.UserName,
                .Password = settings.Password
            })

        conn = connectionFactory.CreatePreviousExpenseOpenConnection(settings)

    End Sub
End Module
