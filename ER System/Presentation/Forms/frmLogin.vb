Imports System.Net.NetworkInformation
Imports System.Threading
Imports ER_System.Application.Services
Imports ER_System.Domain.Entities

Public Class frmLogin
    Private Shared ReadOnly StartupPath As String = System.Windows.Forms.Application.StartupPath
    Private Const MyKey As String = "crimsonmonastery2003"
    Private TripleDes As New clsEncryption(MyKey)

    Private Sub frmLogin_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then System.Windows.Forms.Application.Exit()
    End Sub

    Private Sub frmLogin_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName)

        Try
            EnsureDatabaseConnection()
            LoadStartupData()
        Catch ex As Exception
            MessageBox.Show("Failed to initialize connection: " & ex.Message, "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        CheckApplicationVersion()
    End Sub

    Private Sub EnsureDatabaseConnection()
        DBConnection()

        If IsConnected Then
            Return
        End If

        Me.Visible = False
        frmConnection.ShowDialog()
        DBConnection()

        If Not IsConnected Then
            End
        End If

        Me.Visible = True
    End Sub

    Private Sub LoadStartupData()
        LoadUserAccountAdmin()
    End Sub

    Private Sub CheckApplicationVersion()
        Dim CurrentVersion As String
        Dim NewVersion As String
        Dim MainExeCurrentVersion As String
        Dim MainExeNewVersion As String

        CurrentVersion = GetFileVersionInfo(StartupPath + "\ER.exe").ToString()
        NewVersion = GetFileVersionInfo(StartupPath + "\Executable\ER.exe").ToString()
        MainExeCurrentVersion = GetFileVersionInfo(StartupPath + "\ER System.exe").ToString()
        MainExeNewVersion = modLoadingData.SearchVersion()

        If Pinging("192.168.4.96").Status <> IPStatus.Success Then
            If MainExeCurrentVersion <> MainExeNewVersion Then
                MsgBox("Please Update your Expense Report System.")
            End If
        Else
            If CurrentVersion <> NewVersion Then
                If (Not IO.File.Exists(StartupPath + "\Executable")) Then
                    Thread.Sleep(300)
                    IO.File.Delete(StartupPath + "\ER.exe")
                    IO.File.Copy(StartupPath + "\Executable\ER.exe", StartupPath + "\ER.exe")

                    My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ERUpdater", "1")
                    If (Not IO.Directory.Exists(StartupPath + "\ERPDF")) Then
                        IO.Directory.CreateDirectory(StartupPath + "\ERPDF")
                    End If
                    MsgBox("Application Updated. The Application will be close . . . .")
                    System.Windows.Forms.Application.Exit()
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
    Private _userService As ERSystem.Core.Application.Interfaces.IUserService

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeServices()
        If _userService Is Nothing Then
            Dim connectionString As String = mConn.GetOpenSqlConnection().ConnectionString
            Dim userRepository = New ERSystem.Data.Repositories.SqlUserRepository(connectionString)
            Dim encryptionService = New ERSystem.Common.Utilities.TripleDesEncryptionService("crimsonmonastery2003")
            _userService = New ERSystem.Core.Application.Services.UserService(userRepository, encryptionService)
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
        InitializeServices()
        If txtUsername.Text.Length <> 0 Or txtPassword.Text.Length <> 0 Then

            ' Delegate authentication to Application layer service
            Dim userAccount = _userService.Authenticate(UCase(txtUsername.Text), txtPassword.Text)

            If userAccount IsNot Nothing Then
                ' Migrate setting registries to use the Domain entity instead of DataTable
                SetRegistryValueFromDomain(userAccount)
                LoadUserAccount(userAccount)
                Call ReleasMemory()
            Else
                MsgBox("Username " & txtUsername.Text & " Not Detected or Invalid Password")
            End If
        Else
            MsgBox("Please Fill Your Username/Password")
            txtUsername.Focus()
        End If
    End Sub

    Private Sub SetRegistryValueFromDomain(ByVal user As ERSystem.Core.Domain.Entities.UserAccount)
        ' Backward compatibility wrapper so we don't have to change all the forms that read this registry right away
        Dim ValueName As String() = {"UserID", "username", "Userlevel", "DeptID", "Fullname", "emp_Dept", "BreakFastRate",
            "LunchRate", "DinnerRate", "OTMeal", "TranspoRate", "Password", "Approver1", "Approver2"}
        Dim Value As String() = {user.UserID, user.Username, user.UserLevel, user.DepartmentID, user.Fullname, user.DepartmentName, user.BreakfastRate.ToString(), user.LunchRate.ToString(), user.DinnerRate.ToString(), user.OTMealRate.ToString(), user.TransportationRate.ToString(), user.Password, user.Approver1Id, user.Approver2Id}

        For a As Integer = 0 To ValueName.Length - 1
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\UserAccount", ValueName(a), Value(a))
        Next
    End Sub
    Private Sub LoadUserAccount(account As ERSystem.Core.Domain.Entities.UserAccount)
        If account IsNot Nothing Then
            ' Map to the existing logic access pipeline (if you intend to migrate LoginAccessService, you can do it here) 
            ' As an adapter step, map our new Domain.Entities.UserAccount to the old schema.
            Dim oldAcc = New UserAccount With {
                .UserName = account.Username,
                .UserLevel = account.UserLevel,
                .FullName = account.Fullname,
                .DepartmentName = account.DepartmentName
            }
            Dim loginAccess As LoginAccessResult = New LoginAccessService().Resolve(oldAcc, UCase(txtUsername.Text))

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
        End If
    End Sub
    Private Sub SearchDup()
        _userService.GetUserDetails(GetRegistryValue(RegistryKeys.UserAccountPath, {RegistryKeys.UserID})(0))
    End Sub

    Private Sub DUpAcct(ByVal loginStatus As String)
        _userService.UpdateLoginStatus(GetRegistryValue(RegistryKeys.UserAccountPath, {RegistryKeys.UserID})(0), loginStatus)
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        System.Windows.Forms.Application.Exit()
    End Sub
End Class