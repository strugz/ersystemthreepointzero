Option Strict On

Imports System.Data
Imports ER_System.Domain.Entities
Imports ER_System.Application.Repositories

Namespace Application.Services
    Public Class UserAccountService
        Private ReadOnly _userAccountRepository As IUserAccountRepository

        Public Sub New(ByVal userAccountRepository As IUserAccountRepository)
            _userAccountRepository = userAccountRepository
        End Sub

        Public Function Authenticate(ByVal username As String, ByVal encryptedPassword As String) As DataTable
            Return _userAccountRepository.Authenticate(username, encryptedPassword)
        End Function

        Public Function GetByUserId(ByVal userId As String) As UserAccount
            Return _userAccountRepository.GetByUserId(userId)
        End Function

        Public Sub UpdateLoginStatus(ByVal userId As String, ByVal loginStatus As String)
            _userAccountRepository.UpdateLoginStatus(userId, loginStatus)
        End Sub
    End Class
End Namespace
