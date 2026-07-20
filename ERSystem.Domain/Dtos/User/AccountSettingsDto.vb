Public Class AccountSettingsDto
    Public Property Id As Nullable(Of Integer)
    Public Property UserId As Integer
    Public Property UserName As String
    Public Property FullName As String
    Public Property UserLevel As String
    Public Property DeptId As Nullable(Of Integer)
    Public Property DepartmentName As String
    Public Property EmailAdd As String
    Public Property EmailPass As String
    Public Property EmailTo As String
    Public Property EmailBcc As String
    Public Property NotificationEmail As String
    Public Property Signature As Byte()
    Public Property Position As String
    Public Property Status As String
    Public Property Approver1 As String
    Public Property Approver2 As String
    Public Property ReportNumberStatus As Nullable(Of Integer)
    Public Property WorkWithStatus As String
    Public Property SuperApprover As String
    Public Property TranspoRate As Nullable(Of Double)
    Public Property BreakFastRate As Nullable(Of Double)
    Public Property LunchRate As Nullable(Of Double)
    Public Property DinnerRate As Nullable(Of Double)
    Public Property OtMeal As Nullable(Of Double)
    Public Property AuthorityRows As New List(Of UserAuthorityDto)()
End Class
