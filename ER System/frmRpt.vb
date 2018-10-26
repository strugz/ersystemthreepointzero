Imports CrystalDecisions.CrystalReports.Engine
Imports CrystalDecisions.Shared
Imports System.Security.Cryptography
Imports Microsoft.Win32
Public Class frmRpt
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Public strExportFile As String = Nothing
    Dim User As String
    Dim password As String
    Dim server As String
    Dim database As String
    Dim rptER As New ReportDocument
    Dim ExportER As New ReportDocument
    Public reportID As String
    Public I As String = Nothing
    Public MyUserID As String
    Private Sub frmRpt_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        frmEReport.BringToFront()
        'frmMain.TopMost = False
        'Me.TopMost = False
    End Sub
    Private Sub frmRpt_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If modLoadingData.LoginUserLevel = "Admin" Then
            cryptRptER.Anchor = AnchorStyles.Top
            cryptRptER.Anchor = AnchorStyles.Bottom
            cryptRptER.Anchor = AnchorStyles.Right
            cryptRptER.Anchor = AnchorStyles.Left
            CreateUserDSN()
            Me.cryptRptER.ShowNextPage()
        Else
            CreateUserDSN()
        End If
    End Sub
    Public Sub export()

        User = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
        password = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))
        server = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ServerName", ""))
        database = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Database", ""))

        ExportER.Load(Application.StartupPath & "\ER Report.rpt")
        ExportER.SetDatabaseLogon(User, password)
        'rptER.SetParameterValue("@StartDate", frmEReport.DtpReportFrom.Text)
        'rptER.SetParameterValue("@EndDate", frmEReport.DtpReportTo.Text)
        ExportER.SetParameterValue("@UserID", modLoadingData.LoginuserID)
        ExportER.SetParameterValue("@reportID", modLoadingData.ReportIDExport)
        Dim dtp As DateTime = Date.Now
        '  Dim strExportFile As String = Application.StartupPath & "\" & frmERType.txt.Text & ".pdf".ToString
        If modLoadingData.RBT = "0" Then
            strExportFile = Application.StartupPath & "\ERPDF\" & modLoadingData.LoginUsername & "ER" & modLoadingData.sDate.ToString("ddMMMyyyy").ToUpper & ".pdf".ToString
        Else
            strExportFile = Application.StartupPath & "\ERPDF\" & modLoadingData.LoginUsername & modLoadingData.LocationCode & modLoadingData.sDate.ToString("ddMMMyyyy").ToUpper & ".pdf".ToString
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
        ExportER.Export()
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnSendPrint.Click

        If modLoadingData.ReportIDExport = Nothing Then
            MsgBox("Select Report To Send")
        Else
            Try
                If LoadReportSentStatus(modLoadingData.ReportIDExport) = "1" Then
                    cryptRptER.PrintReport()
                Else
                    Me.reportID = modLoadingData.ReportIDExport
                    frmERType.Show()
                    Me.TopMost = False
                    frmERType.TopMost = True
                End If
            Catch ex As Exception
                MessageBox.Show("Sending Error Please Contact ID Administrator.")
            End Try
        End If
    End Sub
    Function LoadReportSentStatus(ByVal reportidexport As String) As String
        Dim sqlcmdLoadReportSentStatus As New SqlClient.SqlCommand
        Dim dtloadReportSentStatus As New DataTable
        With sqlcmdLoadReportSentStatus
            .Connection = SQLConnection
            .CommandText = "SELECT a.ReportSentStatus from tbReportDetails as a where [ID] = '" & reportidexport & "'"
            .CommandType = CommandType.Text
            dtloadReportSentStatus.Load(.ExecuteReader)
            If dtloadReportSentStatus.Rows.Count <> 0 Then
                reportidexport = IIf(IsDBNull(dtloadReportSentStatus.Rows(0).Item("ReportSentStatus")) = True, "", dtloadReportSentStatus.Rows(0).Item("ReportSentStatus"))
            End If
        End With
        Return reportidexport
    End Function

    Private Sub cryptRptER_Load(sender As Object, e As EventArgs) Handles cryptRptER.Load
        User = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
        password = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))
        server = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ServerName", ""))
        database = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Database", ""))
        rptER.Load(Application.StartupPath & "\ER Report.rpt")
        rptER.SetDatabaseLogon(User, password)
        'rptER.SetParameterValue("@StartDate", frmEReport.DtpReportFrom.Text)
        'rptER.SetParameterValue("@EndDate", frmEReport.DtpReportTo.Text)
        rptER.SetParameterValue("@UserID", MyUserID)
        rptER.SetParameterValue("@reportID", reportID)
        cryptRptER.ReportSource = rptER
        cryptRptER.Refresh()
    End Sub
End Class
