Imports ERSystem.Core.Domain.Entities

Namespace Domain.Interfaces
    Public Interface IUserRepository
        Function GetByUsernameAndPassword(username As String, encodedPassword As String) As UserAccount
        Function GetById(userId As String) As UserAccount
        Sub AddUserAccount(user As UserAccount)
        Sub UpdateUserAccount(user As UserAccount)
        Sub UpdateLoginStatus(userId As String, loginStatus As String)
    End Interface
End Namespace
