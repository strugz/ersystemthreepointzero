Option Strict On

Imports System.Data
Imports ER_System.Domain.Entities

Namespace Application.Repositories
    Public Interface IUserAccountRepository
        Function Authenticate(ByVal username As String, ByVal encryptedPassword As String) As DataTable
        Function GetByUserId(ByVal userId As String) As UserAccount
        Sub UpdateLoginStatus(ByVal userId As String, ByVal loginStatus As String)
    End Interface
End Namespace
