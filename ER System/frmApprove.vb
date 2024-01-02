Public Class frmApprove
    Private Sub frmApprove_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Me.KeyPreview = True
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub
    Private Sub frmApprove_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Call UserAccount()
    End Sub

    Private Sub UserAccount()
        Using dtLoadUserAccountFiled As DataTable = LoadingUserAccountFiled(
        GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0),
        GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
            dgvUser.DataSource = dtLoadUserAccountFiled
        End Using
        DgUserDataVisibility({"UserID"})

        If GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0) = 0 Then
            Me.dgvUser.Columns("Number of File").Visible = False
            Me.dgvUser.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Else
            Me.dgvUser.Columns("Number of File").Visible = True
        End If
    End Sub
    Private Sub dgvUser_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvUser.CellDoubleClick
        If e.RowIndex < 0 Then
            MsgBox("Please Double click on the row you are interested in")
            Exit Sub
        Else
            If GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0) = "1" Then
                Using dtLoadUserReportDetailsFiled As DataTable = LoadingUserReportDetailsFILED(dgvUser.Rows(e.RowIndex).Cells("UserID").Value,
                                              GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0),
                                              GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                    dgvUserReportDetails.DataSource = dtLoadUserReportDetailsFiled
                End Using
            Else
                Using dtLoadUserReportDetailsDONE As DataTable = LoadingUserReportDetailsDONE(dgvUser.Rows(e.RowIndex).Cells("UserID").Value,
                                             GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0),
                                             GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                    dgvUserReportDetails.DataSource = dtLoadUserReportDetailsDONE
                End Using
            End If
        End If
        dgvUserReportDetails.Columns("ID").Visible = False
        dgvUserReportDetails.Columns("Report Description").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvUserReportDetails.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnsMode.Fill
        If dgvUserReportDetails.Rows.Count <> 0 Then
            btnApprove.Enabled = True
            btnReportViewer.Enabled = True
            btnCancel.Enabled = True
            btnReject.Enabled = True
        Else
            MsgBox("Empty Report")
        End If
    End Sub
    Private Sub btnReject_Click(sender As Object, e As EventArgs) Handles btnReject.Click
        frmCancelNote.ShowDialog()
        Call UserAccount()
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnApprove.Click
        Dim ClsData As New ClsLoadData
        Dim ApproverValidate As String = ClsData.ApproverValidation(
            dgvUser.CurrentRow.Cells("UserID").Value,
            GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0),
            dgvUserReportDetails.CurrentRow.Cells("ID").Value)
        If ApproverValidate = "True" Then
            Call UpdateFileStatus(
                dgvUser.CurrentRow.Cells("UserID").Value,
                dgvUserReportDetails.CurrentRow.Cells("ID").Value,
               GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
            MsgBox("Expense Report Verified")
            Using dtLoadUserReportDetailsFiled As DataTable = LoadingUserReportDetailsFILED(dgvUser.CurrentRow.Cells("UserID").Value,
                              GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0),
                              GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                dgvUserReportDetails.DataSource = dtLoadUserReportDetailsFiled
            End Using
            Call UserAccount()
        ElseIf ApproverValidate = "Done" Then
            MsgBox("Already Confirmed")
        Else
            MsgBox("Not Yet Verified by the Precedent Approver")
        End If
    End Sub
    Private Sub dgvUserReportDetails_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvUserReportDetails.CellClick
        Dim ClsData As New ClsLoadData
        If e.RowIndex < 0 Then
            MsgBox("Please Double click on the row you are interested in")
            Exit Sub
        Else
            ClsData.DeleteEReportDetails(Application.StartupPath + "\settings.txt")
            Me.Enabled = False
            Threading.Thread.Sleep(500)
            Me.Enabled = True
            ClsData.SetEReportDetails(dgvUserReportDetails.Rows(e.RowIndex).Cells("ID").Value)
            btnApprove.Enabled = True
            btnReportViewer.Enabled = True
            btnCancel.Enabled = True
        End If
    End Sub
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles btnReportViewer.Click
        frmRpt.Show()
        dgvUser.Enabled = False
        dgvUserReportDetails.Enabled = False
        btnCancel.Enabled = False
        btnReportViewer.Enabled = False
        btnReject.Enabled = False
        btnApprove.Enabled = False
    End Sub
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        btnApprove.Enabled = False
        btnReportViewer.Enabled = False
        btnReject.Enabled = False
    End Sub
    Private Sub dgvUserReportDetails_CellMouseDown(sender As Object, e As DataGridViewCellMouseEventArgs) Handles dgvUserReportDetails.CellMouseDown
        Dim ClsData As New ClsLoadData
        If e.Button = MouseButtons.Right Then
            dgvUserReportDetails.Rows(e.RowIndex).Selected = True
            dgvUserReportDetails.CurrentCell = dgvUserReportDetails.Rows(e.RowIndex).Cells(1)
            ClsData.ApproverValidation(
                dgvUser.CurrentRow.Cells("UserID").Value,
                GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0),
                dgvUserReportDetails.Rows(e.RowIndex).Cells("ID").Value)
            ClsData.DeleteEReportDetails(Application.StartupPath + "\settings.txt")
            Me.Enabled = False
            Threading.Thread.Sleep(500)
            Me.Enabled = True
            ClsData.SetEReportDetails(dgvUserReportDetails.Rows(e.RowIndex).Cells("ID").Value)
            CMSEditUserExpense.Show(dgvUserReportDetails, e.Location)
            CMSEditUserExpense.Show(Cursor.Position)
        End If
    End Sub

    Private Sub EditExpenseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditExpenseToolStripMenuItem.Click
        Dim ClsData As New ClsLoadData
        DGVLoadExpenseReport(EReportOpenValidatiionApprover(), dgvUser.CurrentRow.Cells("UserID").Value)
        ClsData.RegistrySettings("HKEY_CURRENT_USER\Software\ER System", "Settings", {"Approver"}, {"1"})
        frmEReport.ShowDialog()
    End Sub

    Private Sub frmApprove_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Call ReleasMemory()
        frmRpt.Close()
        dgvUserReportDetails.DataSource = Nothing
    End Sub
    Private Sub frmApprove_MouseClick(sender As Object, e As MouseEventArgs) Handles Me.MouseClick
        If Application.OpenForms().OfType(Of frmRpt).Any Then
            frmRpt.BringToFront()
        End If
    End Sub

    Private Sub BTNRefresh_Click(sender As Object, e As EventArgs) Handles BTNRefresh.Click
        Call UserAccount()
    End Sub
End Class