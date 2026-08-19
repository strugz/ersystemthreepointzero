Option Strict On

Imports System.IO
Imports ERSystem.AppServices
Imports ERSystem.Domain
Imports ERSystem.Infrastructure.Data
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Services
    <TestClass>
    Public Class ScannedReceiptCleanupServiceTests
        <TestMethod>
        Public Sub FinalApprovalDeletesOnlyAllowlistedLocalFilesAndRetainsDurableReceipts()
            Dim testRoot As String = Path.Combine(Path.GetTempPath(), "ERSystemReceiptCleanup_" & Guid.NewGuid().ToString("N"))
            Dim receiptsRoot As String = Path.Combine(testRoot, "ScannedReceipts")
            Dim outsideRoot As String = Path.Combine(testRoot, "outside")
            Directory.CreateDirectory(receiptsRoot)
            Directory.CreateDirectory(outsideRoot)

            Dim insidePath As String = Path.Combine(receiptsRoot, "inside.pdf")
            Dim outsidePath As String = Path.Combine(outsideRoot, "outside.pdf")
            File.WriteAllText(insidePath, "inside")
            File.WriteAllText(outsidePath, "outside")

            Try
                Dim reportService As New FakeReportDetailService(New ReportDetailDto With {
                    .ID = "report-1",
                    .ReportAttachment = insidePath & ";" & outsidePath
                })
                Dim financeService As New FakeFinanceReviewService()
                Dim service As New ScannedReceiptCleanupService(reportService, financeService, receiptsRoot)

                service.FinalizeScannedReceiptsForApprovedReport("report-1")

                Assert.IsFalse(File.Exists(insidePath))
                Assert.IsTrue(File.Exists(outsidePath))
                Assert.AreEqual(1, financeService.MarkDeletedCalls)
                Assert.AreEqual(1, financeService.ClearLegacyPathCalls)
            Finally
                If Directory.Exists(testRoot) Then
                    Directory.Delete(testRoot, True)
                End If
            End Try
        End Sub

        Private NotInheritable Class FakeReportDetailService
            Implements IReportDetailService

            Private ReadOnly _report As ReportDetailDto

            Public Sub New(report As ReportDetailDto)
                _report = report
            End Sub

            Public Function GetById(reportId As String) As ReportDetailDto Implements IReportDetailService.GetById
                Return _report
            End Function

            Public Function GetAll() As List(Of ReportDetailDto) Implements IReportDetailService.GetAll
                Throw New NotSupportedException()
            End Function

            Public Function Create(report As CreateReportDetailDto) As ReportDetailDto Implements IReportDetailService.Create
                Throw New NotSupportedException()
            End Function

            Public Function CreateReport(report As CreateReportDetailDto, cashAdvance As CreateCashAdvanceDto) As ReportDetailDto Implements IReportDetailService.CreateReport
                Throw New NotSupportedException()
            End Function

            Public Function CreateReport(report As CreateReportDetailDto,
                                         cashAdvance As CreateCashAdvanceDto,
                                         scannedReceiptPaths As IEnumerable(Of String),
                                         createdByUserId As Integer?) As ReportDetailDto Implements IReportDetailService.CreateReport
                Throw New NotSupportedException()
            End Function

            Public Sub Update(report As UpdateReportDetailDto) Implements IReportDetailService.Update
                Throw New NotSupportedException()
            End Sub

            Public Sub UpdateReport(report As UpdateReportDetailDto,
                                    cashAdvance As UpdateCashAdvanceDto,
                                    scannedReceiptPaths As IEnumerable(Of String),
                                    attachmentUpdateMode As ScannedReceiptAttachmentUpdateMode,
                                    createdByUserId As Integer?) Implements IReportDetailService.UpdateReport
                Throw New NotSupportedException()
            End Sub
        End Class

        Private NotInheritable Class FakeFinanceReviewService
            Implements IFinanceReviewService

            Public Property MarkDeletedCalls As Integer
            Public Property ClearLegacyPathCalls As Integer

            Public Sub MarkScannedReceiptsDeleted(reportId As String) Implements IFinanceReviewService.MarkScannedReceiptsDeleted
                MarkDeletedCalls += 1
            End Sub

            Public Sub ClearScannedReceiptAttachment(reportId As String) Implements IFinanceReviewService.ClearScannedReceiptAttachment
                ClearLegacyPathCalls += 1
            End Sub

            Public Function LoadQueue(statusFilter As String, receiptFilter As String, employeeFilter As String, dateFrom As Date?, dateTo As Date?, reportType As String) As List(Of FinanceErfQueueDto) Implements IFinanceReviewService.LoadQueue
                Throw New NotSupportedException()
            End Function

            Public Function GetDetail(reportId As String) As FinanceErfDetailDto Implements IFinanceReviewService.GetDetail
                Throw New NotSupportedException()
            End Function

            Public Sub MarkPhysicalReceiptsReceived(request As MarkPhysicalReceiptsReceivedDto) Implements IFinanceReviewService.MarkPhysicalReceiptsReceived
                Throw New NotSupportedException()
            End Sub

            Public Sub EnsureTrackingRowForApprovedReport(reportId As String) Implements IFinanceReviewService.EnsureTrackingRowForApprovedReport
                Throw New NotSupportedException()
            End Sub

            Public Function GetMissingPhysicalReceiptsForUser(userId As Integer) As List(Of MissingPhysicalReceiptDto) Implements IFinanceReviewService.GetMissingPhysicalReceiptsForUser
                Throw New NotSupportedException()
            End Function
        End Class
    End Class
End Namespace
