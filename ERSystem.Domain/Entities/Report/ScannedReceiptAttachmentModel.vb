Option Strict On

Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbScannedReceiptAttachment")>
Public Class ScannedReceiptAttachmentModel
    <Key>
    Public Property ID As Integer

    Public Property ReportID As String

    Public Property OriginalFileName As String

    Public Property StoredFilePath As String

    Public Property ContentType As String

    Public Property FileExtension As String

    Public Property FileSizeBytes As Long

    Public Property ReceiptContent As Byte()

    Public Property CreatedByUserID As Nullable(Of Integer)

    Public Property CreatedDate As DateTime

    Public Overridable Property Report As ReportDetailModel
End Class
