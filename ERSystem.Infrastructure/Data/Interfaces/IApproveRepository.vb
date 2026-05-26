Option Strict On

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IApproveRepository
        Function LoadUserAccountFiled(deptId As String, signId As String) As DataTable
        Function LoadUserReportDetailsDone(userId As String, fileStatus As String, signId As String) As DataTable
        Function LoadUserReportDetailsFiled(userId As String, fileStatus As String, signId As String) As DataTable
    End Interface
End Namespace
