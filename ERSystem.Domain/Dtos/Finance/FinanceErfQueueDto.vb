Imports System

Public Class FinanceErfQueueDto
    Public Property ReportID As String
    Public Property UserID As Nullable(Of Integer)
    Public Property Username As String
    Public Property EmployeeName As String
    Public Property ReportDateFrom As Nullable(Of Date)
    Public Property ReportDateTo As Nullable(Of Date)
    Public Property ReportDescription As String
    Public Property ReportType As String
    Public Property ERFReferenceNo As String
    Public Property CashRefNo As String
    Public Property FinanceStatus As String
    Public Property PhysicalReceiptsReceived As Boolean
    Public Property PhysicalReceiptsReceivedDate As Nullable(Of DateTime)
    Public Property FinanceRemarks As String
End Class
