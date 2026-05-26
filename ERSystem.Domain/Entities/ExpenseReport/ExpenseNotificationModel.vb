Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbNotification")>
Public Class ExpenseNotificationModel

    <Key>
    Public Property ID As String

    Public Property ToNotify As String

    Public Property DateIncluded As Nullable(Of Date)

    Public Property ExpenseID As String

    Public Property Category As String

    Public Property UsernameFiled As String

    Public Property Status As Integer

End Class
