Imports System

Public Class FinanceErfDetailDto
    Public Property ReportID As String
    Public Property UserID As Nullable(Of Integer)
    Public Property Username As String
    Public Property EmployeeName As String
    Public Property ReportDateFrom As Nullable(Of Date)
    Public Property ReportDateTo As Nullable(Of Date)
    Public Property ReportDescription As String
    Public Property ReportType As String
    Public Property CashAmount As Nullable(Of Double)
    Public Property CashDate As String
    Public Property CashRefDoc As String
    Public Property CashRefNo As String
    Public Property RevolvingFund As String
    Public Property ReportAttachment As String
    Public Property FinanceStatus As String
    Public Property PhysicalReceiptsReceived As Boolean
    Public Property PhysicalReceiptsReceivedBy As Nullable(Of Integer)
    Public Property PhysicalReceiptsReceivedDate As Nullable(Of DateTime)
    Public Property FinanceRemarks As String
    Public Property ScannedReceiptsDeletedDate As Nullable(Of DateTime)
End Class
