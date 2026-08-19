Option Strict On

Public Interface IAccountSettingsValueProtector
    Function Protect(value As String) As String
    Function Unprotect(value As String) As String
End Interface
