Imports System

Public Class ReportDetailDto

    Public Property ID As String

    Public Property ReportDateFrom As Nullable(Of Date)

    Public Property ReportDateTo As Nullable(Of Date)

    Public Property ReportDescription As String

    Public Property UserID As Nullable(Of Integer)

    Public Property ReportStatus As String

    Public Property ReportEndorseSignature As Byte()

    Public Property ReportEndorseStatus As String

    Public Property ReportDateFiled As String

    Public Property ReportFileStatus As String

    Public Property ExpenseID As Nullable(Of Integer)

    Public Property ReportPrintStatus As String

    Public Property ReportReturnedForModi As String

    Public Property ReportNumberStatus As Nullable(Of Integer)

    Public Property ReportReserveSignature As Byte()

    Public Property ReportReserveStatus1 As String

    Public Property ReportReserveStatus2 As String

    Public Property ReportCancelNote As String

    Public Property ReportAttachment As String

    Public Property ReportSentStatus As String

    Public Property ReportType As String

    Public Property ERFReferenceNo As String

End Class
