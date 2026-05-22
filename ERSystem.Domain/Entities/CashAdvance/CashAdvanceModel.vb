Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbCashAdvance")>
Public Class CashAdvanceModel

    <Key>
    Public Property ID As Integer

    Public Property ReportID As String

    Public Property emp_userID As Nullable(Of Integer)

    Public Property CashAmount As Nullable(Of Double)

    Public Property CashDate As String

    Public Property CashRefDoc As String

    Public Property CashRefNo As String

    Public Property BalanceTo As String

    Public Property RevolvingFund As String

    Public Property CashCheck As String

    <ForeignKey("ReportID")>
    Public Overridable Property ReportDetail As ReportDetailModel

End Class
