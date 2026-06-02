Option Strict On

Imports Microsoft.Win32

Namespace Global.ERSystem.Infrastructure.Configuration
    Public Class UserAccountRegistryProvider
        Public Sub Save(valueNames As String(), values As String())
            If valueNames Is Nothing Then
                Throw New ArgumentNullException("valueNames")
            End If

            If values Is Nothing Then
                Throw New ArgumentNullException("values")
            End If

            If valueNames.Length <> values.Length Then
                Throw New ArgumentException("Value names and values must have the same length.")
            End If

            Using key As RegistryKey = Registry.CurrentUser.CreateSubKey(RegistryKeys.UserAccountSubKey)
                If key Is Nothing Then
                    Throw New InvalidOperationException("Unable to open the user account registry key.")
                End If

                For index As Integer = 0 To valueNames.Length - 1
                    key.SetValue(valueNames(index), If(values(index), String.Empty))
                Next
            End Using
        End Sub

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
