Imports System

Public Class ExpenseDetailDto

    Public Property ID As Long

    Public Property ExpenseTransDate As Nullable(Of Date)

    Public Property ExpensePerdiem As String

    Public Property ExpenseParticulars As String

    Public Property ExpenseInvoice As String

    Public Property ExpenseMultiplier As Nullable(Of Integer)

    Public Property ExpenseType As String

    Public Property ExpenseCategory As String

    Public Property ExpenseAmount As Nullable(Of Double)

    Public Property VatAmount As Nullable(Of Double)

    Public Property ExpenseRemarks As String

    Public Property ExpenseStatus As String

    Public Property UserID As Nullable(Of Integer)

    Public Property ExpenseTotalAmount As Nullable(Of Double)

    Public Property ExpenseLocation As String

    Public Property ReportID As String

    Public Property WorkWith As String

    Public Property ServiceNumber As String

    Public Property Instrument As String

    Public Property SerialNumber As String

    Public Property Sort As Nullable(Of Integer)

    Public Property MDays As String

    Public Property Computation As String

    Public Property TotDays As String

    Public Property NumberEdited As Nullable(Of Integer)

End Class
