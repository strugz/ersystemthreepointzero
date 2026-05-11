Namespace Infrastructure.Configuration
    Public Class RegistryConnectionSettingsProvider
        Private Const ConnectionRegistryPath As String = "HKEY_CURRENT_USER\Software\ER System\Connection"

        Private ReadOnly _encryption As ER_System.clsEncryption

        Public Sub New(ByVal encryption As ER_System.clsEncryption)
            _encryption = encryption
        End Sub

        Public Function Load() As ConnectionSettings
            Return New ConnectionSettings With {
                .DatabaseType = ConnectionSettings.SqlServerDatabaseType,
                .Authentication = ConnectionSettings.SqlServerAuthentication,
                .ServerName = ReadEncryptedRegistryValue("ServerName"),
                .DatabaseName = ReadEncryptedRegistryValue("Database"),
                .UserName = ReadEncryptedRegistryValue("UserName"),
                .Password = ReadEncryptedRegistryValue("Password")
            }
        End Function

        Private Function ReadRegistryValue(ByVal valueName As String, ByVal defaultValue As String) As String
            Return Convert.ToString(My.Computer.Registry.GetValue(ConnectionRegistryPath, valueName, defaultValue))
        End Function

        Private Function ReadEncryptedRegistryValue(ByVal valueName As String) As String
            Return _encryption.DecryptData(ReadRegistryValue(valueName, String.Empty))
        End Function
    End Class
End Namespace
