Option Strict On

Imports System.Data.SqlClient
Imports ERSystem.Domain.Approval
Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data.Sql

Namespace Global.ERSystem.Infrastructure.Data
    Public Class ApproveActionRepository
        Implements IApproveActionRepository

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

        Public Function ValidateApproval(reportId As String, managerUserId As String) As ApprovalValidationStatus Implements IApproveActionRepository.ValidateApproval
            If String.IsNullOrWhiteSpace(reportId) OrElse String.IsNullOrWhiteSpace(managerUserId) Then
                Return ApprovalValidationStatus.WaitingForPreviousApprover
            End If

            Const sql As String =
                "DECLARE @Cycle int; " &
                "SELECT @Cycle = MAX(ApprovalCycle) FROM tbReportApprovalTransaction WHERE ReportID = @ReportID; " &
                "SELECT CASE " &
                "WHEN currentStep.ID IS NULL THEN 0 " &
                "WHEN currentStep.Status <> 'Pending' THEN 2 " &
                "WHEN EXISTS (SELECT 1 FROM tbReportApprovalTransaction earlier " &
                "             WHERE earlier.ReportID = currentStep.ReportID " &
                "               AND earlier.ApprovalCycle = currentStep.ApprovalCycle " &
                "               AND earlier.StepOrder < currentStep.StepOrder " &
                "               AND earlier.Status <> 'Approved') THEN 0 " &
                "ELSE 1 END " &
                "FROM (SELECT 1 AS Anchor) anchor " &
                "LEFT JOIN tbReportApprovalTransaction currentStep " &
                "  ON currentStep.ReportID = @ReportID " &
                " AND currentStep.ApprovalCycle = @Cycle " &
                " AND currentStep.ApproverUserID = @ManagerUserID;"

            Dim settings As ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
                Using command As New SqlCommand(sql, connection)
                    command.CommandType = CommandType.Text
                    command.Parameters.Add("@ReportID", SqlDbType.VarChar, 50).Value = reportId
                    command.Parameters.Add("@ManagerUserID", SqlDbType.BigInt).Value = managerUserId
                    Return DirectCast(Convert.ToInt32(command.ExecuteScalar()), ApprovalValidationStatus)
                End Using
            End Using
        End Function

        Public Sub UpdateFileStatus(userIdToApprover As String, reportIdToApprove As String, loginUserId As String) Implements IApproveActionRepository.UpdateFileStatus
            Dim settings As ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
                Using command As New SqlCommand("sp2_UpdateReportNumberStatus", connection)
                    command.CommandType = CommandType.StoredProcedure
                    command.Parameters.Add("@UserID", SqlDbType.BigInt).Value = userIdToApprover
                    command.Parameters.Add("@ReportID", SqlDbType.VarChar, 50).Value = reportIdToApprove
                    command.Parameters.Add("@SignID", SqlDbType.BigInt).Value = loginUserId
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Function IsReportApprovedDone(reportId As String) As Boolean Implements IApproveActionRepository.IsReportApprovedDone
            If String.IsNullOrWhiteSpace(reportId) Then
                Return False
            End If

            Dim settings As ConnectionSettings = _settingsProvider.Load()
            Using connection As SqlConnection = _connectionFactory.CreateOpenConnection(settings)
                Using command As New SqlCommand("SELECT COUNT(1) FROM tbReportDetails WHERE ID = @ReportID AND ReportFileStatus = '0' AND ReportPrintStatus = '0'", connection)
                    command.CommandType = CommandType.Text
                    command.Parameters.Add("@ReportID", SqlDbType.VarChar, 50).Value = reportId
                    Return Convert.ToInt32(command.ExecuteScalar()) > 0
                End Using
            End Using
        End Function
    End Class
End Namespace
