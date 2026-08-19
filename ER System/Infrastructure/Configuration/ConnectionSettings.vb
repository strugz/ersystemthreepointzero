Option Strict On

Namespace Infrastructure.Configuration
    Friend Class ConnectionSettings
        Public Property DbType As String = String.Empty
        Public Property Authentication As String = String.Empty
        Public Property ServerName As String = String.Empty
        Public Property Database As String = String.Empty
        Public Property UserName As String = String.Empty
        Public Property Password As String = String.Empty

        Public ReadOnly Property UsesWindowsAuthentication As Boolean
            Get
                Return String.Equals(Authentication, [Shared].Utilities.RegistryKeys.WindowsAuthentication, StringComparison.OrdinalIgnoreCase)
            End Get
        End Property
    End Class
End Namespace
