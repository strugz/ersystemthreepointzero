Imports CrystalDecisions.Shared
Imports CrystalDecisions.CrystalReports.Engine
Imports Microsoft.Win32

Namespace Services.FileServices
    Public Class ReportExportService
        ' Moved exactly from frmRpt.export() with no behavior change

        Public Sub ExportReport(startupPath As String, strExportFile As String, reportUserId As String, reportId As String, tripleDes As clsEncryption)
            Dim ExportER As New ReportDocument

            ' Replicate registry decryption exactly as it was
            Dim user As String = tripleDes.DecryptData(Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", "")))
            Dim password As String = tripleDes.DecryptData(Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", "")))

            ExportER.Load(startupPath & "\ER Report.rpt")
            ExportER.SetDatabaseLogon(user, password)
            ExportER.SetParameterValue("@UserID", reportUserId)
            ExportER.SetParameterValue("@reportID", reportId)

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
    End Class
End Namespace
