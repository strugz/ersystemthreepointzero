Option Strict On

Public Class SaveScannedReceiptAttachmentRequest
    Public Property ReportID As String

    Public Property LocalPaths As IEnumerable(Of String)

    Public Property CreatedByUserID As Nullable(Of Integer)
End Class
