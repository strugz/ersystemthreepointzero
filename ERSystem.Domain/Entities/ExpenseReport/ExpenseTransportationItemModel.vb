Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbExpenseTransportationItem")>
Public Class ExpenseTransportationItemModel

    <Key>
    Public Property id As Integer

    Public Property expense_id As Nullable(Of Long)

    Public Property FareID As String

    Public Property FareFrom As String

    Public Property FareTo As String

End Class
