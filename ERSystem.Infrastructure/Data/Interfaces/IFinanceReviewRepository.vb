Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IFinanceReviewRepository
        Sub EnsureTrackingRowsForApprovedReports()
        Sub EnsureTrackingRowForApprovedReport(reportId As String)
        Function GetQueue(statusFilter As String,
                          receiptFilter As String,
                          employeeFilter As String,
                          dateFrom As Nullable(Of Date),
                          dateTo As Nullable(Of Date),
                          reportType As String) As List(Of FinanceErfQueueDto)
        Function GetDetail(reportId As String) As FinanceErfDetailDto
        Sub MarkPhysicalReceiptsReceived(request As MarkPhysicalReceiptsReceivedDto)
        Sub CompleteFinanceReview(request As CompleteFinanceReviewDto)
        Sub MarkScannedReceiptsDeleted(reportId As String)
        Sub ClearScannedReceiptAttachment(reportId As String)
        Function GetMissingPhysicalReceiptsForUser(userId As Integer) As List(Of MissingPhysicalReceiptDto)
    End Interface
End Namespace
