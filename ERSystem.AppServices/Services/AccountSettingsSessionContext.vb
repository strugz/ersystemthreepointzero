Option Strict On

Imports Microsoft.Win32

Public Class AccountSettingsSessionContext
    Implements IAccountSettingsSessionContext

    Private Const UserAccountSubKey As String = "Software\ER System\UserAccount"

    Public Function GetCurrentUserId() As Integer Implements IAccountSettingsSessionContext.GetCurrentUserId
        Dim userIdValue As String = GetValue("UserID")
        Dim userId As Integer
        If Not Integer.TryParse(userIdValue, userId) Then
            Throw New InvalidOperationException("The current user is not available in account settings.")
        End If

        Return userId
    End Function

    Public Function GetCurrentPasswordValue() As String Implements IAccountSettingsSessionContext.GetCurrentPasswordValue
        Return GetValue("Password")
    End Function

    Public Sub SaveCurrentUserAccount(valueNames As String(), values As String()) Implements IAccountSettingsSessionContext.SaveCurrentUserAccount
        If valueNames Is Nothing Then
            Throw New ArgumentNullException("valueNames")
        End If

        If values Is Nothing Then
            Throw New ArgumentNullException("values")
        End If

        If valueNames.Length <> values.Length Then
            Throw New ArgumentException("Value names and values must have the same length.")
        End If

        Using key As RegistryKey = Registry.CurrentUser.CreateSubKey(UserAccountSubKey)
            If key Is Nothing Then
                Throw New InvalidOperationException("Unable to open the user account registry key.")
            End If

            For index As Integer = 0 To valueNames.Length - 1
                key.SetValue(valueNames(index), If(values(index), String.Empty), RegistryValueKind.String)
            Next
        End Using
    End Sub

    Private Shared Function GetValue(valueName As String) As String
        If String.IsNullOrWhiteSpace(valueName) Then
            Throw New ArgumentException("Value name is required.", "valueName")
        End If

        Using key As RegistryKey = Registry.CurrentUser.OpenSubKey(UserAccountSubKey)
            If key Is Nothing Then
                Return String.Empty
            End If

            Dim value As Object = key.GetValue(valueName, String.Empty)
            If value Is Nothing Then
                Return String.Empty
            End If

            Return value.ToString()
        End Using
    End Function
End Class
