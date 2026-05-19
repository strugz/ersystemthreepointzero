Option Strict On

Imports System.Data.SqlClient

Namespace Infrastructure.Data.Repositories
    Friend Class ApproveRepository
        Private ReadOnly _settingsProvider As Infrastructure.Configuration.RegistryConnectionSettingsProvider
        Private ReadOnly _connectionFactory As Infrastructure.Data.Sql.SqlConnectionFactory

        Public Sub New()
            _settingsProvider = New Infrastructure.Configuration.RegistryConnectionSettingsProvider()
            _connectionFactory = New Infrastructure.Data.Sql.SqlConnectionFactory()
        End Sub

        Public Function LoadUserAccountFiled(deptId As String, signId As String) As DataTable
            Dim settings As Infrastructure.Configuration.ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
            Using command As New SqlCommand("[sp2_LoadUserAccFiled]", connection)
            Using table As New DataTable()
                command.CommandType = CommandType.StoredProcedure
                command.Parameters.Add("@DeptID", SqlDbType.BigInt).Value = deptId
                command.Parameters.Add("@SignID", SqlDbType.BigInt).Value = signId
                table.Load(command.ExecuteReader())
                Return table.Copy()
            End Using
            End Using
            End Using
        End Function

        Public Function LoadUserReportDetailsDone(userId As String, fileStatus As String, signId As String) As DataTable
            Dim settings As Infrastructure.Configuration.ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
            Using command As New SqlCommand("[sp2_LoadUserReportDetailsDONE] '" & userId & "', '" & fileStatus & "','" & signId & "'", connection)
            Using table As New DataTable()
                command.CommandType = CommandType.Text
                table.Load(command.ExecuteReader())
                Return table.Copy()
            End Using
            End Using
            End Using
        End Function

        Public Function LoadUserReportDetailsFiled(userId As String, fileStatus As String, signId As String) As DataTable
            Dim settings As Infrastructure.Configuration.ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
            Using command As New SqlCommand("[sp2_LoadUserReportDetailsFILED] '" & userId & "', '" & fileStatus & "','" & signId & "'", connection)
            Using table As New DataTable()
                command.CommandType = CommandType.Text
                table.Load(command.ExecuteReader())
                Return table.Copy()
            End Using
            End Using
            End Using
        End Function
    End Class
End Namespace
