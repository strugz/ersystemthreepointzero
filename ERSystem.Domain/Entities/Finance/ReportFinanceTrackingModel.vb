Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbReportFinanceTracking")>
Public Class ReportFinanceTrackingModel
    <Key>
    Public Property ID As Long

    Public Property ReportID As String

    Public Property FinanceStatus As String

    Public Property PhysicalReceiptsReceived As Boolean

    Public Property PhysicalReceiptsReceivedBy As Nullable(Of Integer)

    Public Property PhysicalReceiptsReceivedDate As Nullable(Of DateTime)

    Public Property FinanceRemarks As String

    Public Property ScannedReceiptsDeletedDate As Nullable(Of DateTime)

    <ForeignKey("ReportID")>
    Public Overridable Property Report As ReportDetailModel
End Class
