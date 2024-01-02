Public Class frmCancelNote

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnOkay.Click
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")

        Dim sqlcmdUpdateFilePrintStatus As New SqlClient.SqlCommand

        Try
            If myERData(13) = Nothing Or myERData(13) = "" Then
                MsgBox("No Report Selected")
            Else
                modMaintenance.RejectFiledER(myERData(13), rtbNote.Text)
                'With sqlcmdUpdateFilePrintStatus
                '    .Connection = SQLConnection
                '    .CommandText = "sp2_LoadUserReportDetailsCancel"
                '    .Parameters.Add("@reportID", SqlDbType.BigInt).Value = myERData(13)
                '    .Parameters.AddWithValue("@reportCancelNote", rtbNote.Text).SqlDbType = SqlDbType.VarChar
                '    .CommandType = CommandType.StoredProcedure
                '    .ExecuteNonQuery()
                '    MsgBox("Report Rejected")
                'End With
                If GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0) = "1" Then
                    frmApprove.dgvUserReportDetails.DataSource = LoadingUserReportDetailsFILED(myERData(14), GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0), GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                Else
                    frmApprove.dgvUserReportDetails.DataSource = LoadingUserReportDetailsDONE(myERData(14), GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0), GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                End If

                frmApprove.dgvUserReportDetails.Columns("ID").Visible = False
                frmApprove.dgvUserReportDetails.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnsMode.Fill
                If GetRegistryValue("Software\\ER System\\Settings", {"ChangeLoading"})(0) = "1" Then
                    LoadingUserAccountFiled(GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0),
                                            GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                    SetRegistry("Settings", {"ChangeLoading"}, {"1"})
                Else
                    LoadingUserAccountPending(GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0))
                    frmApprove.Show()
                    SetRegistry("Settings", {"ChangeLoading"}, {"0"})
                    frmApprove.btnCancel.Location = New Point(331, 480)
                    frmApprove.btnReportViewer.Location = New Point(442, 480)
                    frmApprove.dgvUser.Columns("Number of File").Visible = False
                    frmApprove.dgvUser.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                End If
            End If
        Catch ex As Exception
        End Try
        Me.Close()
    End Sub

    Private Sub frmCancelNote_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class