Imports System.Data
Imports System.Data.SqlClient
Imports ERSystem.Core.Domain.Entities
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class SqlReportRepository
        Implements IReportRepository

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        Public Sub AddReport(report As Report) Implements IReportRepository.AddReport
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("space2_AddReportData", conn) ' Adjusted text to exec
                    cmd.CommandText = "EXEC sp2_AddReportData @dateFrom,@dateTo,@description,@cashAdvance,@cashDate,@cashrefdoc,@cashrefNumber,@balto,@revolvingfund,@cashCheck,@userID,@status,@approved,@dateFiled,@fileStatus"
                    cmd.CommandType = CommandType.Text

                    cmd.Parameters.Add("@dateFrom", SqlDbType.VarChar).Value = report.DateFrom
                    cmd.Parameters.Add("@dateTo", SqlDbType.VarChar).Value = report.DateTo
                    cmd.Parameters.Add("@description", SqlDbType.VarChar).Value = Replace(LTrim(RTrim(report.Description)), vbLf, "")
                    cmd.Parameters.Add("@cashAdvance", SqlDbType.VarChar).Value = report.CashAdvance
                    cmd.Parameters.Add("@cashDate", SqlDbType.VarChar).Value = report.CashDate
                    cmd.Parameters.Add("@cashrefdoc", SqlDbType.VarChar).Value = report.CashRefDoc
                    cmd.Parameters.Add("@cashrefNumber", SqlDbType.VarChar).Value = report.CashRefNumber
                    cmd.Parameters.Add("@balto", SqlDbType.VarChar).Value = report.BalTo
                    cmd.Parameters.Add("@revolvingfund", SqlDbType.VarChar).Value = report.RevolvingFund
                    cmd.Parameters.Add("@cashCheck", SqlDbType.VarChar).Value = report.CashCheck
                    cmd.Parameters.Add("@userID", SqlDbType.VarChar).Value = report.UserID
                    cmd.Parameters.Add("@status", SqlDbType.VarChar).Value = report.Status
                    cmd.Parameters.Add("@approved", SqlDbType.VarChar).Value = report.Approved
                    cmd.Parameters.Add("@dateFiled", SqlDbType.VarChar).Value = report.DateFiled
                    cmd.Parameters.Add("@fileStatus", SqlDbType.VarChar).Value = report.FileStatus

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub UpdateReport(report As Report) Implements IReportRepository.UpdateReport
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_UpdateReportData", conn)
                    cmd.CommandText = "EXEC sp2_UpdateReportData @reportID,@dateFrom,@dateTo,@description,@cashAdvance,@cashDate,@cashrefdoc,@cashrefNumber,@revolvingfund,@cashCheck"
                    cmd.CommandType = CommandType.Text

                    cmd.Parameters.Add("@reportID", SqlDbType.VarChar).Value = report.ReportID
                    cmd.Parameters.Add("@dateFrom", SqlDbType.VarChar).Value = report.DateFrom
                    cmd.Parameters.Add("@dateTo", SqlDbType.VarChar).Value = report.DateTo
                    cmd.Parameters.Add("@description", SqlDbType.VarChar).Value = Replace(LTrim(RTrim(report.Description)), vbLf, "")
                    cmd.Parameters.Add("@cashAdvance", SqlDbType.VarChar).Value = report.CashAdvance
                    cmd.Parameters.Add("@cashDate", SqlDbType.VarChar).Value = report.CashDate
                    cmd.Parameters.Add("@cashrefdoc", SqlDbType.VarChar).Value = report.CashRefDoc
                    cmd.Parameters.Add("@cashrefNumber", SqlDbType.VarChar).Value = report.CashRefNumber
                    cmd.Parameters.Add("@revolvingfund", SqlDbType.VarChar).Value = report.RevolvingFund
                    cmd.Parameters.Add("@cashCheck", SqlDbType.VarChar).Value = report.CashCheck

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub RefileReport(reportID As String, status As String) Implements IReportRepository.RefileReport
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_RefileER", conn)
                    cmd.CommandText = "EXEC sp2_RefileER @reportID,@status"
                    cmd.CommandType = CommandType.Text

                    cmd.Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                    cmd.Parameters.Add("@status", SqlDbType.VarChar).Value = status

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class
End Namespace