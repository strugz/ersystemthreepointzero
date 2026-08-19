Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tblEmpRate")>
Public Class EmployeeRateModel
    <Key>
    Public Property ID As Integer

    Public Property UserID As Nullable(Of Integer)
    Public Property TranspoRate As Nullable(Of Double)
    Public Property BreakFastRate As Nullable(Of Double)
    Public Property LunchRate As Nullable(Of Double)
    Public Property DinnerRate As Nullable(Of Double)
    Public Property OTMeal As Nullable(Of Double)
End Class
