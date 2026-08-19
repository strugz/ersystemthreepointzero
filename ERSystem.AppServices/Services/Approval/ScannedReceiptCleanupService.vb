Option Strict On

Imports System.IO
Imports ERSystem.Infrastructure.Data

Public Class ScannedReceiptCleanupService
    Private ReadOnly _reportDetailService As IReportDetailService
    Private ReadOnly _financeReviewService As IFinanceReviewService
    Private ReadOnly _scannedReceiptsDirectory As String

    Public Sub New()
        Me.New(New ReportDetailService(), New FinanceReviewService(), GetDefaultScannedReceiptsDirectory())
    End Sub

    Public Sub New(reportDetailService As IReportDetailService, financeReviewService As IFinanceReviewService)
        Me.New(reportDetailService, financeReviewService, GetDefaultScannedReceiptsDirectory())
    End Sub

    Public Sub New(reportDetailService As IReportDetailService,
                   financeReviewService As IFinanceReviewService,
                   scannedReceiptsDirectory As String)
        If reportDetailService Is Nothing Then
            Throw New ArgumentNullException("reportDetailService")
        End If

        If financeReviewService Is Nothing Then
            Throw New ArgumentNullException("financeReviewService")
        End If

        If String.IsNullOrWhiteSpace(scannedReceiptsDirectory) Then
            Throw New ArgumentException("Scanned receipts directory is required.", "scannedReceiptsDirectory")
        End If

        _reportDetailService = reportDetailService
        _financeReviewService = financeReviewService
        _scannedReceiptsDirectory = Path.GetFullPath(scannedReceiptsDirectory)
    End Sub

    Public Sub FinalizeScannedReceiptsForApprovedReport(reportId As String)
        If String.IsNullOrWhiteSpace(reportId) Then
            Return
        End If

        Dim report = _reportDetailService.GetById(reportId)

        If report Is Nothing OrElse String.IsNullOrWhiteSpace(report.ReportAttachment) Then
            _financeReviewService.MarkScannedReceiptsDeleted(reportId)
            Return
        End If

        For Each attachmentPath As String In report.ReportAttachment.Split(";"c)
            Dim path As String = attachmentPath.Trim()

            If path.Length = 0 Then
                Continue For
            End If

            Try
                If Not IsPathInScannedReceiptsDirectory(path) Then
                    Debug.WriteLine("Skipped scanned receipt cleanup outside the configured directory: " & path)
                ElseIf File.Exists(path) Then
                    File.Delete(path)
                End If
            Catch ex As IOException
                Debug.WriteLine("Unable to delete scanned receipt file: " & ex.Message)
            Catch ex As UnauthorizedAccessException
                Debug.WriteLine("Unable to delete scanned receipt file: " & ex.Message)
            End Try
        Next

        _financeReviewService.MarkScannedReceiptsDeleted(reportId)
        _financeReviewService.ClearScannedReceiptAttachment(reportId)
    End Sub

    Private Function IsPathInScannedReceiptsDirectory(filePath As String) As Boolean
        Try
            Dim fullFilePath As String = Path.GetFullPath(filePath)
            Dim fullDirectoryPath As String = _scannedReceiptsDirectory.
                TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) & Path.DirectorySeparatorChar

            Return fullFilePath.StartsWith(fullDirectoryPath, StringComparison.OrdinalIgnoreCase)
        Catch ex As Exception
            Debug.WriteLine("Unable to validate scanned receipt cleanup path: " & ex.Message)
            Return False
        End Try
    End Function

    Private Shared Function GetDefaultScannedReceiptsDirectory() As String
        Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScannedReceipts")
    End Function
End Class
