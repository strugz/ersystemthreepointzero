Option Strict On

Imports System.IO
Imports ERSystem.Infrastructure.Data

Namespace AppServices
    Friend Class ScannedReceiptCleanupService
        Private ReadOnly _reportDetailService As IReportDetailService
        Private ReadOnly _financeReviewService As IFinanceReviewService

        Public Sub New()
            Me.New(New ReportDetailService(), New FinanceReviewService())
        End Sub

        Public Sub New(reportDetailService As IReportDetailService, financeReviewService As IFinanceReviewService)
            _reportDetailService = reportDetailService
            _financeReviewService = financeReviewService
        End Sub

        Public Sub DeleteScannedReceiptsForApprovedReport(reportId As String)
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
                    If File.Exists(path) Then
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
    End Class
End Namespace
