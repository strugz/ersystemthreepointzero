Option Strict On

Public Class ScannedReceiptAttachmentMetadataDto
    Public Property ID As Integer

    Public Property ReportID As String

    Public Property OriginalFileName As String

    Public Property StoredFilePath As String

    Public Property ContentType As String

    Public Property FileExtension As String

    Public Property FileSizeBytes As Long

    Public Property CreatedByUserID As Nullable(Of Integer)

    Public Property CreatedDate As DateTime
End Class
