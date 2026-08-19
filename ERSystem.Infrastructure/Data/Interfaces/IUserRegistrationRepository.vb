Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IUserRegistrationRepository
        Function GetByUserId(userId As Integer) As UserRegistrationModel
        Function GetByUsername(username As String) As UserRegistrationModel
    End Interface
End Namespace
