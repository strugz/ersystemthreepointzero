Imports System.Net.NetworkInformation
Public Class frmLogin
    Private ReadOnly _userAccountService As New AppServices.UserAccountService()
    Private Sub frmLogin_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then Application.Exit()
    End Sub
    Private Sub frmLogin_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Process.GetProcessesByName(Process.GetCurrentProcess.ProcessName)
        Try
            Dim connectionResult As AppServices.StartupConnectionResult = _userAccountService.EnsureConnection()

            If connectionResult.RequiresConnectionSetup Then
                Me.Visible = False
                frmConnection.ShowDialog()
                connectionResult = _userAccountService.FinalizeConnectionSetup()

                If connectionResult.ShouldExitApplication Then
                    End
                End If
            End If

            Dim adminCheckResult As AppServices.StartupAdminCheckResult = _userAccountService.CheckAdminAccounts()

            If adminCheckResult.RequiresDepartmentSelection Then
                frmSelectDept.ShowDialog()
            End If
        Catch ex As Exception
        End Try
        Dim MainExeCurrentVersion As String
        Dim MainExeNewVersion As String
        MainExeCurrentVersion = GetFileVersionInfo(Application.StartupPath + "\ER System.exe").ToString()
        MainExeNewVersion = modLoadingData.SearchVersion()
        If Pinging("192.168.4.96").Status <> IPStatus.Success Then
            If MainExeCurrentVersion <> MainExeNewVersion Then
                MsgBox("Please Update your Expense Report System.")
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
        Dim result As AppServices.LoginResult = _userAccountService.Login(
            New AppServices.LoginRequest With {
                .UserName = txtUsername.Text,
                .Password = txtPassword.Text
            })

        If result.IsSuccess Then
            ApplyLoginResult(result)
            Call ReleasMemory()
        Else
            MsgBox(result.Message)

            If String.Equals(result.Message, "Please Fill Your Username/Password", StringComparison.Ordinal) Then
                txtUsername.Focus()
            Else
                txtPassword.Clear()
                txtPassword.Focus()
            End If
        End If
    End Sub
    Private Sub ApplyLoginResult(result As AppServices.LoginResult)
        Me.Hide()
        frmMain.ttuser.Text = result.FullName
        frmMain.tsslUserDept.Text = result.Department
        LoginSettingsControl(result.ShowMenuForms,
                             result.ShowMenuFile,
                             result.EnableMainForm,
                             result.ShowPreviousReports,
                             result.ShowUserAccountMenu,
                             result.ShowExpenseSummary)
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
