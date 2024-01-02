Imports System.Security.Cryptography
Imports Microsoft.Win32
Public Class frmChangePassword
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Private Sub btnChange_Click(sender As Object, e As EventArgs) Handles btnChange.Click
        If txtNew.Text = txtCon.Text Then
            ChangePassword(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0),
                           TripleDes.EncryptData(txtNew.Text))
            MsgBox("Change Successfully")
            Me.Close()
        Else
            MsgBox("Your Password is not match")
        End If
    End Sub
    Private Sub frmChangePassword_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Me.KeyPreview = True
        If e.KeyCode = Keys.Escape Then
            Me.Close()

        End If
    End Sub
    Private Sub frmChangePassword_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Using dtLoadUserAccountEmail As DataTable = LoadingUserAccountEmail(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0))
            If dtLoadUserAccountEmail.Rows.Count <> 0 Then
                txtEmailAdd.Text = TripleDes.DecryptData(dtLoadUserAccountEmail.Rows(0).Item("EmailAdd"))
                txtEmailPass.Text = TripleDes.DecryptData(dtLoadUserAccountEmail.Rows(0).Item("EmailPass"))
                txtEmailTo.Text = dtLoadUserAccountEmail.Rows(0).Item("EmailTo")
                txtBcc.Text = dtLoadUserAccountEmail.Rows(0).Item("EmailBCC")
            End If
        End Using
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
        Try
            UpdateEmailSetup(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0),
                             TripleDes.EncryptData(txtEmailAdd.Text), TripleDes.EncryptData(txtEmailPass.Text),
                             txtEmailTo.Text, txtBcc.Text)
            MsgBox("Successfully Update" + vbNewLine + "Application Need to close ....")
            Application.Exit()
        Catch ex As Exception
        End Try
    End Sub
End Class