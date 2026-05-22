Option Strict On

Imports ERSystem.Domain
Imports ERSystem.Infrastructure.Data

Public Class AccountSettingsService
    Private Shared ReadOnly UserAccountValueNames As String() = {
        "UserID", "username", "Userlevel", "DeptID", "Fullname", "emp_Dept", "BreakFastRate",
        "LunchRate", "DinnerRate", "OTMeal", "TranspoRate", "Password", "Approver1", "Approver2"
    }

    Private ReadOnly _repository As IAccountSettingsRepository
    Private ReadOnly _sessionContext As IAccountSettingsSessionContext
    Private ReadOnly _valueProtector As IAccountSettingsValueProtector

    Public Sub New(repository As IAccountSettingsRepository,
                   sessionContext As IAccountSettingsSessionContext,
                   valueProtector As IAccountSettingsValueProtector)
        If repository Is Nothing Then
            Throw New ArgumentNullException("repository")
        End If

        If sessionContext Is Nothing Then
            Throw New ArgumentNullException("sessionContext")
        End If

        If valueProtector Is Nothing Then
            Throw New ArgumentNullException("valueProtector")
        End If

        _repository = repository
        _sessionContext = sessionContext
        _valueProtector = valueProtector
    End Sub

    Public Function LoadDepartments() As List(Of DepartmentDto)
        Return _repository.LoadDepartments()
    End Function

    Public Function LoadAuthorityUsers() As List(Of AuthorityUserDto)
        Return _repository.LoadAuthorityUsers()
    End Function

    Public Function LoadCurrentAccount() As AccountSettingsDto
        Dim userId As Integer = _sessionContext.GetCurrentUserId()
        Dim account As AccountSettingsDto = _repository.LoadAccountSettings(userId)
        account.EmailAdd = _valueProtector.Unprotect(account.EmailAdd)
        account.EmailPass = _valueProtector.Unprotect(account.EmailPass)
        Return account
    End Function

    Public Sub SaveCurrentAccount(account As AccountSettingsDto)
        If account Is Nothing Then
            Throw New ArgumentNullException("account")
        End If

        Dim currentUserId As Integer = _sessionContext.GetCurrentUserId()
        If account.UserId <> currentUserId Then
            Throw New InvalidOperationException("Account settings can only update the logged-in user.")
        End If

        Dim saveData As AccountSettingsDto = CopyForSave(account)
        saveData.EmailAdd = ProtectOptionalValue(account.EmailAdd)
        saveData.EmailPass = ProtectOptionalValue(account.EmailPass)

        _repository.SaveAccountSettings(saveData)
        RefreshCurrentUserRegistry(currentUserId)
    End Sub

    Private Function CopyForSave(account As AccountSettingsDto) As AccountSettingsDto
        Dim copy As New AccountSettingsDto With {
            .Id = account.Id,
            .UserId = account.UserId,
            .UserName = account.UserName,
            .FullName = account.FullName,
            .UserLevel = account.UserLevel,
            .DeptId = account.DeptId,
            .DepartmentName = account.DepartmentName,
            .EmailAdd = account.EmailAdd,
            .EmailPass = account.EmailPass,
            .EmailTo = account.EmailTo,
            .EmailBcc = account.EmailBcc,
            .Signature = account.Signature,
            .Position = account.Position,
            .Status = account.Status,
            .Approver1 = account.Approver1,
            .Approver2 = account.Approver2,
            .ReportNumberStatus = account.ReportNumberStatus,
            .WorkWithStatus = account.WorkWithStatus,
            .SuperApprover = account.SuperApprover,
            .TranspoRate = account.TranspoRate,
            .BreakFastRate = account.BreakFastRate,
            .LunchRate = account.LunchRate,
            .DinnerRate = account.DinnerRate,
            .OtMeal = account.OtMeal
        }

        For Each authority As UserAuthorityDto In account.AuthorityRows
            copy.AuthorityRows.Add(New UserAuthorityDto With {
                .Id = authority.Id,
                .UserId = account.UserId,
                .AuthorityId = authority.AuthorityId,
                .AuthorityName = authority.AuthorityName,
                .Sort = authority.Sort
            })
        Next

        Return copy
    End Function

    Private Sub RefreshCurrentUserRegistry(userId As Integer)
        Dim account As AccountSettingsDto = _repository.LoadSessionAccount(userId)
        Dim values As String() = {
            account.UserId.ToString(),
            If(account.UserName, String.Empty),
            If(account.UserLevel, String.Empty),
            If(account.DeptId.HasValue, account.DeptId.Value.ToString(), String.Empty),
            If(account.FullName, String.Empty),
            If(account.DepartmentName, String.Empty),
            NullableDoubleToString(account.BreakFastRate),
            NullableDoubleToString(account.LunchRate),
            NullableDoubleToString(account.DinnerRate),
            NullableDoubleToString(account.OtMeal),
            NullableDoubleToString(account.TranspoRate),
            _sessionContext.GetCurrentPasswordValue(),
            If(account.Approver1, String.Empty),
            If(account.Approver2, String.Empty)
        }

        _sessionContext.SaveCurrentUserAccount(UserAccountValueNames, values)
    End Sub

    Private Function ProtectOptionalValue(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then
            Return String.Empty
        End If

        Return _valueProtector.Protect(value.Trim())
    End Function

    Private Shared Function NullableDoubleToString(value As Nullable(Of Double)) As String
        If Not value.HasValue Then
            Return String.Empty
        End If

        Return value.Value.ToString()
    End Function
End Class
