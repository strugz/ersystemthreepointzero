Option Strict On

Public Interface IAccountSettingsSessionContext
    Function GetCurrentUserId() As Integer
    Function GetCurrentPasswordValue() As String
    Sub SaveCurrentUserAccount(valueNames As String(), values As String())
End Interface
