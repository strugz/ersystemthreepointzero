Imports System.Net.NetworkInformation
Imports System.Threading
Imports ER_System.Application.Repositories
Imports ER_System.Application.Services
Imports ER_System.Domain.Entities
Imports ER_System.Infrastructure.Configuration
Imports ER_System.Infrastructure.Data.Repositories
Imports ER_System.Infrastructure.Data.Sql

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
            Using dtLoginUserAccount As DataTable = CreateUserAccountService().Authenticate(UCase(txtUsername.Text), TripleDes.EncryptData(txtPassword.Text))
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
        Dim loginAccess As LoginAccessResult = New LoginAccessService().Resolve(CreateRegisteredUserAccount(), UCase(txtUsername.Text))

        If loginAccess.IsAllowed Then
            Me.Hide()
            frmMain.ttuser.Text = loginAccess.DisplayName
            frmMain.tsslUserDept.Text = loginAccess.DepartmentName
            LoginSettingsControl(
                loginAccess.MenuFormsVisible,
                loginAccess.MenuFileVisible,
                loginAccess.MainFormEnabled,
                loginAccess.PreviousReportsVisible,
                loginAccess.UserAccountVisible,
                loginAccess.ExpenseSummaryVisible)
        Else
            MsgBox("Invalid Username/Password")
            txtPassword.Clear()
            txtPassword.Focus()
        End If
    End Sub
    Private Sub SearchDup()
        CreateUserAccountService().GetByUserId(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
    End Sub

    Private Sub DUpAcct(ByVal loginStatus As String)
        CreateUserAccountService().UpdateLoginStatus(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), loginStatus)
    End Sub

    Private Function CreateUserAccountService() As UserAccountService
        Dim settingsProvider As New RegistryConnectionSettingsProvider(TripleDes)
        Dim connectionFactory As New SqlConnectionFactory(settingsProvider)
        Dim userAccountRepository As IUserAccountRepository = New SqlUserAccountRepository(connectionFactory)

        Return New UserAccountService(userAccountRepository)
    End Function

    Private Function CreateRegisteredUserAccount() As UserAccount
        Return New UserAccount With {
            .UserName = GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0),
            .UserLevel = GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0),
            .FullName = GetRegistryValue("Software\\ER System\\UserAccount", {"Fullname"})(0),
            .DepartmentName = GetRegistryValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0)
        }
    End Function
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Application.Exit()
    End Sub
End Class