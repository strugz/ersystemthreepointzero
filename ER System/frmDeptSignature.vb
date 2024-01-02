Public Class frmDeptSignature
    Dim dtDeptSIgn As New DataTable
    Public picSignature As New PictureBox
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        AddDeptSign(cbbDept.SelectedValue.ToString, txtReview.Text,
                    txtEndorse.Text, txtApprove.Text,
                    GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
        MsgBox("Successfully Added")
        Me.Close()
    End Sub

    Private Sub frmDeptSignature_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Me.KeyPreview = True
        If e.KeyCode = Keys.Escape Then
            Me.Close()

        End If
    End Sub
    Private Sub frmDeptSignature_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            MyDepartment(LoadingDepartment())
            Using dtLoadUserAccDept As DataTable = LoadingUserAccDept(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                If dtLoadUserAccDept.Rows.Count <> 0 Then
                    btnUpdate.Visible = True
                    btnAdd.Visible = False
                    cbbDept.SelectedValue = GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0)
                    txtEndorse.Text = dtLoadUserAccDept.Rows(0).Item("emp_Endorser")
                    txtReview.Text = dtLoadUserAccDept.Rows(0).Item("emp_Reviewer")
                    txtApprove.Text = dtLoadUserAccDept.Rows(0).Item("emp_Approver")
                Else
                    btnUpdate.Visible = True
                    btnAdd.Visible = False
                    cbbDept.SelectedValue = GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0)
                End If
            End Using
        Catch ex As Exception
        End Try
    End Sub

    Private Sub btnUpdate_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
        Dim myUserID As String = GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0)
        UpdateDeptSign(myUserID,
                       GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0), txtReview.Text, txtEndorse.Text, txtApprove.Text)
        MsgBox("Successfully Update")
        LoadingUserAccDept(myUserID)
        Me.Close()
    End Sub

    Private Sub frmDeptSignature_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        cbbDept.DataSource = Nothing
    End Sub
End Class