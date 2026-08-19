Imports System

Public Class UpdateExpenseRequestDto

    Public Property ExpenseID As Long

    Public Property Transdate As Nullable(Of Date)

    Public Property Perdiem As String

    Public Property Particulars As String

    Public Property Invoice As String

    Public Property Multiplier As Nullable(Of Integer)

    Public Property [Type] As String

    Public Property Category As String

    Public Property Amount As Nullable(Of Double)

    Public Property VatAmount As Nullable(Of Double)

    Public Property Remarks As String

    Public Property Status As String

    Public Property TotalAmount As Nullable(Of Double)

    Public Property Location As String

    Public Property UserID As Nullable(Of Integer)

    Public Property ReportID As String

    Public Property WorkWith As String

    Public Property ServiceNumber As String

    Public Property Instrument As String

    Public Property SerialNumber As String

    Public Property MDays As String

    Public Property Computation As String

    Public Property TotDays As String

    Public Property Meal As String

    Public Property PaidFor As String

    Public Property PaidEmp As String

    Public Property FareID As Nullable(Of Long)

    Public Property FareFrom As String

    Public Property FareTo As String

End Class
