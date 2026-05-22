Option Strict On

Namespace AppServices
    Friend Class ApproveActionService
        Private ReadOnly _approveActionRepository As Infrastructure.Data.Repositories.ApproveActionRepository
        Private ReadOnly _approveService As ApproveService
        Private ReadOnly _userAccountRegistryProvider As Infrastructure.Configuration.UserAccountRegistryProvider
        Private ReadOnly _loader As ClsLoadData
        Private ReadOnly _financeReviewService As ERSystem.Infrastructure.Data.IFinanceReviewService
        Private ReadOnly _scannedReceiptCleanupService As ScannedReceiptCleanupService

        Public Sub New()
            _approveActionRepository = New Infrastructure.Data.Repositories.ApproveActionRepository()
            _approveService = New ApproveService()
            _userAccountRegistryProvider = New Infrastructure.Configuration.UserAccountRegistryProvider()
            _loader = New ClsLoadData()
            _financeReviewService = New ERSystem.Infrastructure.Data.FinanceReviewService()
            _scannedReceiptCleanupService = New ScannedReceiptCleanupService()
        End Sub

        Public Function ApproveReport(userIdToApprover As String, reportIdToApprove As String) As ApproveActionResult
            Dim loginUserId As String = _userAccountRegistryProvider.GetValue("UserID")
            Dim approverValidate As String = _loader.ApproverValidation(userIdToApprover, loginUserId, reportIdToApprove)

            If String.Equals(approverValidate, "True", StringComparison.Ordinal) Then
                _approveActionRepository.UpdateFileStatus(userIdToApprover, reportIdToApprove, loginUserId)

                If _approveActionRepository.IsReportApprovedDone(reportIdToApprove) Then
                    Try
                        _financeReviewService.EnsureTrackingRowForApprovedReport(reportIdToApprove)
                        _scannedReceiptCleanupService.DeleteScannedReceiptsForApprovedReport(reportIdToApprove)
                    Catch ex As Exception
                        Debug.WriteLine("Finance tracking or scanned receipt cleanup failed: " & ex.Message)
                    End Try
                End If

                Dim reportDetailsResult As ApproveReportDetailsLoadResult = _approveService.LoadReportDetails(userIdToApprover)
                Dim userAccountsResult As ApproveUserAccountLoadResult = _approveService.LoadUserAccounts()

                Return New ApproveActionResult With {
                    .IsSuccess = True,
                    .Message = "Expense Report Verified",
                    .ReportDetails = reportDetailsResult.ReportDetails,
                    .UserAccounts = userAccountsResult.Users,
                    .ShowNumberOfFile = userAccountsResult.ShowNumberOfFile
                }
            End If

            If String.Equals(approverValidate, "Done", StringComparison.Ordinal) Then
                Return New ApproveActionResult With {
                    .IsSuccess = False,
                    .Message = "Already Confirmed"
                }
            End If

            Return New ApproveActionResult With {
                .IsSuccess = False,
                .Message = "Not Yet Verified by the Precedent Approver"
            }
        End Function
    End Class
End Namespace
