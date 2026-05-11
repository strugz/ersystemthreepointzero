Imports System.Security.Cryptography
Imports Microsoft.Win32

Public Class frmConnection
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Public SQLConnection As SqlClient.SqlConnection
    Public cnString As String
    Public ActiveDBType As String
    Public IsConnected As Boolean
    Public ExtDBConnection As Object
    Public objIntegration As Object
    Public currentDate As String
    Private strLogs As String
    Public objRatesSettings As Object

    Private Sub frmConnection_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        cmbboxDataSource.Text = "Microsoft SQL Server"
        cmbboxDataSource.Enabled = False

        Try
            If My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "DBType", "Microsoft Access") = "" Then
                MsgBox("No Connection please Click Save")
            Else
                txtboxServerName.Text = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ServerName", ""))
                rdbtnLogOnDbWin.Checked = False
                rdbtnLogOnDbSQL.Checked = True
                rdbtnLogOnDbWin.Enabled = False
                rdbtnLogOnDbSQL.Enabled = False
                txtboxUserName.Text = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
                txtboxPassword.Text = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))
                txtboxDatabase.Text = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Database", ""))
            End If

            txtboxUserName.Enabled = True
            txtboxPassword.Enabled = True
        Catch ex As Exception
            MessageBox.Show("Failed to load connection settings: " & ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If TestConnection(False) = "1" Then
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "DBType", "Microsoft SQL Server")
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ServerName", TripleDes.EncryptData(txtboxServerName.Text))
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Authentication", "SQL Server Authentication")
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", TripleDes.EncryptData(txtboxUserName.Text))
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", TripleDes.EncryptData(txtboxPassword.Text))
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Database", TripleDes.EncryptData(txtboxDatabase.Text))
            Me.Close()
            MsgBox("Connected")
        End If
    End Sub

    Private Function TestConnection(ByVal isTesting As Boolean) As String
        Dim strToReturn As String = "0"

        If String.IsNullOrWhiteSpace(txtboxServerName.Text) Then
            MsgBox("Please enter SQL Server name.")
            Return strToReturn
        End If

        If String.IsNullOrWhiteSpace(txtboxDatabase.Text) Then
            MsgBox("Please enter database name.")
            Return strToReturn
        End If

        If String.IsNullOrWhiteSpace(txtboxUserName.Text) OrElse String.IsNullOrWhiteSpace(txtboxPassword.Text) Then
            MsgBox("Please enter SQL Server username and password.")
            Return strToReturn
        End If

        Dim TestCon As New SqlClient.SqlConnection
        cnString = "Data Source=" & txtboxServerName.Text & ";Integrated Security=FALSE;UID=" & Trim(txtboxUserName.Text) & ";PWD=" & Trim(txtboxPassword.Text) & ";Database=" & txtboxDatabase.Text & ";TrustServerCertificate=True"
        TestCon.ConnectionString = cnString

        Try
            TestCon.Open()
            If isTesting AndAlso TestCon.State = 1 Then
                MsgBox("Test Connection Succeeded")
            End If
            strToReturn = "1"
        Catch ex As Exception
            MsgBox(ex.Message)
            strToReturn = "0"
        Finally
            If TestCon.State <> ConnectionState.Closed Then
                TestCon.Close()
            End If
        End Try

        Return strToReturn
    End Function

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles btnTest.Click
        TestConnection(True)
    End Sub
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub

    Private Sub rdbtnLogOnDbWin_CheckedChanged(sender As Object, e As EventArgs) Handles rdbtnLogOnDbWin.CheckedChanged
        txtboxUserName.Enabled = True
        txtboxPassword.Enabled = True
    End Sub
    Private Sub rdbtnLogOnDbSQL_CheckedChanged(sender As Object, e As EventArgs) Handles rdbtnLogOnDbSQL.CheckedChanged
        txtboxUserName.Enabled = True
        txtboxPassword.Enabled = True
    End Sub
End Class