Option Strict On

Imports ERSystem.Domain.Approval
Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data

Public Class ApproveActionService
    Private ReadOnly _approveActionRepository As IApproveActionRepository
    Private ReadOnly _approveService As ApproveService
    Private ReadOnly _userAccountRegistryProvider As UserAccountRegistryProvider
    Private ReadOnly _financeReviewService As IFinanceReviewService
    Private ReadOnly _scannedReceiptCleanupService As ScannedReceiptCleanupService

    Public Sub New(approveActionRepository As IApproveActionRepository,
                   approveService As ApproveService,
                   userAccountRegistryProvider As UserAccountRegistryProvider,
                   financeReviewService As IFinanceReviewService,
                   scannedReceiptCleanupService As ScannedReceiptCleanupService)
        If approveActionRepository Is Nothing Then
            Throw New ArgumentNullException("approveActionRepository")
        End If

        If approveService Is Nothing Then
            Throw New ArgumentNullException("approveService")
        End If

        If userAccountRegistryProvider Is Nothing Then
            Throw New ArgumentNullException("userAccountRegistryProvider")
        End If

        If financeReviewService Is Nothing Then
            Throw New ArgumentNullException("financeReviewService")
        End If

        If scannedReceiptCleanupService Is Nothing Then
            Throw New ArgumentNullException("scannedReceiptCleanupService")
        End If

        _approveActionRepository = approveActionRepository
        _approveService = approveService
        _userAccountRegistryProvider = userAccountRegistryProvider
        _financeReviewService = financeReviewService
        _scannedReceiptCleanupService = scannedReceiptCleanupService
    End Sub

    Public Function ApproveReport(userIdToApprover As String, reportIdToApprove As String) As ApproveActionResult
        Dim loginUserId As String = _userAccountRegistryProvider.GetValue("UserID")
        Dim approverValidate As ApprovalValidationStatus = _approveActionRepository.ValidateApproval(reportIdToApprove, loginUserId)

        If approverValidate = ApprovalValidationStatus.CanApprove Then
            _approveActionRepository.UpdateFileStatus(userIdToApprover, reportIdToApprove, loginUserId)

            If _approveActionRepository.IsReportApprovedDone(reportIdToApprove) Then
                Try
                    _financeReviewService.EnsureTrackingRowForApprovedReport(reportIdToApprove)
                    _scannedReceiptCleanupService.FinalizeScannedReceiptsForApprovedReport(reportIdToApprove)
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

        If approverValidate = ApprovalValidationStatus.AlreadyConfirmed Then
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
