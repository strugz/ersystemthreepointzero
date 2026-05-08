Option Strict On

Namespace Application.Services
    Public Class LoginAccessResult
        Public Property IsAllowed As Boolean
        Public Property DisplayName As String
        Public Property DepartmentName As String
        Public Property MenuFormsVisible As Boolean
        Public Property MenuFileVisible As Boolean
        Public Property MainFormEnabled As Boolean
        Public Property PreviousReportsVisible As Boolean
        Public Property UserAccountVisible As Boolean
        Public Property ExpenseSummaryVisible As Boolean
    End Class
End Namespace
