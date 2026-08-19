Option Strict On

Imports System.Data.SqlClient
Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data.Sql

Namespace Global.ERSystem.Infrastructure.Data
    Public Class ApproveRepository
        Implements IApproveRepository

        Private ReadOnly _settingsProvider As RegistryConnectionSettingsProvider
        Private ReadOnly _connectionFactory As SqlConnectionFactory

        Public Sub New()
            Me.New(New RegistryConnectionSettingsProvider(), New SqlConnectionFactory())
        End Sub

        Public Sub New(settingsProvider As RegistryConnectionSettingsProvider, connectionFactory As SqlConnectionFactory)
            If settingsProvider Is Nothing Then
                Throw New ArgumentNullException("settingsProvider")
            End If

            If connectionFactory Is Nothing Then
                Throw New ArgumentNullException("connectionFactory")
            End If

            _settingsProvider = settingsProvider
            _connectionFactory = connectionFactory
        End Sub

        Public Function LoadUserAccountFiled(deptId As String, signId As String) As DataTable Implements IApproveRepository.LoadUserAccountFiled
            Dim settings As ConnectionSettings = _settingsProvider.Load()
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

        Public Function LoadUserReportDetailsDone(userId As String, fileStatus As String, signId As String) As DataTable Implements IApproveRepository.LoadUserReportDetailsDone
            Return LoadUserReportDetails("[sp2_LoadUserReportDetailsDONE]", userId, fileStatus, signId)
        End Function

        Public Function LoadUserReportDetailsFiled(userId As String, fileStatus As String, signId As String) As DataTable Implements IApproveRepository.LoadUserReportDetailsFiled
            Return LoadUserReportDetails("[sp2_LoadUserReportDetailsFILED]", userId, fileStatus, signId)
        End Function

        Private Function LoadUserReportDetails(storedProcedureName As String, userId As String, fileStatus As String, signId As String) As DataTable
            Dim settings As ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
                Using command As New SqlCommand(storedProcedureName, connection)
                    Using table As New DataTable()
                        command.CommandType = CommandType.StoredProcedure
                        command.Parameters.Add("@UserID", SqlDbType.BigInt).Value = userId
                        command.Parameters.Add("@FileStatus", SqlDbType.VarChar, 10).Value = fileStatus
                        command.Parameters.Add("@SignID", SqlDbType.BigInt).Value = signId
                        table.Load(command.ExecuteReader())
                        Return table.Copy()
                    End Using
                End Using
            End Using
        End Function
    End Class
End Namespace
