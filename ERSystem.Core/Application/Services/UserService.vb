Imports ERSystem.Core.Domain.Entities
Imports ERSystem.Core.Domain.Interfaces
Imports ERSystem.Core.Application.Interfaces
Imports ERSystem.Common.Interfaces

Namespace Application.Services
    Public Class UserService
        Implements IUserService

        Private ReadOnly _userRepository As IUserRepository
        Private ReadOnly _encryptionService As IEncryptionService

        Public Sub New(userRepository As IUserRepository, encryptionService As IEncryptionService)
            _userRepository = userRepository
            _encryptionService = encryptionService
        End Sub

        Public Function Authenticate(username As String, plainTextPassword As String) As UserAccount Implements IUserService.Authenticate
            Dim encodedPassword = _encryptionService.EncryptData(plainTextPassword)
            Return _userRepository.GetByUsernameAndPassword(username, encodedPassword)
        End Function

        Public Function GetUserDetails(userId As String) As UserAccount Implements IUserService.GetUserDetails
            Return _userRepository.GetById(userId)
        End Function

        Public Sub RegisterUser(user As UserAccount, plainTextPassword As String) Implements IUserService.RegisterUser
            user.Password = _encryptionService.EncryptData(plainTextPassword)
            _userRepository.AddUserAccount(user)
        End Sub

        Public Sub UpdateUser(user As UserAccount) Implements IUserService.UpdateUser
            _userRepository.UpdateUserAccount(user)
        End Sub

        Public Sub UpdateLoginStatus(userId As String, loginStatus As String) Implements IUserService.UpdateLoginStatus
            _userRepository.UpdateLoginStatus(userId, loginStatus)
        End Sub
    End Class
End Namespace
