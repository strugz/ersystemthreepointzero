Option Strict On

Namespace Infrastructure.Configuration
    Friend Class UserAccountRegistryProvider
        Private Const RootPath As String = "HKEY_CURRENT_USER\Software\ER System"
        Private Const UserAccountSubKey As String = "UserAccount"

        Public Sub Save(valueNames As String(), values As String())
            If valueNames Is Nothing Then
                Throw New ArgumentNullException(NameOf(valueNames))
            End If

            If values Is Nothing Then
                Throw New ArgumentNullException(NameOf(values))
            End If

            If valueNames.Length <> values.Length Then
                Throw New ArgumentException("Value names and values must have the same length.")
            End If

            Dim loader As New ClsLoadData()
            loader.RegistrySettings(RootPath, UserAccountSubKey, valueNames, values)
        End Sub

        Public Function GetValue(valueName As String) As String
            If String.IsNullOrWhiteSpace(valueName) Then
                Throw New ArgumentException("Value name is required.", NameOf(valueName))
            End If

            Dim loader As New ClsLoadData()
            Dim values As List(Of String) = loader.RegistryGetValue("Software\ER System\UserAccount", {valueName})

            If values Is Nothing OrElse values.Count = 0 Then
                Return String.Empty
            End If

            Return values(0)
        End Function
    End Class
End Namespace
