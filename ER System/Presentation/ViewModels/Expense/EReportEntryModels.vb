Imports System.Data

Public Class EReportEntrySnapshot
    Public Property Transdate As Date
    Public Property PerdiemChecked As Boolean
    Public Property Particulars As String
    Public Property Invoice As String
    Public Property Multiplier As String
    Public Property LocalChecked As Boolean
    Public Property ForeignChecked As Boolean
    Public Property Category As String
    Public Property CategorySelected As Boolean
    Public Property Amount As String
    Public Property Remarks As String
    Public Property Status As String
    Public Property TotalAmount As String
    Public Property Location As String
    Public Property WorkWith As String
    Public Property ServiceNumber As String
    Public Property Instrument As String
    Public Property SerialNumber As String
    Public Property MDays As String
    Public Property Computation As String
    Public Property TotalDays As String
End Class

Public Class EReportEntryResult
    Public Property Success As Boolean
    Public Property Message As String
    Public Property ShouldAskClearDetails As Boolean
    Public Property ShouldRefreshExpenseGrid As Boolean
    Public Property RefreshReportId As String
    Public Property RefreshUserId As String
    Public Property RefreshData As DataTable
    Public Property FocusParticulars As Boolean
    Public Property EnableExpenseGrid As Boolean
    Public Property ResetTransactionId As Boolean
    Public Property ResetStatusIndex As Nullable(Of Integer)
    Public Property EnableCategory As Boolean
    Public Property ClearComputation As Boolean
    Public Property DeleteExpenseSettingsAfterClear As Boolean
    Public Property PersistWorkContext As Boolean

    Public Shared Function Failed(message As String) As EReportEntryResult
        Return New EReportEntryResult With {
            .Success = False,
            .Message = message
        }
    End Function
End Class

Public Class ExpenseHelperState
    Public Property CategoryIndex As Integer
    Public Property CategoryText As String
    Public Property WorkWith As String
    Public Property ExpenseId As String
    Public Property ComboClick As Integer
    Public Property HasPerdiem As Boolean
End Class
