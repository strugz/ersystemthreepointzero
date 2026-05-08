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
        cmbboxDataSource.SelectedIndex = 1
        Try
            If My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "DBType", "Microsoft Access") = "" Then
                MsgBox("No Connection please Click Save")
            Else

                cmbboxDataSource.Text = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "DBType", "Microsoft Access")
                txtboxServerName.Text = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ServerName", ""))
                If My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Authentication", _
                                                    "Windows Authentication") = "Windows Authentication" Then
                    rdbtnLogOnDbWin.Checked = True
                    rdbtnLogOnDbSQL.Checked = False
                Else
                    rdbtnLogOnDbWin.Checked = False
                    rdbtnLogOnDbSQL.Checked = True
                    txtboxUserName.Text = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
                    txtboxPassword.Text = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))
                End If
                txtboxDatabase.Text = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Database", ""))

            End If

        Catch ex As Exception
        End Try
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If TestConnection(False) = "1" Then
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "DBType", cmbboxDataSource.Text)
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ServerName", TripleDes.EncryptData(txtboxServerName.Text))
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Authentication", _
                                            IIf(rdbtnLogOnDbWin.Checked, rdbtnLogOnDbWin.Text, rdbtnLogOnDbSQL.Text))
            If rdbtnLogOnDbSQL.Checked Then
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", TripleDes.EncryptData(txtboxUserName.Text))
                My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", TripleDes.EncryptData(txtboxPassword.Text))
            End If
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Database", TripleDes.EncryptData(txtboxDatabase.Text))
            Me.Close()
            MsgBox("Connected")
        End If
    End Sub

    Private Function TestConnection(ByVal isTesting As Boolean) As String
        Dim strToReturn As String = "0"

        Select Case cmbboxDataSource.Text
            Case "Microsoft Access"
            Case "Miscrosoft SQL Server"
                Dim TestCon As New SqlClient.SqlConnection
                If rdbtnLogOnDbWin.Checked = True Then
                    cnString = "Data Source=" & txtboxServerName.Text & ";Integrated Security=TRUE" & ";Database=" & txtboxDatabase.Text
                ElseIf rdbtnLogOnDbSQL.Checked = True Then
                    cnString = "Data Source=" & txtboxServerName.Text & ";Integrated Security=FALSE" & ";UID=" & Trim(txtboxUserName.Text) & ";PWD=" & Trim(txtboxPassword.Text) & ";Database=" & txtboxDatabase.Text
                End If
                TestCon.ConnectionString = cnString

                Try
                    TestCon.Open()
                    If isTesting Then
                        If TestCon.State = 1 Then
                            MsgBox("Test Connection Succeeded")
                        End If
                    End If
                    strToReturn = "1"
                Catch ex As Exception
                    MsgBox(ex.Message)
                    strToReturn = "0"
                End Try
            Case "MYSQL"
            Case "Odbc"
        End Select

        Return strToReturn

    End Function

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles btnTest.Click
        TestConnection(True)
    End Sub
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub

    Private Sub rdbtnLogOnDbWin_CheckedChanged(sender As Object, e As EventArgs) Handles rdbtnLogOnDbWin.CheckedChanged
        txtboxUserName.Enabled = False
        txtboxPassword.Enabled = False
    End Sub
    Private Sub rdbtnLogOnDbSQL_CheckedChanged(sender As Object, e As EventArgs) Handles rdbtnLogOnDbSQL.CheckedChanged
        txtboxUserName.Enabled = True
        txtboxPassword.Enabled = True
    End Sub
End Class