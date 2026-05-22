Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbUserRegistration")>
Public Class UserRegistrationModel
    <Key>
    Public Property ID As Integer

    Public Property UserID As Nullable(Of Integer)

    <Column("username")>
    Public Property Username As String

    Public Property Password As String
    Public Property Fullname As String
    Public Property Userlevel As String
    Public Property DeptID As Nullable(Of Integer)
    Public Property EmailAdd As String
    Public Property EmailPass As String
    Public Property EmailTo As String
    Public Property EmailBCC As String
    Public Property Signature As Byte()
    Public Property Position As String
    Public Property Status As String
    Public Property Approver1 As String
    Public Property Approver2 As String
    Public Property ReportNumberStatus As Nullable(Of Integer)
    Public Property WorkWithStatus As String
    Public Property SuperApprover As String
End Class
