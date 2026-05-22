Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class FinanceReviewService
        Implements IFinanceReviewService

        Private Const CompletedStatus As String = "Completed"

        Private ReadOnly _repository As IFinanceReviewRepository

        Public Sub New()
            Me.New(New FinanceReviewRepository())
        End Sub

        Public Sub New(repository As IFinanceReviewRepository)
            If repository Is Nothing Then
                Throw New ArgumentNullException("repository")
            End If

            _repository = repository
        End Sub

        Public Function LoadQueue(statusFilter As String,
                                  receiptFilter As String,
                                  employeeFilter As String,
                                  dateFrom As Nullable(Of Date),
                                  dateTo As Nullable(Of Date),
                                  reportType As String) As List(Of FinanceErfQueueDto) Implements IFinanceReviewService.LoadQueue
            Return _repository.GetQueue(statusFilter, receiptFilter, employeeFilter, dateFrom, dateTo, reportType)
        End Function

        Public Function GetDetail(reportId As String) As FinanceErfDetailDto Implements IFinanceReviewService.GetDetail
            Return _repository.GetDetail(reportId)
        End Function

        Public Sub MarkPhysicalReceiptsReceived(request As MarkPhysicalReceiptsReceivedDto) Implements IFinanceReviewService.MarkPhysicalReceiptsReceived
            If request Is Nothing Then
                Throw New ArgumentNullException("request")
            End If

            If String.IsNullOrWhiteSpace(request.ReportID) Then
                Throw New ArgumentException("Report ID is required.", "request")
            End If

            _repository.MarkPhysicalReceiptsReceived(request)
        End Sub

        Public Sub CompleteFinanceReview(request As CompleteFinanceReviewDto) Implements IFinanceReviewService.CompleteFinanceReview
            If request Is Nothing Then
                Throw New ArgumentNullException("request")
            End If

            If String.IsNullOrWhiteSpace(request.ReportID) Then
                Throw New ArgumentException("Report ID is required.", "request")
            End If

            Dim detail As FinanceErfDetailDto = _repository.GetDetail(request.ReportID)

            If detail Is Nothing Then
                Throw New InvalidOperationException("Finance tracking details were not found.")
            End If

            If String.Equals(detail.FinanceStatus, CompletedStatus, StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidOperationException("This ERF is already completed by Finance.")
            End If

            If Not detail.PhysicalReceiptsReceived Then
                Throw New InvalidOperationException("Physical receipts must be received before Finance can complete this ERF.")
            End If

            _repository.CompleteFinanceReview(request)
        End Sub

        Public Sub EnsureTrackingRowForApprovedReport(reportId As String) Implements IFinanceReviewService.EnsureTrackingRowForApprovedReport
            _repository.EnsureTrackingRowForApprovedReport(reportId)
        End Sub

        Public Sub MarkScannedReceiptsDeleted(reportId As String) Implements IFinanceReviewService.MarkScannedReceiptsDeleted
            _repository.MarkScannedReceiptsDeleted(reportId)
        End Sub

        Public Sub ClearScannedReceiptAttachment(reportId As String) Implements IFinanceReviewService.ClearScannedReceiptAttachment
            _repository.ClearScannedReceiptAttachment(reportId)
        End Sub

        Public Function GetMissingPhysicalReceiptsForUser(userId As Integer) As List(Of MissingPhysicalReceiptDto) Implements IFinanceReviewService.GetMissingPhysicalReceiptsForUser
            Return _repository.GetMissingPhysicalReceiptsForUser(userId)
        End Function
    End Class
End Namespace
