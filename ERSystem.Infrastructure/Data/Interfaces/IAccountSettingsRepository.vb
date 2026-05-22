Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IAccountSettingsRepository
        Function LoadDepartments() As List(Of DepartmentDto)
        Function LoadAuthorityUsers() As List(Of AuthorityUserDto)
        Function LoadAccountSettings(userId As Integer) As AccountSettingsDto
        Sub SaveAccountSettings(account As AccountSettingsDto)
        Function LoadSessionAccount(userId As Integer) As AccountSettingsDto
    End Interface
End Namespace
