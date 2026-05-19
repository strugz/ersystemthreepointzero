Option Strict On

Namespace AppServices
    Friend Class LoginResult
        Public Property IsSuccess As Boolean
        Public Property Message As String = String.Empty
        Public Property FullName As String = String.Empty
        Public Property Department As String = String.Empty
        Public Property ShowMenuForms As Boolean
        Public Property ShowMenuFile As Boolean
        Public Property EnableMainForm As Boolean
        Public Property ShowPreviousReports As Boolean
        Public Property ShowUserAccountMenu As Boolean
        Public Property ShowExpenseSummary As Boolean
    End Class
End Namespace
