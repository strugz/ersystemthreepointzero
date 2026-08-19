Public Class frmCancelNote
    Private ReadOnly _rejectActionService As ERSystem.AppServices.RejectActionService = ApprovalServicesFactory.CreateRejectActionService()
    Private ReadOnly _selectedReportContextStore As ERSystem.AppServices.ISelectedReportContextStore = ApprovalServicesFactory.CreateSelectedReportContextStore()

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnOkay.Click
        Try
            Dim myERData As String() = _selectedReportContextStore.LoadValues()
            Dim result As ERSystem.AppServices.RejectActionResult = _rejectActionService.RejectReport(myERData(13), myERData(14), rtbNote.Text)

            If Not result.IsSuccess Then
                MsgBox(result.Message)
            Else
                Dim reloadResult As ERSystem.AppServices.ApproveReloadResult = result.ReloadResult
                frmApprove.dgvUserReportDetails.DataSource = reloadResult.ReportDetails
                frmApprove.dgvUser.DataSource = reloadResult.UserAccounts
                frmApprove.dgvUserReportDetails.Columns("ID").Visible = False
                frmApprove.dgvUserReportDetails.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnsMode.Fill

                If String.Equals(reloadResult.ChangeLoading, "1", StringComparison.Ordinal) Then
                    frmApprove.dgvUser.Columns("Number of File").Visible = True
                Else
                    frmApprove.Show()
                    frmApprove.dgvUser.Columns("Number of File").Visible = False
                    frmApprove.dgvUser.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                End If
            End If
        Catch ex As Exception
            Debug.WriteLine("Unable to reject report: " & ex.Message)
            MsgBox("Unable to return the selected report. " & ex.Message)
        End Try
        Me.Close()
    End Sub

    Private Sub frmCancelNote_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class
