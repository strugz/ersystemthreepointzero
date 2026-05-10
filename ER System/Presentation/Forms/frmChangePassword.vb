Imports Microsoft.Win32
Public Class frmChangePassword
    Private Const MyKey As String = "crimsonmonastery2003"
    Private ReadOnly TripleDes As New clsEncryption(MyKey)
    Private Sub btnChange_Click(sender As Object, e As EventArgs) Handles btnChange.Click
        If txtNew.Text = txtCon.Text Then
            ChangePassword(GetRegistryValue(RegistryKeys.UserAccountPath, {RegistryKeys.UserID})(0),
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
        Using dtLoadUserAccountEmail As DataTable = LoadingUserAccountEmail(
                GetRegistryValue(RegistryKeys.UserAccountPath, {RegistryKeys.UserID})(0),
                GetRegistryValue(RegistryKeys.UserAccountPath, {RegistryKeys.DeptID})(0))
            If dtLoadUserAccountEmail.Rows.Count <> 0 Then
                Dim encryptionService = New ERSystem.Common.Utilities.TripleDesEncryptionService("crimsonmonastery2003")
                txtEmailAdd.Text = encryptionService.DecryptData(dtLoadUserAccountEmail.Rows(0).Item("EmailAdd"))
                txtEmailPass.Text = encryptionService.DecryptData(dtLoadUserAccountEmail.Rows(0).Item("EmailPass"))
                txtEmailTo.Text = dtLoadUserAccountEmail.Rows(0).Item("EmailTo")
                txtBcc.Text = dtLoadUserAccountEmail.Rows(0).Item("EmailBCC")
            End If
        End Using
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
        Try
            Dim encryptionService = New ERSystem.Common.Utilities.TripleDesEncryptionService("crimsonmonastery2003")
            UpdateEmailSetup(GetRegistryValue(RegistryKeys.UserAccountPath, {RegistryKeys.UserID})(0),
                             encryptionService.EncryptData(txtEmailAdd.Text), encryptionService.EncryptData(txtEmailPass.Text),
                             txtEmailTo.Text, txtBcc.Text)
            MsgBox("Successfully Update" + vbNewLine + "Application Need to close ....")
            System.Windows.Forms.Application.Exit()
        Catch ex As Exception
            MessageBox.Show("Failed to update email settings: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class