Imports System.Net.NetworkInformation
Imports System.Threading
Public Class frmLogin
    Private Const MyKey As String = "crimsonmonastery2003"
    Private TripleDes As New clsEncryption(MyKey)
    Private Sub frmLogin_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then Application.Exit()
    End Sub
    Private Sub frmLogin_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName)
        Try
            DBConnection()
            If Not IsConnected Then
                Me.Visible = False
                frmConnection.ShowDialog()
                DBConnection()
                If Not IsConnected Then
                    End
                End If
            End If
            LoadUserAccountAdmin()
        Catch ex As Exception
        End Try
        Dim CurrentVersion As String
        Dim NewVersion As String
        Dim MainExeCurrentVersion As String
        Dim MainExeNewVersion As String
        CurrentVersion = GetFileVersionInfo(Application.StartupPath + "\ER.exe").ToString()
        NewVersion = GetFileVersionInfo(Application.StartupPath + "\Executable\ER.exe").ToString()
        MainExeCurrentVersion = GetFileVersionInfo(Application.StartupPath + "\ER System.exe").ToString()
        MainExeNewVersion = modLoadingData.SearchVersion()
        If Pinging("192.168.4.96").Status <> IPStatus.Success Then
            If MainExeCurrentVersion <> MainExeNewVersion Then
                MsgBox("Please Update your Expense Report System.")
            End If
        Else
            If CurrentVersion <> NewVersion Then
                If (Not IO.File.Exists(Application.StartupPath + "\Executable")) Then
                    Thread.Sleep(300)
                    IO.File.Delete(Application.StartupPath + "\ER.exe")
                    IO.File.Copy(Application.StartupPath + "\Executable\ER.exe", Application.StartupPath + "\ER.exe")

                    My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ERUpdater", "1")
                    If (Not IO.Directory.Exists(Application.StartupPath + "\ERPDF")) Then
                        IO.Directory.CreateDirectory(Application.StartupPath + "\ERPDF")
                    End If
                    MsgBox("Application Updated. The Application will be close . . . .")
                    Application.Exit()
                End If
            End If
        End If
    End Sub
    Public Function Pinging(ByVal path) As PingReply
        Dim ping As New Ping
        Dim pingreply As PingReply = Nothing
        Try
            pingreply = ping.Send(path)
        Catch ex As Exception
            MessageBox.Show("Contact Jay")
        End Try
        Return pingreply
    End Function
    Private Function GetFileVersionInfo(ByVal filename As String) As String
        GetFileVersionInfo = FileVersionInfo.GetVersionInfo(filename).FileVersion
        Return GetFileVersionInfo
    End Function
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
        If txtUsername.Text.Length <> 0 Or txtPassword.Text.Length <> 0 Then
            Using dtLoginUserAccount As DataTable = LoginUserAccount(UCase(txtUsername.Text), TripleDes.EncryptData(txtPassword.Text))
                If dtLoginUserAccount.Rows.Count <> 0 Then
                    SetRegistryValue(dtLoginUserAccount)
                    LoadUserAccount()
                    Call ReleasMemory()
                Else
                    MsgBox("Username " & txtUsername.Text & " Not Detected")
                End If
            End Using
        Else
            MsgBox("Please Fill Your Username/Password")
            txtUsername.Focus()
        End If
    End Sub
    Private Sub LoadUserAccount()
        If GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0) <> "IMS" And
            GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) = UCase(txtUsername.Text) And
             GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0) = "Admin" Then
            Me.Hide()
            frmMain.ttuser.Text = LTrim(GetRegistryValue("Software\\ER System\\UserAccount", {"Fullname"})(0)).Replace(vbCrLf, "")
            frmMain.tsslUserDept.Text = LTrim(GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0)).Replace(vbCrLf, "")
            LoginSettingsControl(True, True, True, False, False, False)
        ElseIf GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0) = "IMS" And
            GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) = UCase(txtUsername.Text) And
            GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0) = "Admin" Then
            Me.Hide()
            frmMain.ttuser.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"Fullname"})(0).Replace(vbCrLf, "")
            frmMain.tsslUserDept.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0).Replace(vbCrLf, "")
            LoginSettingsControl(True, True, True, True, True, True)
        ElseIf GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0) = "IMS" And
            GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) = UCase(txtUsername.Text) And
            GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0) = "User" Then
            Me.Hide()
            frmMain.ttuser.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"Fullname"})(0).Replace(vbCrLf, "")
            frmMain.tsslUserDept.Text = LTrim(GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0)).Replace(vbCrLf, "")
            LoginSettingsControl(False, True, True, True, False, False)
        ElseIf GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0) <> "IMS" And
            GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) = UCase(txtUsername.Text) And
            GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0) = "User" Then
            Me.Hide()
            frmMain.ttuser.Text = LTrim(GetRegistryValue("Software\\ER System\\UserAccount", {"Fullname"})(0)).Replace(vbCrLf, "")
            frmMain.tsslUserDept.Text = LTrim(GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0)).Replace(vbCrLf, "")
            LoginSettingsControl(False, True, True, False, False, False)
        Else
            MsgBox("Invalid Username/Password")
            txtPassword.Clear()
            txtPassword.Focus()
        End If
    End Sub
    Private Sub SearchDup()
        DBConnection()
        Using dt As New DataTable
            Using sqlcmdSearchDup As New SqlClient.SqlCommand
                Using SQLConnection As SqlClient.SqlConnection = mConn.SQLConnection
                    With sqlcmdSearchDup
                        .Connection = SQLConnection
                        .CommandText = "Select a.[Status] from tbUserRegistration as a where UserID='" & GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0) & "'"
                        .CommandType = CommandType.Text
                        dt.Load(.ExecuteReader)
                        'If dt.Rows.Count <> 0 Then
                        '    loginSearchStatus = dt.Rows(0).Item("Status")
                        'End If
                    End With
                End Using
            End Using
        End Using
    End Sub
    Private Sub DUpAcct(ByVal loginStatus As String)
        DBConnection()
        Using sqlcmdDup As New SqlClient.SqlCommand
            Using SQLConnection As SqlClient.SqlConnection = mConn.SQLConnection
                With sqlcmdDup
                    .Connection = SQLConnection
                    .CommandText = "Update tbUserRegistration set [Status] = '" & loginStatus & "' where UserID = '" & GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0) & "'"
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Application.Exit()
    End Sub
End Class