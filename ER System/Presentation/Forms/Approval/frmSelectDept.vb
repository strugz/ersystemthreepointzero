Imports System.Security.Cryptography
Imports Microsoft.Win32
Public Class frmSelectDept
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Dim dtDeptAddPassword As New DataTable

    Private Sub frmSelectDept_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Me.KeyPreview = True
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub
    Private Sub frmSelectDept_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadingDepartment()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        DeptAddPassword(cbbDept.SelectedValue.ToString, TripleDes.EncryptData(txtDeptPassword.Text))
        MsgBox("Admin Account Saved" & vbNewLine & "The Application need to close......")
        frmLogin.Close()
        Application.Exit()
    End Sub
End Class