Option Strict On

Namespace AppServices
    Friend Class RejectActionResult
        Public Property IsSuccess As Boolean
        Public Property Message As String = String.Empty
        Public Property ReloadResult As ApproveReloadResult
    End Class
End Namespace
