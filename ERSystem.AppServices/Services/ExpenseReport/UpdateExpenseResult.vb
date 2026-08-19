Option Strict On

Namespace Global.ERSystem.AppServices.Services.ExpenseReport
    Public Class UpdateExpenseResult

        Public Sub New(success As Boolean, updatedExpenseId As Long, message As String)
            Me.Success = success
            Me.UpdatedExpenseId = updatedExpenseId
            Me.Message = message
        End Sub

        Public Property Success As Boolean

        Public Property UpdatedExpenseId As Long

        Public Property Message As String

        Public Shared Function Succeeded(updatedExpenseId As Long) As UpdateExpenseResult
            Return New UpdateExpenseResult(True, updatedExpenseId, String.Empty)
        End Function

        Public Shared Function Failed(message As String) As UpdateExpenseResult
            Return New UpdateExpenseResult(False, 0, message)
        End Function
    End Class
End Namespace
