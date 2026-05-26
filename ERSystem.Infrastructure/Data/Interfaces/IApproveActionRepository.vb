Option Strict On

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IApproveActionRepository
        Sub UpdateFileStatus(userIdToApprover As String, reportIdToApprove As String, loginUserId As String)
        Function IsReportApprovedDone(reportId As String) As Boolean
    End Interface
End Namespace
