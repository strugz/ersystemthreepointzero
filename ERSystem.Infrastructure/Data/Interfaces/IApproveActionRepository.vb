Option Strict On

Imports ERSystem.Domain.Approval

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IApproveActionRepository
        Function ValidateApproval(reportId As String, managerUserId As String) As ApprovalValidationStatus
        Sub UpdateFileStatus(userIdToApprover As String, reportIdToApprove As String, loginUserId As String)
        Function IsReportApprovedDone(reportId As String) As Boolean
    End Interface
End Namespace
