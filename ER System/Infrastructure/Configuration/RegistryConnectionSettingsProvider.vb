Option Strict On

Namespace Infrastructure.Configuration
    Friend Class RegistryConnectionSettingsProvider
        Private Const EncryptionKey As String = "crimsonmonastery2003"
        Private ReadOnly _encryption As clsEncryption

        Public Sub New()
            _encryption = New clsEncryption(EncryptionKey)
        End Sub

        Public Function Load() As ConnectionSettings
            Dim settings As New ConnectionSettings()

            settings.DbType = ReadValue([Shared].Utilities.RegistryKeys.DbTypeValueName, [Shared].Utilities.RegistryKeys.DefaultDbType)
            settings.Authentication = ReadValue([Shared].Utilities.RegistryKeys.AuthenticationValueName, [Shared].Utilities.RegistryKeys.WindowsAuthentication)
            settings.ServerName = ReadEncryptedValue([Shared].Utilities.RegistryKeys.ServerNameValueName)
            settings.Database = ReadEncryptedValue([Shared].Utilities.RegistryKeys.DatabaseValueName)

            If Not settings.UsesWindowsAuthentication Then
                settings.UserName = ReadEncryptedValue([Shared].Utilities.RegistryKeys.UserNameValueName)
                settings.Password = ReadEncryptedValue([Shared].Utilities.RegistryKeys.PasswordValueName)
            End If

            Return settings
        End Function

        Private Function ReadValue(valueName As String, defaultValue As String) As String
            Dim value As Object = My.Computer.Registry.GetValue([Shared].Utilities.RegistryKeys.ConnectionPath, valueName, defaultValue)

            If value Is Nothing Then
                Return defaultValue
            End If

            Return Convert.ToString(value)
        End Function

        Private Function ReadEncryptedValue(valueName As String) As String
            Dim encryptedValue As String = ReadValue(valueName, String.Empty)

            If String.IsNullOrEmpty(encryptedValue) Then
                Return String.Empty
            End If

            Return _encryption.DecryptData(encryptedValue)
        End Function
    End Class
End Namespace
