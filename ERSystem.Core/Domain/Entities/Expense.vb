Namespace Domain.Entities
    Public Class Expense
        Public Property TransID As String
        Public Property TransDate As String
        Public Property PerDiem As String
        Public Property Particulars As String
        Public Property Invoice As String
        Public Property Multiplier As String
        Public Property ExtType As String ' type is a reserved keyword
        Public Property Category As String
        Public Property Amount As String
        Public Property Remarks As String
        Public Property Status As String
        Public Property TotalAmount As String
        Public Property Location As String
        Public Property UserID As String
        Public Property ReportID As String
        Public Property ServiceNumber As String
        Public Property Instrument As String
        Public Property SerialNumber As String
        Public Property WorkWith As String

        Public Property MDays As String
        Public Property Computation As String
        Public Property TotDays As String

        Public Property UserExpenseMeal As String
        Public Property UserExpenseTransportation As String
    End Class
End Namespace
