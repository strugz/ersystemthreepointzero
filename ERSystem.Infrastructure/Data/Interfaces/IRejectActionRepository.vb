Option Strict On

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IRejectActionRepository
        Sub RejectFiledReport(reportId As String, rejectNote As String, managerUserId As String)
    End Interface
End Namespace
