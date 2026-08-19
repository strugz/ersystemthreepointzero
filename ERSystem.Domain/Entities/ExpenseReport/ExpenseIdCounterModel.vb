Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbExpenseIdCounter")>
Public Class ExpenseIdCounterModel

    <Key>
    Public Property CounterName As String

    Public Property CurrentValue As Long

    Public Property UpdatedAt As Nullable(Of Date)

End Class
