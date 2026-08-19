Option Strict On

Imports ERSystem.AppServices
Imports ERSystem.Domain
Imports ERSystem.Infrastructure.Data
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Services
    <TestClass>
    Public Class AccountSettingsServiceTests
        <TestMethod>
        Public Sub SaveTrimsAndPersistsOneReminderEmailAddress()
            Dim repository As New FakeRepository()
            Dim service As New AccountSettingsService(repository, New FakeSessionContext(), New FakeValueProtector())
            Dim account As AccountSettingsDto = CreateAccount()
            account.NotificationEmail = "  employee@example.com  "

            service.SaveCurrentAccount(account)

            Assert.IsNotNull(repository.SavedAccount)
            Assert.AreEqual("employee@example.com", repository.SavedAccount.NotificationEmail)
        End Sub

        <TestMethod>
        Public Sub SaveAllowsReminderEmailToRemainEmpty()
            Dim repository As New FakeRepository()
            Dim service As New AccountSettingsService(repository, New FakeSessionContext(), New FakeValueProtector())
            Dim account As AccountSettingsDto = CreateAccount()
            account.NotificationEmail = "   "

            service.SaveCurrentAccount(account)

            Assert.IsNull(repository.SavedAccount.NotificationEmail)
        End Sub

        <TestMethod>
        Public Sub SaveRejectsDisplayNameOrMultipleAddressInput()
            Dim repository As New FakeRepository()
            Dim service As New AccountSettingsService(repository, New FakeSessionContext(), New FakeValueProtector())
            Dim account As AccountSettingsDto = CreateAccount()
            account.NotificationEmail = "Employee <employee@example.com>"
            Dim failed As Boolean

            Try
                service.SaveCurrentAccount(account)
            Catch ex As InvalidOperationException
                failed = True
            End Try

            Assert.IsTrue(failed)
            Assert.IsNull(repository.SavedAccount)
        End Sub

        Private Shared Function CreateAccount() As AccountSettingsDto
            Return New AccountSettingsDto With {
                .Id = 1,
                .UserId = 7,
                .UserName = "EMPLOYEE",
                .FullName = "Employee User",
                .AuthorityRows = New List(Of UserAuthorityDto)()
            }
        End Function

        Private NotInheritable Class FakeRepository
            Implements IAccountSettingsRepository

            Public Property SavedAccount As AccountSettingsDto

            Public Function LoadDepartments() As List(Of DepartmentDto) Implements IAccountSettingsRepository.LoadDepartments
                Return New List(Of DepartmentDto)()
            End Function

            Public Function LoadAuthorityUsers() As List(Of AuthorityUserDto) Implements IAccountSettingsRepository.LoadAuthorityUsers
                Return New List(Of AuthorityUserDto)()
            End Function

            Public Function LoadAccountSettings(userId As Integer) As AccountSettingsDto Implements IAccountSettingsRepository.LoadAccountSettings
                Return If(SavedAccount, CreateAccount())
            End Function

            Public Sub SaveAccountSettings(account As AccountSettingsDto) Implements IAccountSettingsRepository.SaveAccountSettings
                SavedAccount = account
            End Sub

            Public Function LoadSessionAccount(userId As Integer) As AccountSettingsDto Implements IAccountSettingsRepository.LoadSessionAccount
                Return If(SavedAccount, CreateAccount())
            End Function
        End Class

        Private NotInheritable Class FakeSessionContext
            Implements IAccountSettingsSessionContext

            Public Function GetCurrentUserId() As Integer Implements IAccountSettingsSessionContext.GetCurrentUserId
                Return 7
            End Function

            Public Function GetCurrentPasswordValue() As String Implements IAccountSettingsSessionContext.GetCurrentPasswordValue
                Return "protected-password"
            End Function

            Public Sub SaveCurrentUserAccount(valueNames As String(), values As String()) Implements IAccountSettingsSessionContext.SaveCurrentUserAccount
            End Sub
        End Class

        Private NotInheritable Class FakeValueProtector
            Implements IAccountSettingsValueProtector

            Public Function Protect(value As String) As String Implements IAccountSettingsValueProtector.Protect
                Return "protected:" & value
            End Function

            Public Function Unprotect(value As String) As String Implements IAccountSettingsValueProtector.Unprotect
                Return value
            End Function
        End Class
    End Class
End Namespace
