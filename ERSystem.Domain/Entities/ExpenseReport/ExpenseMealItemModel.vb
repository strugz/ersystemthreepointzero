Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbExpenseMealItem")>
Public Class ExpenseMealItemModel

    <Key>
    Public Property id As Integer

    Public Property Meal As String

    Public Property PaidFor As String

    Public Property PaidEmp As String

    Public Property ExpenseID As Nullable(Of Long)

End Class
