Option Strict On

Namespace Global.ERSystem.Infrastructure.Configuration
    Public NotInheritable Class RegistryKeys
        Public Const ConnectionSubKey As String = "Software\ER System\Connection"
        Public Const SettingsSubKey As String = "Software\ER System\Settings"
        Public Const UserAccountSubKey As String = "Software\ER System\UserAccount"
        Public Const DbTypeValueName As String = "DBType"
        Public Const AuthenticationValueName As String = "Authentication"
        Public Const ServerNameValueName As String = "ServerName"
        Public Const DatabaseValueName As String = "Database"
        Public Const UserNameValueName As String = "UserName"
        Public Const PasswordValueName As String = "Password"
        Public Const DefaultDbType As String = "Microsoft Access"
        Public Const WindowsAuthentication As String = "Windows Authentication"

        Private Sub New()
        End Sub
    End Class
End Namespace
