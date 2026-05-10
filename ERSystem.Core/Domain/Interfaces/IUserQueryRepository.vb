Imports System.Data

Namespace Domain.Interfaces
    Public Interface IUserQueryRepository
        Function LoadDuplicateUser(username As String) As DataTable
        Function LoadDuplicateUserID(userid As String) As String
        Function LoadingOfficersToSign(userid As String) As String
        Function LoadingUserAccountEmail(userid As String, deptID As String) As DataTable
        Function LoadingUserAccountFiled(deptID As String, SignID As String) As DataTable
        Function LoadingUserAccount(deptID As String) As DataTable
        Function LoadingUserAccountPending(deptID As String) As DataTable
        Function LoadUserAccountAdmin() As DataTable
        Function LoadingUserAccDept(UserID As String) As DataTable
        Function LoginUserAccount(Username As String, Password As String) As DataTable
        Function LoadMaxUserID() As String
    End Interface
End Namespace