Option Strict On

Imports Microsoft.Win32

Namespace Global.ERSystem.Infrastructure.Configuration
    Public Class RegistryConnectionSettingsProvider
        Private Const EncryptionKey As String = "crimsonmonastery2003"
        Private ReadOnly _protector As LegacyValueProtector

        Public Sub New()
            _protector = New LegacyValueProtector(EncryptionKey)
        End Sub

        Public Function Load() As ConnectionSettings
            Dim settings As New ConnectionSettings With {
                .DbType = ReadValue(RegistryKeys.ConnectionSubKey, RegistryKeys.DbTypeValueName, RegistryKeys.DefaultDbType),
                .Authentication = ReadValue(RegistryKeys.ConnectionSubKey, RegistryKeys.AuthenticationValueName, RegistryKeys.WindowsAuthentication),
                .ServerName = ReadEncryptedValue(RegistryKeys.ServerNameValueName),
                .Database = ReadEncryptedValue(RegistryKeys.DatabaseValueName)
            }

            If Not settings.UsesWindowsAuthentication Then
                settings.UserName = ReadEncryptedValue(RegistryKeys.UserNameValueName)
                settings.Password = ReadEncryptedValue(RegistryKeys.PasswordValueName)
            End If

            Return settings
        End Function

        Private Function ReadEncryptedValue(valueName As String) As String
            Dim encryptedValue As String = ReadValue(RegistryKeys.ConnectionSubKey, valueName, String.Empty)
            If String.IsNullOrEmpty(encryptedValue) Then
                Return String.Empty
            End If

            Return _protector.DecryptData(encryptedValue)
        End Function

        Private Shared Function ReadValue(subKey As String, valueName As String, defaultValue As String) As String
            Using key As RegistryKey = Registry.CurrentUser.OpenSubKey(subKey)
                If key Is Nothing Then
                    Return defaultValue
                End If

                Dim value As Object = key.GetValue(valueName, defaultValue)
                If value Is Nothing Then
                    Return defaultValue
                End If

                Return value.ToString()
            End Using
        End Function
    End Class
End Namespace
