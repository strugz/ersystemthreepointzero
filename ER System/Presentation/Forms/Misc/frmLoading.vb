Public Class frmLoading

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        Threading.Thread.Sleep(1000)
    End Sub
    Private Sub BackgroundWorker1_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        Try
            Dim ClsData As New ClsLoadData
            Me.Close()
            frmEReport.TopMost = False
            frmERType.TopMost = True
            frmERType.btnSend.Text = "Send"
            MsgBox("E-mail Sent . . .", TopMost = True)
            Dim myERData As String()
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
            If myERData(13) <> "" Then
                'PrintSendingReport()
            End If
            frmERType.Close()
            frmERType.Enabled = True
            frmEReport.TopMost = True
            frmRpt.CrystalReportViewer1.PrintReport()
        Catch ex As Exception
            MsgBox("Please Call IT Immediately")
        End Try
    End Sub
    Private Sub frmLoading_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.TransparencyKey = Me.BackColor
        BackgroundWorker1.RunWorkerAsync()
    End Sub
End Class

