Imports ERSystem.Core.Domain.Entities

Namespace Application.Interfaces
    Public Interface IUserService
        Function Authenticate(username As String, plainTextPassword As String) As UserAccount
        Function GetUserDetails(userId As String) As UserAccount
        Sub RegisterUser(user As UserAccount, plainTextPassword As String)
        Sub UpdateUser(user As UserAccount)
        Sub UpdateLoginStatus(userId As String, loginStatus As String)
    End Interface
End Namespace
