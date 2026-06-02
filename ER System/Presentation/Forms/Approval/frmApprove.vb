Public Class frmApprove
    Private ReadOnly _approveService As ERSystem.AppServices.ApproveService = ApprovalServicesFactory.CreateApproveService()
    Private ReadOnly _approveActionService As ERSystem.AppServices.ApproveActionService = ApprovalServicesFactory.CreateApproveActionService()
    Private ReadOnly _selectionContextService As ERSystem.AppServices.ApproveSelectionContextService = ApprovalServicesFactory.CreateSelectionContextService()

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
        Dim result As ERSystem.AppServices.ApproveUserAccountLoadResult = _approveService.LoadUserAccounts()

        dgvUser.DataSource = result.Users
        DgUserDataVisibility({"UserID"})

        If Not result.ShowNumberOfFile Then
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
            Dim result As ERSystem.AppServices.ApproveReportDetailsLoadResult = _approveService.LoadReportDetails(dgvUser.Rows(e.RowIndex).Cells("UserID").Value.ToString())
            dgvUserReportDetails.DataSource = result.ReportDetails

            If Not result.HasRows Then
                MsgBox("Empty Report")
                Exit Sub
            End If
        End If

        dgvUserReportDetails.Columns("ID").Visible = False
        dgvUserReportDetails.Columns("Report Description").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvUserReportDetails.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnsMode.Fill
        btnApprove.Enabled = True
        btnReportViewer.Enabled = True
        btnCancel.Enabled = True
        btnReject.Enabled = True
    End Sub
    Private Sub btnReject_Click(sender As Object, e As EventArgs) Handles btnReject.Click
        frmCancelNote.ShowDialog()
        Call UserAccount()
    End Sub
    Private Sub btnApprove_Click(sender As Object, e As EventArgs) Handles btnApprove.Click
        If MsgBox("Are you sure you want to approve this report?", MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "Confirm Approval") <> MsgBoxResult.Yes Then
            Exit Sub
        End If

        Dim result As ERSystem.AppServices.ApproveActionResult = _approveActionService.ApproveReport(
            dgvUser.CurrentRow.Cells("UserID").Value.ToString(),
            dgvUserReportDetails.CurrentRow.Cells("ID").Value.ToString())

        MsgBox(result.Message)

        If result.IsSuccess Then
            dgvUserReportDetails.DataSource = result.ReportDetails
            dgvUser.DataSource = result.UserAccounts
            DgUserDataVisibility({"UserID"})

            If result.ShowNumberOfFile Then
                dgvUser.Columns("Number of File").Visible = True
            Else
                dgvUser.Columns("Number of File").Visible = False
                dgvUser.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            End If
        End If
    End Sub
    Private Sub dgvUserReportDetails_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvUserReportDetails.CellClick
        If e.RowIndex < 0 Then
            MsgBox("Please Double click on the row you are interested in")
            Exit Sub
        Else
            Dim result As ERSystem.AppServices.ApproveSelectionContextResult = _selectionContextService.PrepareSelectedReportContext(dgvUserReportDetails.Rows(e.RowIndex).Cells("ID").Value.ToString())

            If Not result.HasSelection Then
                Exit Sub
            End If

            Me.Enabled = False
            Threading.Thread.Sleep(result.DelayMilliseconds)
            Me.Enabled = True

            If result.ShouldEnableActionButtons Then
                btnApprove.Enabled = True
                btnReportViewer.Enabled = True
                btnCancel.Enabled = True
            End If
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
        If e.Button = MouseButtons.Right Then
            dgvUserReportDetails.Rows(e.RowIndex).Selected = True
            dgvUserReportDetails.CurrentCell = dgvUserReportDetails.Rows(e.RowIndex).Cells(1)

            Dim result As ERSystem.AppServices.ApproveSelectionContextResult = _selectionContextService.PrepareContextMenuSelection(dgvUserReportDetails.Rows(e.RowIndex).Cells("ID").Value.ToString())

            If Not result.HasSelection Then
                Exit Sub
            End If

            Me.Enabled = False
            Threading.Thread.Sleep(result.DelayMilliseconds)
            Me.Enabled = True

            If result.ShouldShowContextMenu Then
                CMSEditUserExpense.Show(dgvUserReportDetails, e.Location)
                CMSEditUserExpense.Show(Cursor.Position)
            End If
        End If
    End Sub

    Private Sub EditExpenseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditExpenseToolStripMenuItem.Click
        DGVLoadExpenseReport(EReportOpenValidatiionApprover(), dgvUser.CurrentRow.Cells("UserID").Value)
        _selectionContextService.SetApproverEditMode()
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
