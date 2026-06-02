Imports System.Data.Entity
Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class FinanceReviewRepository
        Implements IFinanceReviewRepository

        Private Const PendingStatus As String = "Pending"
        Private Const ReceiptsReceivedStatus As String = "Receipts Received"
        Private Const ApprovedFileStatus As String = "0"
        Private Const ApprovedPrintStatus As String = "0"

        Public Sub EnsureTrackingRowForApprovedReport(reportId As String) Implements IFinanceReviewRepository.EnsureTrackingRowForApprovedReport
            If String.IsNullOrWhiteSpace(reportId) Then
                Return
            End If

            Using dbContext As New AppDbContext()
                Dim report = dbContext.ReportsDetails.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.ID = reportId)

                If report Is Nothing OrElse Not IsApprovedDone(report) Then
                    Return
                End If

                Dim hasTrackingRow As Boolean = dbContext.ReportFinanceTrackings.
                    AsNoTracking().
                    Any(Function(finance) finance.ReportID = reportId)

                If hasTrackingRow Then
                    Return
                End If

                dbContext.ReportFinanceTrackings.Add(New ReportFinanceTrackingModel With {
                    .ReportID = reportId,
                    .FinanceStatus = PendingStatus,
                    .PhysicalReceiptsReceived = False
                })
                dbContext.SaveChanges()
            End Using
        End Sub

        Public Function GetQueue(statusFilter As String,
                                 receiptFilter As String,
                                 employeeFilter As String,
                                 dateFrom As Nullable(Of Date),
                                 dateTo As Nullable(Of Date),
                                 reportType As String) As List(Of FinanceErfQueueDto) Implements IFinanceReviewRepository.GetQueue
            Using dbContext As New AppDbContext()
                Dim reports As List(Of ReportDetailModel) = dbContext.ReportsDetails.
                    AsNoTracking().
                    Where(Function(report) report.ReportFileStatus = ApprovedFileStatus AndAlso report.ReportPrintStatus = ApprovedPrintStatus).
                    ToList()

                Dim finances As List(Of ReportFinanceTrackingModel) = dbContext.ReportFinanceTrackings.
                    AsNoTracking().
                    ToList()

                Dim cashAdvances As List(Of CashAdvanceModel) = dbContext.CashAdvances.
                    AsNoTracking().
                    ToList()

                Dim users As List(Of UserRegistrationModel) = dbContext.UserRegistrations.
                    AsNoTracking().
                    ToList()

                Dim query = From report In reports
                            Join finance In finances On report.ID Equals finance.ReportID
                            Group Join cashAdvance In cashAdvances On report.ID Equals cashAdvance.ReportID Into cashAdvanceGroup = Group
                            From cashAdvance In cashAdvanceGroup.DefaultIfEmpty()
                            Group Join user In users On report.UserID.GetValueOrDefault() Equals user.UserID.GetValueOrDefault() Into userGroup = Group
                            From user In userGroup.DefaultIfEmpty()
                            Select New With {
                                .Report = report,
                                .Finance = finance,
                                .CashAdvance = cashAdvance,
                                .User = user
                            }

                If Not String.IsNullOrWhiteSpace(statusFilter) AndAlso Not String.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase) Then
                    query = query.Where(Function(item) String.Equals(item.Finance.FinanceStatus, statusFilter, StringComparison.OrdinalIgnoreCase))
                End If

                If String.Equals(receiptFilter, "Missing", StringComparison.OrdinalIgnoreCase) Then
                    query = query.Where(Function(item) Not item.Finance.PhysicalReceiptsReceived)
                ElseIf String.Equals(receiptFilter, "Received", StringComparison.OrdinalIgnoreCase) Then
                    query = query.Where(Function(item) item.Finance.PhysicalReceiptsReceived)
                End If

                If Not String.IsNullOrWhiteSpace(employeeFilter) Then
                    query = query.Where(Function(item) item.User IsNot Nothing AndAlso
                        item.User.Fullname IsNot Nothing AndAlso
                        item.User.Fullname.IndexOf(employeeFilter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                End If

                If dateFrom.HasValue Then
                    query = query.Where(Function(item) item.Report.ReportDateFrom.HasValue AndAlso item.Report.ReportDateFrom.Value.Date >= dateFrom.Value.Date)
                End If

                If dateTo.HasValue Then
                    query = query.Where(Function(item) item.Report.ReportDateTo.HasValue AndAlso item.Report.ReportDateTo.Value.Date <= dateTo.Value.Date)
                End If

                If Not String.IsNullOrWhiteSpace(reportType) AndAlso Not String.Equals(reportType, "All", StringComparison.OrdinalIgnoreCase) Then
                    query = query.Where(Function(item) String.Equals(item.Report.ReportType, reportType, StringComparison.OrdinalIgnoreCase))
                End If

                Return query.
                    OrderByDescending(Function(item) item.Report.ReportDateFrom).
                    ThenBy(Function(item) If(item.User Is Nothing, String.Empty, item.User.Fullname)).
                    Select(Function(item) ToQueueDto(item.Report, item.Finance, item.CashAdvance, item.User)).
                    ToList()
            End Using
        End Function

        Public Function GetDetail(reportId As String) As FinanceErfDetailDto Implements IFinanceReviewRepository.GetDetail
            If String.IsNullOrWhiteSpace(reportId) Then
                Return Nothing
            End If

            EnsureTrackingRowForApprovedReport(reportId)

            Using dbContext As New AppDbContext()
                Dim report = dbContext.ReportsDetails.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.ID = reportId)

                If report Is Nothing Then
                    Return Nothing
                End If

                Dim finance = dbContext.ReportFinanceTrackings.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.ReportID = reportId)

                If finance Is Nothing Then
                    Return Nothing
                End If

                Dim cashAdvance = dbContext.CashAdvances.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.ReportID = reportId)

                Dim user As UserRegistrationModel = Nothing
                If report.UserID.HasValue Then
                    Dim userId As Integer = report.UserID.Value
                    user = dbContext.UserRegistrations.
                        AsNoTracking().
                        FirstOrDefault(Function(item) item.UserID.HasValue AndAlso item.UserID.Value = userId)
                End If

                Return ToDetailDto(report, finance, cashAdvance, user)
            End Using
        End Function

        Public Sub MarkPhysicalReceiptsReceived(request As MarkPhysicalReceiptsReceivedDto) Implements IFinanceReviewRepository.MarkPhysicalReceiptsReceived
            If request Is Nothing Then
                Throw New ArgumentNullException("request")
            End If

            EnsureTrackingRowForApprovedReport(request.ReportID)

            Using dbContext As New AppDbContext()
                Dim existing = dbContext.ReportFinanceTrackings.FirstOrDefault(Function(item) item.ReportID = request.ReportID)

                If existing Is Nothing Then
                    Throw New InvalidOperationException("Finance tracking details were not found.")
                End If

                existing.PhysicalReceiptsReceived = True
                existing.PhysicalReceiptsReceivedBy = request.ReviewerUserID
                existing.PhysicalReceiptsReceivedDate = DateTime.Now
                existing.FinanceStatus = ReceiptsReceivedStatus
                existing.FinanceRemarks = NormalizeRemarks(request.Remarks)
                dbContext.SaveChanges()
            End Using
        End Sub

        Public Sub MarkScannedReceiptsDeleted(reportId As String) Implements IFinanceReviewRepository.MarkScannedReceiptsDeleted
            If String.IsNullOrWhiteSpace(reportId) Then
                Return
            End If

            EnsureTrackingRowForApprovedReport(reportId)

            Using dbContext As New AppDbContext()
                Dim existing = dbContext.ReportFinanceTrackings.FirstOrDefault(Function(item) item.ReportID = reportId)

                If existing Is Nothing Then
                    Return
                End If

                If Not existing.ScannedReceiptsDeletedDate.HasValue Then
                    existing.ScannedReceiptsDeletedDate = DateTime.Now
                    dbContext.SaveChanges()
                End If
            End Using
        End Sub

        Public Sub ClearScannedReceiptAttachment(reportId As String) Implements IFinanceReviewRepository.ClearScannedReceiptAttachment
            If String.IsNullOrWhiteSpace(reportId) Then
                Return
            End If

            Using dbContext As New AppDbContext()
                Dim existing = dbContext.ReportsDetails.FirstOrDefault(Function(item) item.ID = reportId)

                If existing Is Nothing Then
                    Return
                End If

                existing.ReportAttachment = String.Empty
                dbContext.SaveChanges()
            End Using
        End Sub

        Public Function GetMissingPhysicalReceiptsForUser(userId As Integer) As List(Of MissingPhysicalReceiptDto) Implements IFinanceReviewRepository.GetMissingPhysicalReceiptsForUser
            Using dbContext As New AppDbContext()
                Dim reports As List(Of ReportDetailModel) = dbContext.ReportsDetails.
                    AsNoTracking().
                    Where(Function(report) report.UserID.HasValue AndAlso
                        report.UserID.Value = userId AndAlso
                        report.ReportFileStatus = ApprovedFileStatus AndAlso
                        report.ReportPrintStatus = ApprovedPrintStatus).
                    ToList()

                Dim finances As List(Of ReportFinanceTrackingModel) = dbContext.ReportFinanceTrackings.
                    AsNoTracking().
                    Where(Function(finance) Not finance.PhysicalReceiptsReceived).
                    ToList()

                Return (From report In reports
                        Join finance In finances On report.ID Equals finance.ReportID
                        Order By report.ReportDateFrom Descending
                        Select ToMissingReceiptDto(report, finance)).
                    ToList()
            End Using
        End Function

        Private Shared Function IsApprovedDone(report As ReportDetailModel) As Boolean
            Return report IsNot Nothing AndAlso
                String.Equals(report.ReportFileStatus, ApprovedFileStatus, StringComparison.Ordinal) AndAlso
                String.Equals(report.ReportPrintStatus, ApprovedPrintStatus, StringComparison.Ordinal)
        End Function

        Private Shared Function ToQueueDto(report As ReportDetailModel,
                                           finance As ReportFinanceTrackingModel,
                                           cashAdvance As CashAdvanceModel,
                                           user As UserRegistrationModel) As FinanceErfQueueDto
            Return New FinanceErfQueueDto With {
                .ReportID = report.ID,
                .UserID = report.UserID,
                .Username = If(user Is Nothing, String.Empty, user.Username),
                .EmployeeName = If(user Is Nothing, String.Empty, user.Fullname),
                .ReportDateFrom = report.ReportDateFrom,
                .ReportDateTo = report.ReportDateTo,
                .ReportDescription = report.ReportDescription,
                .ReportType = report.ReportType,
                .ERFReferenceNo = ResolveErfReferenceNo(report, cashAdvance),
                .CashRefNo = If(cashAdvance Is Nothing, String.Empty, cashAdvance.CashRefNo),
                .FinanceStatus = finance.FinanceStatus,
                .PhysicalReceiptsReceived = finance.PhysicalReceiptsReceived,
                .PhysicalReceiptsReceivedDate = finance.PhysicalReceiptsReceivedDate,
                .FinanceRemarks = finance.FinanceRemarks
            }
        End Function

        Private Shared Function ToDetailDto(report As ReportDetailModel,
                                            finance As ReportFinanceTrackingModel,
                                            cashAdvance As CashAdvanceModel,
                                            user As UserRegistrationModel) As FinanceErfDetailDto
            Return New FinanceErfDetailDto With {
                .ReportID = report.ID,
                .UserID = report.UserID,
                .Username = If(user Is Nothing, String.Empty, user.Username),
                .EmployeeName = If(user Is Nothing, String.Empty, user.Fullname),
                .ReportDateFrom = report.ReportDateFrom,
                .ReportDateTo = report.ReportDateTo,
                .ReportDescription = report.ReportDescription,
                .ReportType = report.ReportType,
                .ERFReferenceNo = ResolveErfReferenceNo(report, cashAdvance),
                .CashAmount = If(cashAdvance Is Nothing, Nothing, cashAdvance.CashAmount),
                .CashDate = If(cashAdvance Is Nothing, String.Empty, cashAdvance.CashDate),
                .CashRefDoc = If(cashAdvance Is Nothing, String.Empty, cashAdvance.CashRefDoc),
                .CashRefNo = If(cashAdvance Is Nothing, String.Empty, cashAdvance.CashRefNo),
                .RevolvingFund = If(cashAdvance Is Nothing, String.Empty, cashAdvance.RevolvingFund),
                .ReportAttachment = report.ReportAttachment,
                .FinanceStatus = finance.FinanceStatus,
                .PhysicalReceiptsReceived = finance.PhysicalReceiptsReceived,
                .PhysicalReceiptsReceivedBy = finance.PhysicalReceiptsReceivedBy,
                .PhysicalReceiptsReceivedDate = finance.PhysicalReceiptsReceivedDate,
                .FinanceRemarks = finance.FinanceRemarks,
                .ScannedReceiptsDeletedDate = finance.ScannedReceiptsDeletedDate
            }
        End Function

        Private Shared Function ResolveErfReferenceNo(report As ReportDetailModel, cashAdvance As CashAdvanceModel) As String
            If report IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(report.ERFReferenceNo) Then
                Return report.ERFReferenceNo.Trim()
            End If

            If cashAdvance IsNot Nothing AndAlso IsGeneratedErfReference(cashAdvance.CashRefNo) Then
                Return cashAdvance.CashRefNo.Trim()
            End If

            Return String.Empty
        End Function

        Private Shared Function IsGeneratedErfReference(value As String) As Boolean
            Return Not String.IsNullOrWhiteSpace(value) AndAlso value.Trim().StartsWith("ER-", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function ToMissingReceiptDto(report As ReportDetailModel, finance As ReportFinanceTrackingModel) As MissingPhysicalReceiptDto
            Return New MissingPhysicalReceiptDto With {
                .ReportID = report.ID,
                .ReportDescription = report.ReportDescription,
                .ReportDateFrom = report.ReportDateFrom,
                .ReportDateTo = report.ReportDateTo,
                .FinanceStatus = finance.FinanceStatus
            }
        End Function

        Private Shared Function NormalizeRemarks(value As String) As String
            If String.IsNullOrWhiteSpace(value) Then
                Return Nothing
            End If

            Return value.Trim()
        End Function
    End Class
End Namespace
