Imports System.Data.SqlClient
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class FinanceReviewRepository
        Implements IFinanceReviewRepository

        Private Const PendingStatus As String = "Pending"
        Private Const CompletedStatus As String = "Completed"

        Public Sub EnsureTrackingRowsForApprovedReports() Implements IFinanceReviewRepository.EnsureTrackingRowsForApprovedReports
            Using dbContext As New AppDbContext()
                dbContext.Database.ExecuteSqlCommand(
                    "INSERT INTO dbo.tbReportFinanceTracking (ReportID) " &
                    "SELECT report.ID " &
                    "FROM dbo.tbReportDetails report " &
                    "WHERE report.ReportFileStatus = '0' " &
                    "AND report.ReportPrintStatus = '0' " &
                    "AND NOT EXISTS (SELECT 1 FROM dbo.tbReportFinanceTracking finance WHERE finance.ReportID = report.ID)")
            End Using
        End Sub

        Public Sub EnsureTrackingRowForApprovedReport(reportId As String) Implements IFinanceReviewRepository.EnsureTrackingRowForApprovedReport
            If String.IsNullOrWhiteSpace(reportId) Then
                Return
            End If

            Using dbContext As New AppDbContext()
                dbContext.Database.ExecuteSqlCommand(
                    "INSERT INTO dbo.tbReportFinanceTracking (ReportID) " &
                    "SELECT report.ID " &
                    "FROM dbo.tbReportDetails report " &
                    "WHERE report.ID = @ReportID " &
                    "AND report.ReportFileStatus = '0' " &
                    "AND report.ReportPrintStatus = '0' " &
                    "AND NOT EXISTS (SELECT 1 FROM dbo.tbReportFinanceTracking finance WHERE finance.ReportID = report.ID)",
                    New SqlParameter("@ReportID", reportId))
            End Using
        End Sub

        Public Function GetQueue(statusFilter As String,
                                 receiptFilter As String,
                                 employeeFilter As String,
                                 dateFrom As Nullable(Of Date),
                                 dateTo As Nullable(Of Date),
                                 reportType As String) As List(Of FinanceErfQueueDto) Implements IFinanceReviewRepository.GetQueue
            EnsureTrackingRowsForApprovedReports()

            Dim parameters As New List(Of SqlParameter)()
            Dim sql As String =
                "SELECT report.ID AS ReportID, report.UserID, users.Fullname AS EmployeeName, " &
                "report.ReportDateFrom, report.ReportDateTo, report.ReportDescription, report.ReportType, " &
                "cash.CashRefNo, finance.FinanceStatus, finance.PhysicalReceiptsReceived, " &
                "finance.PhysicalReceiptsReceivedDate, finance.FinanceCompletedDate, finance.FinanceRemarks " &
                "FROM dbo.tbReportDetails report " &
                "INNER JOIN dbo.tbReportFinanceTracking finance ON report.ID = finance.ReportID " &
                "LEFT JOIN dbo.tbUserRegistration users ON report.UserID = users.UserID " &
                "LEFT JOIN dbo.tbCashAdvance cash ON report.ID = cash.ReportID " &
                "WHERE report.ReportFileStatus = '0' AND report.ReportPrintStatus = '0' "

            If Not String.IsNullOrWhiteSpace(statusFilter) AndAlso Not String.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase) Then
                sql &= "AND finance.FinanceStatus = @StatusFilter "
                parameters.Add(New SqlParameter("@StatusFilter", statusFilter))
            End If

            If String.Equals(receiptFilter, "Missing", StringComparison.OrdinalIgnoreCase) Then
                sql &= "AND finance.PhysicalReceiptsReceived = 0 "
            ElseIf String.Equals(receiptFilter, "Received", StringComparison.OrdinalIgnoreCase) Then
                sql &= "AND finance.PhysicalReceiptsReceived = 1 "
            End If

            If Not String.IsNullOrWhiteSpace(employeeFilter) Then
                sql &= "AND users.Fullname LIKE @EmployeeFilter "
                parameters.Add(New SqlParameter("@EmployeeFilter", "%" & employeeFilter.Trim() & "%"))
            End If

            If dateFrom.HasValue Then
                sql &= "AND report.ReportDateFrom >= @DateFrom "
                parameters.Add(New SqlParameter("@DateFrom", dateFrom.Value.Date))
            End If

            If dateTo.HasValue Then
                sql &= "AND report.ReportDateTo <= @DateTo "
                parameters.Add(New SqlParameter("@DateTo", dateTo.Value.Date))
            End If

            If Not String.IsNullOrWhiteSpace(reportType) AndAlso Not String.Equals(reportType, "All", StringComparison.OrdinalIgnoreCase) Then
                sql &= "AND report.ReportType = @ReportType "
                parameters.Add(New SqlParameter("@ReportType", reportType))
            End If

            sql &= "ORDER BY report.ReportDateFrom DESC, users.Fullname ASC"

            Using dbContext As New AppDbContext()
                Return dbContext.Database.SqlQuery(Of FinanceErfQueueDto)(sql, parameters.ToArray()).ToList()
            End Using
        End Function

        Public Function GetDetail(reportId As String) As FinanceErfDetailDto Implements IFinanceReviewRepository.GetDetail
            If String.IsNullOrWhiteSpace(reportId) Then
                Return Nothing
            End If

            EnsureTrackingRowForApprovedReport(reportId)

            Using dbContext As New AppDbContext()
                Return dbContext.Database.SqlQuery(Of FinanceErfDetailDto)(
                    "SELECT report.ID AS ReportID, report.UserID, users.Fullname AS EmployeeName, " &
                    "report.ReportDateFrom, report.ReportDateTo, report.ReportDescription, report.ReportType, " &
                    "cash.CashAmount, cash.CashDate, cash.CashRefDoc, cash.CashRefNo, cash.RevolvingFund, " &
                    "report.ReportAttachment, finance.FinanceStatus, finance.PhysicalReceiptsReceived, " &
                    "finance.PhysicalReceiptsReceivedBy, finance.PhysicalReceiptsReceivedDate, " &
                    "finance.FinanceCompletedBy, finance.FinanceCompletedDate, finance.FinanceRemarks, " &
                    "finance.ScannedReceiptsDeletedDate " &
                    "FROM dbo.tbReportDetails report " &
                    "INNER JOIN dbo.tbReportFinanceTracking finance ON report.ID = finance.ReportID " &
                    "LEFT JOIN dbo.tbUserRegistration users ON report.UserID = users.UserID " &
                    "LEFT JOIN dbo.tbCashAdvance cash ON report.ID = cash.ReportID " &
                    "WHERE report.ID = @ReportID",
                    New SqlParameter("@ReportID", reportId)).
                    FirstOrDefault()
            End Using
        End Function

        Public Sub MarkPhysicalReceiptsReceived(request As MarkPhysicalReceiptsReceivedDto) Implements IFinanceReviewRepository.MarkPhysicalReceiptsReceived
            If request Is Nothing Then
                Throw New ArgumentNullException("request")
            End If

            EnsureTrackingRowForApprovedReport(request.ReportID)

            Using dbContext As New AppDbContext()
                dbContext.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tbReportFinanceTracking " &
                    "SET PhysicalReceiptsReceived = 1, " &
                    "PhysicalReceiptsReceivedBy = @ReviewerUserID, " &
                    "PhysicalReceiptsReceivedDate = GETDATE(), " &
                    "FinanceRemarks = @Remarks " &
                    "WHERE ReportID = @ReportID",
                    New SqlParameter("@ReviewerUserID", request.ReviewerUserID),
                    New SqlParameter("@Remarks", ToDbNullable(request.Remarks)),
                    New SqlParameter("@ReportID", request.ReportID))
            End Using
        End Sub

        Public Sub CompleteFinanceReview(request As CompleteFinanceReviewDto) Implements IFinanceReviewRepository.CompleteFinanceReview
            If request Is Nothing Then
                Throw New ArgumentNullException("request")
            End If

            EnsureTrackingRowForApprovedReport(request.ReportID)

            Using dbContext As New AppDbContext()
                dbContext.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tbReportFinanceTracking " &
                    "SET FinanceStatus = @CompletedStatus, " &
                    "FinanceCompletedBy = @ReviewerUserID, " &
                    "FinanceCompletedDate = GETDATE(), " &
                    "FinanceRemarks = @Remarks " &
                    "WHERE ReportID = @ReportID AND PhysicalReceiptsReceived = 1",
                    New SqlParameter("@CompletedStatus", CompletedStatus),
                    New SqlParameter("@ReviewerUserID", request.ReviewerUserID),
                    New SqlParameter("@Remarks", ToDbNullable(request.Remarks)),
                    New SqlParameter("@ReportID", request.ReportID))
            End Using
        End Sub

        Public Sub MarkScannedReceiptsDeleted(reportId As String) Implements IFinanceReviewRepository.MarkScannedReceiptsDeleted
            If String.IsNullOrWhiteSpace(reportId) Then
                Return
            End If

            EnsureTrackingRowForApprovedReport(reportId)

            Using dbContext As New AppDbContext()
                dbContext.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tbReportFinanceTracking " &
                    "SET ScannedReceiptsDeletedDate = GETDATE() " &
                    "WHERE ReportID = @ReportID AND ScannedReceiptsDeletedDate IS NULL",
                    New SqlParameter("@ReportID", reportId))
            End Using
        End Sub

        Public Sub ClearScannedReceiptAttachment(reportId As String) Implements IFinanceReviewRepository.ClearScannedReceiptAttachment
            If String.IsNullOrWhiteSpace(reportId) Then
                Return
            End If

            Using dbContext As New AppDbContext()
                dbContext.Database.ExecuteSqlCommand(
                    "UPDATE dbo.tbReportDetails SET ReportAttachment = '' WHERE ID = @ReportID",
                    New SqlParameter("@ReportID", reportId))
            End Using
        End Sub

        Public Function GetMissingPhysicalReceiptsForUser(userId As Integer) As List(Of MissingPhysicalReceiptDto) Implements IFinanceReviewRepository.GetMissingPhysicalReceiptsForUser
            EnsureTrackingRowsForApprovedReports()

            Using dbContext As New AppDbContext()
                Return dbContext.Database.SqlQuery(Of MissingPhysicalReceiptDto)(
                    "SELECT report.ID AS ReportID, report.ReportDescription, report.ReportDateFrom, report.ReportDateTo, finance.FinanceStatus " &
                    "FROM dbo.tbReportDetails report " &
                    "INNER JOIN dbo.tbReportFinanceTracking finance ON report.ID = finance.ReportID " &
                    "WHERE report.UserID = @UserID " &
                    "AND report.ReportFileStatus = '0' " &
                    "AND report.ReportPrintStatus = '0' " &
                    "AND finance.PhysicalReceiptsReceived = 0 " &
                    "AND finance.FinanceStatus <> @CompletedStatus " &
                    "ORDER BY report.ReportDateFrom DESC",
                    New SqlParameter("@UserID", userId),
                    New SqlParameter("@CompletedStatus", CompletedStatus)).
                    ToList()
            End Using
        End Function

        Private Shared Function ToDbNullable(value As String) As Object
            If String.IsNullOrWhiteSpace(value) Then
                Return DBNull.Value
            End If

            Return value.Trim()
        End Function
    End Class
End Namespace
