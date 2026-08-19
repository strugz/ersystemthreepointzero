Imports CrystalDecisions.CrystalReports.Engine
Imports CrystalDecisions.Shared
Imports System.Security.Cryptography
Imports Microsoft.Win32
Public Class frmrptExpenseSummary
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Dim User As String
    Dim password As String
    Dim server As String
    Dim database As String
    Dim rptER As New ReportDocument
    Dim ExportER As New ReportDocument
    Public reportID As String
    Dim str As String
    Private Sub CrystalReportViewer1_Load(sender As Object, e As EventArgs) Handles CrystalReportViewer1.Load
        User = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
        password = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))
        server = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ServerName", ""))
        database = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Database", ""))
        rptER.Load(Application.StartupPath & "\ExpenseSummary.rpt")
        rptER.SetDatabaseLogon(User, password)
        rptER.SetParameterValue("@ReportDateFrom", ExpenseSummaryDateFrom)
        rptER.SetParameterValue("@ReportDateTo", ExpenseSummaryDateTo)
        CrystalReportViewer1.ReportSource = rptER
        CrystalReportViewer1.Refresh()
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        CrystalReportViewer1.ExportReport()
        'SaveFileDialog1.Filter = "Excel 2003|*.xls"
        'If SaveFileDialog1.ShowDialog = System.Windows.Forms.DialogResult.OK Then
        '    str = SaveFileDialog1.FileName
        '    CrystalReportViewer1.ExportReport()
        '    'export()
        'End If
    End Sub
    Public Sub export()
        User = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
        password = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))
        server = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "ServerName", ""))
        database = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Database", ""))

        ExportER.Load(Application.StartupPath & "\ExpenseSummary.rpt")
        ExportER.SetDatabaseLogon(User, password)
        ExportER.SetParameterValue("@ReportDateFrom", ExpenseSummaryDateFrom)
        ExportER.SetParameterValue("@ReportDateTo", ExpenseSummaryDateTo)

        Dim ErExportOptions As ExportOptions
        Dim ERDiskDestinationOptions As New DiskFileDestinationOptions()
        Dim ErFormatTypeOptions As New ExcelFormatOptions()
        ERDiskDestinationOptions.DiskFileName = str
        ErExportOptions = ExportER.ExportOptions
        With ErExportOptions
            .ExportDestinationType = ExportDestinationType.DiskFile
            .ExportFormatType = ExportFormatType.Excel
            .ExportDestinationOptions = ERDiskDestinationOptions
            .ExportFormatOptions = ErFormatTypeOptions
        End With
        ExportER.Export()
    End Sub
End Class