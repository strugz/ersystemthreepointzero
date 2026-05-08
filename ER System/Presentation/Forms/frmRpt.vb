Imports CrystalDecisions.Shared
Imports ER_System.Presentation.Presenters
Imports ER_System.Presentation.ViewModels

Public Class frmRpt
    Private Shared ReadOnly StartupPath As String = System.Windows.Forms.Application.StartupPath
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Public strExportFile As String = Nothing
    Dim User As String
    Dim password As String
    Private ReadOnly _reportPresenter As New ReportPrintPresenter()

    Private Function BuildReportPrintViewModel(ByVal myERData As String()) As ReportPrintViewModel
        Return New ReportPrintViewModel With {
            .UserLevel = Convert.ToString(GetRegistryValue("Software\\ER System\\UserAccount", {"UserLevel"})(0)),
            .CurrentUserId = Convert.ToString(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0)),
            .ReportOwnerUserId = myERData(14),
            .ReportStatus = myERData(12),
            .ReportType = myERData(3),
            .ReportId = myERData(13),
            .ReportUserId = ModDataStore.ReportUserID,
            .Username = Convert.ToString(GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0)),
            .Rbt = modLoadingData.RBT,
            .LocationCode = modLoadingData.LocationCode,
            .ReportDate = modLoadingData.sDate
        }
    End Function

    Private Sub frmRpt_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        CrystalReportViewer1.ReportSource = Nothing
        frmApprove.dgvUser.Enabled = True
        frmApprove.dgvUserReportDetails.Enabled = True
        frmApprove.btnCancel.Enabled = True
        frmApprove.btnReportViewer.Enabled = True
        frmApprove.btnReject.Enabled = True
        frmApprove.btnApprove.Enabled = True
        Call ReleasMemory()
    End Sub

    Private Sub frmRpt_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(StartupPath + "\settings.txt")

        Dim model As ReportPrintViewModel = BuildReportPrintViewModel(myERData)

        If _reportPresenter.IsAdminOwner(model) Then
            Me.CrystalReportViewer1.DisplayToolbar = True
            Me.CrystalReportViewer1.ShowPrintButton = False
            Me.CrystalReportViewer1.ShowExportButton = False
            Me.CrystalReportViewer1.ShowNextPage()
            btnSendPrint.Enabled = True
            CreateUserDSN()
        Else
            Call RPTValidation(model.ReportStatus, model.ReportType)
            CreateUserDSN()
        End If
    End Sub

    Public Sub export()
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(StartupPath + "\settings.txt")
        Dim model As ReportPrintViewModel = BuildReportPrintViewModel(myERData)

        strExportFile = _reportPresenter.BuildExportFilePath(model, StartupPath)

        Dim exportService As New ER_System.Services.FileServices.ReportExportService()
        exportService.ExportReport(StartupPath, strExportFile, model.ReportUserId, model.ReportId, TripleDes)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnSendPrint.Click
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(StartupPath + "\settings.txt")

        Dim message As String = _reportPresenter.CanSendToPrint(myERData(13))
        If message <> String.Empty Then
            MsgBox(message)
        Else
            Try
                If LoadReportSentStatus(myERData(13)).Rows(0).Item("ReportSentStatus").ToString() = "1" Then
                    CrystalReportViewer1.PrintReport()
                Else
                    frmERType.ShowDialog()
                    If LoadReportSentStatus(myERData(13)).Rows(0).Item("ReportSentStatus").ToString() = "1" Then
                        CrystalReportViewer1.PrintReport()
                    End If
                End If
            Catch ex As Exception
                MessageBox.Show("Sending Error Please Contact ID Administrator.")
            End Try
        End If
    End Sub

    Private Sub CrystalReportViewer1_Load(sender As Object, e As EventArgs) Handles CrystalReportViewer1.Load

        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        Dim rptER As New ReportDocument
        myERData = ClsData.GetEReportDetails(StartupPath + "\settings.txt")
        rptER = ClsData.MyReportDocument(
                StartupPath & "\ER Report.rpt",
                TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", "")),
                TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", "")),
                {"@UserID", "@reportID"}, {myERData(14), myERData(13)})
        CrystalReportViewer1.ReportSource = rptER
        CrystalReportViewer1.Refresh()
    End Sub
End Class
