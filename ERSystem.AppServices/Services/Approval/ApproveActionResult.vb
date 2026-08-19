Option Strict On

Imports System.Data

Public Class ApproveActionResult
    Public Property IsSuccess As Boolean
    Public Property Message As String = String.Empty
    Public Property UserAccounts As DataTable
    Public Property ReportDetails As DataTable
    Public Property ShowNumberOfFile As Boolean
End Class
