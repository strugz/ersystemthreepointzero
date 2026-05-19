Option Strict On

Namespace [Shared].Utilities
    Friend NotInheritable Class RegistryKeys
        Public Const ConnectionPath As String = "HKEY_CURRENT_USER\Software\ER System\Connection"
        Public Const DbTypeValueName As String = "DBType"
        Public Const AuthenticationValueName As String = "Authentication"
        Public Const ServerNameValueName As String = "ServerName"
        Public Const DatabaseValueName As String = "Database"
        Public Const UserNameValueName As String = "UserName"
        Public Const PasswordValueName As String = "Password"
        Public Const DefaultDbType As String = "Microsoft Access"
        Public Const SqlServerDbType As String = "Miscrosoft SQL Server"
        Public Const WindowsAuthentication As String = "Windows Authentication"
        Public Const PreviousExpenseDatabase As String = "ExpenseReportDB"

        Private Sub New()
        End Sub
    End Class
End Namespace
