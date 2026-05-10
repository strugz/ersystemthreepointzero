Imports System.Data
Imports System.Data.SqlClient
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class SqlReportQueryRepository
        Implements IReportQueryRepository

        Private ReadOnly _connectionString As String
        Private ReadOnly _previousConnectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        Public Sub New(connectionString As String, previousConnectionString As String)
            _connectionString = connectionString
            _previousConnectionString = previousConnectionString
        End Sub

        Public Function LoadingDataReport(userID As String, sDate As String, eDate As String) As DataTable Implements IReportQueryRepository.LoadingDataReport
            Dim dtLoadingER As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadDataReport_Three '" & userID & "','" & sDate & "','" & eDate & "'", conn)
                    cmd.CommandType = CommandType.Text

                    ' Parameters are here but the previous code interpolated them into the string. 
                    ' I'll keep the string interpolation from Legacy to match behaviour but the params are also listed.
                    cmd.Parameters.Add("@userID", SqlDbType.VarChar).Value = userID
                    cmd.Parameters.Add("@sDate", SqlDbType.VarChar).Value = sDate
                    cmd.Parameters.Add("@eDate", SqlDbType.VarChar).Value = eDate

                    conn.Open()
                    dtLoadingER.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dtLoadingER
        End Function

        Public Function LoadingExpenseReport(reportID As String, userID As String) As DataTable Implements IReportQueryRepository.LoadingExpenseReport
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("EXEC sp2_LoadExpense_Three'" & reportID & "','" & userID & "'", conn)
                    cmd.CommandType = CommandType.Text
                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadingExpenseCount(reportID As String) As Integer Implements IReportQueryRepository.LoadingExpenseCount
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("sp2_LoadingExpenseCount", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            If dt.Rows.Count > 0 Then
                Return Convert.ToInt32(dt.Rows(0)("ExpenseCount"))
            End If
            Return 0
        End Function

        Public Function LoadingUserReportDetailsDONE(userID As String, FileStatus As String, signID As String) As DataTable Implements IReportQueryRepository.LoadingUserReportDetailsDONE
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_LoadUserReportDetailsDONE] '" & userID & "', '" & FileStatus & "','" & signID & "'", conn)
                    cmd.CommandType = CommandType.Text
                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadingUserReportDetailsFILED(userID As String, FileStatus As String, signID As String) As DataTable Implements IReportQueryRepository.LoadingUserReportDetailsFILED
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_LoadUserReportDetailsFILED] '" & userID & "', '" & FileStatus & "','" & signID & "'", conn)
                    cmd.CommandType = CommandType.Text
                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadingPreviousER(userID As String, sdate As String, edate As String) As DataTable Implements IReportQueryRepository.LoadingPreviousER
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_previousConnectionString)
                Using cmd As New SqlCommand("[sp2_LoadDataReport]", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@userID", SqlDbType.VarChar).Value = userID
                    cmd.Parameters.Add("@sDate", SqlDbType.VarChar).Value = sdate
                    cmd.Parameters.Add("@eDate", SqlDbType.VarChar).Value = edate

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadingExpenseER(userID As String, reportID As String, sdate As String, edate As String) As DataTable Implements IReportQueryRepository.LoadingExpenseER
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_previousConnectionString)
                Using cmd As New SqlCommand("sp_LoadExpense", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@ReportID", SqlDbType.VarChar).Value = reportID
                    cmd.Parameters.Add("@userID", SqlDbType.VarChar).Value = userID
                    cmd.Parameters.Add("@sDate", SqlDbType.VarChar).Value = sdate
                    cmd.Parameters.Add("@eDate", SqlDbType.VarChar).Value = edate

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

        Public Function LoadExpenseDetails(Location As String, DeptID As String) As DataTable Implements IReportQueryRepository.LoadExpenseDetails
            Dim dt As New DataTable()
            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_LoadExpenceDetails]", conn)
                    cmd.CommandType = CommandType.StoredProcedure
                    cmd.Parameters.Add("@Location", SqlDbType.NVarChar).Value = Location
                    cmd.Parameters.Add("@DeptID", SqlDbType.NVarChar).Value = DeptID

                    conn.Open()
                    dt.Load(cmd.ExecuteReader())
                End Using
            End Using
            Return dt
        End Function

    End Class
End Namespace