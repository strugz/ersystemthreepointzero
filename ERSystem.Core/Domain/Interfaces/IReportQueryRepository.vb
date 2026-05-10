Imports System.Data

Namespace Domain.Interfaces
    Public Interface IReportQueryRepository
        Function LoadingDataReport(userID As String, sDate As String, eDate As String) As DataTable
        Function LoadingExpenseReport(reportID As String, userID As String) As DataTable
        Function LoadingExpenseCount(reportID As String) As Integer
        Function LoadingUserReportDetailsDONE(userID As String, FileStatus As String, signID As String) As DataTable
        Function LoadingUserReportDetailsFILED(userID As String, FileStatus As String, signID As String) As DataTable
        Function LoadingPreviousER(userID As String, sdate As String, edate As String) As DataTable
        Function LoadingExpenseER(userID As String, reportID As String, sdate As String, edate As String) As DataTable
        Function LoadExpenseDetails(Location As String, DeptID As String) As DataTable
    End Interface
End Namespace