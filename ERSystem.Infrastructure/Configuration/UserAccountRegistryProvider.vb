Option Strict On

Imports Microsoft.Win32

Namespace Global.ERSystem.Infrastructure.Configuration
    Public Class UserAccountRegistryProvider
        Public Function GetValue(valueName As String) As String
            If String.IsNullOrWhiteSpace(valueName) Then
                Throw New ArgumentException("Value name is required.", "valueName")
            End If

            Using key As RegistryKey = Registry.CurrentUser.OpenSubKey(RegistryKeys.UserAccountSubKey)
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
End Namespace
