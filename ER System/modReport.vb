Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports Microsoft.Win32
Module modReport
    'Public conn As New SqlClient.SqlConnection
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Public smpleID As String
    Private Declare Function SQLConfigDataSource Lib "ODBCCP32.DLL" _
    (ByVal hwndParent As Integer, ByVal ByValfRequest As Integer,
    ByVal lpszDriver As String, ByVal lpszAttributes As String) As Integer
    Private Const vbAPINull As Integer = 0
    Private Const ODBC_ADD_DSN As Short = 1
    Public tempServerName As String
    Public tempDSNName As String
    Public tempDB As String

    Public Function CreateUserDSN()
        DBConnection()
        Using SQLConnection As SqlConnection = mConn.SQLConnection
            tempServerName = SQLConnection.DataSource.ToString()
            tempDSNName = SQLConnection.Database.ToString
            tempDB = SQLConnection.Database.ToString
        End Using

        On Error Resume Next
        Dim Driver As String
        Dim Attributes As String

        Driver = "SQL Server"
        Attributes = "SERVER=" & tempServerName & Chr(0)
        Attributes = Attributes & "DESCRIPTION=" & Chr(0)
        Attributes = Attributes & "DSN=" & tempDSNName & Chr(0)
        Attributes = Attributes & "DATABASE=" & tempDB & Chr(0)


        SQLConfigDataSource(vbAPINull, ODBC_ADD_DSN, Driver, Attributes)
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER system\DSN", "Name", tempDSNName)
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER system\DSN", "Server", TripleDes.EncryptData(tempServerName))
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER system\DSN", "Database", tempDB)

    End Function
End Module
