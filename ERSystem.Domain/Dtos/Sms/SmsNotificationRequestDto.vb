Imports System.Collections.Generic

Public Class SmsNotificationRequestDto
    Public Property Receiver As String
    Public Property Recipients As List(Of SmsRecipientDto)
    Public Property Sender As String
    Public Property UserInitial As String
    Public Property Message As String
    Public Property ReportID As String
    Public Property ReferenceNo As String
End Class
