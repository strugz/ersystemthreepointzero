Option Strict On

Namespace Global.ERSystem.AppServices.Services.ExpenseReport
    Public Class AddExpenseResult

        Public Sub New(success As Boolean, createdExpenseId As Long, message As String)
            Me.Success = success
            Me.CreatedExpenseId = createdExpenseId
            Me.Message = message
        End Sub

        Public Property Success As Boolean

        Public Property CreatedExpenseId As Long

        Public Property Message As String

        Public Shared Function Succeeded(createdExpenseId As Long) As AddExpenseResult
            Return New AddExpenseResult(True, createdExpenseId, String.Empty)
        End Function

        Public Shared Function Failed(message As String) As AddExpenseResult
            Return New AddExpenseResult(False, 0, message)
        End Function
    End Class
End Namespace
