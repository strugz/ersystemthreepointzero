Imports CrystalDecisions.Shared

Public Class frmRpt
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Public strExportFile As String = Nothing
    Dim User As String
    Dim password As String
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
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        If GetRegistryValue("Software\\ER System\\UserAccount", {"UserLevel"})(0) = "Admin" And
            myERData(14) = GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0) Then
            Me.CrystalReportViewer1.DisplayToolbar = True
            Me.CrystalReportViewer1.ShowPrintButton = False
            Me.CrystalReportViewer1.ShowExportButton = False
            Me.CrystalReportViewer1.ShowNextPage()
            btnSendPrint.Enabled = True
            CreateUserDSN()
        Else
            Call RPTValidation(myERData(12), myERData(3))
            CreateUserDSN()
        End If
    End Sub
    Public Sub export()
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        Dim ExportER As New ReportDocument
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        User = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
        password = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))

        ExportER.Load(Application.StartupPath & "\ER Report.rpt")
        ExportER.SetDatabaseLogon(User, password)
        ExportER.SetParameterValue("@UserID", ModDataStore.ReportUserID)
        ExportER.SetParameterValue("@reportID", myERData(13))
        Dim dtp As DateTime = Date.Now
        If modLoadingData.RBT = "0" Then
            strExportFile = Application.StartupPath & "\ERPDF\" & GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) & "ER" & modLoadingData.sDate.ToString("ddMMMyyyy").ToUpper & ".pdf".ToString
        Else
            strExportFile = Application.StartupPath & "\ERPDF\" & GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) & modLoadingData.LocationCode & modLoadingData.sDate.ToString("ddMMMyyyy").ToUpper & ".pdf".ToString
        End If
        Dim ErExportOptions As ExportOptions
        Dim ERDiskDestinationOptions As New DiskFileDestinationOptions()
        Dim ErFormatTypeOptions As New PdfRtfWordFormatOptions()
        ERDiskDestinationOptions.DiskFileName = strExportFile
        ErExportOptions = ExportER.ExportOptions
        With ErExportOptions
            .ExportDestinationType = ExportDestinationType.DiskFile
            .ExportFormatType = ExportFormatType.PortableDocFormat
            .ExportDestinationOptions = ERDiskDestinationOptions
            .ExportFormatOptions = ErFormatTypeOptions
        End With
        ExportER.PrintOptions.PrinterDuplex = PrinterDuplex.Simplex
        ExportER.Export()
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnSendPrint.Click
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        If myERData(13) = Nothing Then
            MsgBox("Select Report To Send")
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
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        rptER = ClsData.MyReportDocument(
                Application.StartupPath & "\ER Report.rpt",
                TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", "")),
                TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", "")),
                {"@UserID", "@reportID"}, {myERData(14), myERData(13)})
        CrystalReportViewer1.ReportSource = rptER
        CrystalReportViewer1.Refresh()
    End Sub
End Class
