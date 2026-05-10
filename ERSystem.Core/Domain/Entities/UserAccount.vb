Namespace Domain.Entities
    Public Class UserAccount
        Public Property UserID As String
        Public Property Username As String
        Public Property Password As String
        Public Property Fullname As String
        Public Property Position As String
        Public Property DepartmentID As String
        Public Property DepartmentName As String
        Public Property EmailAddress As String
        Public Property EmailPassword As String
        Public Property EmailTo As String
        Public Property EmailBcc As String
        Public Property UserLevel As String
        Public Property Approver1Id As String
        Public Property Approver2Id As String
        Public Property TransportationRate As Decimal
        Public Property BreakfastRate As Decimal
        Public Property LunchRate As Decimal
        Public Property DinnerRate As Decimal
        Public Property OTMealRate As Decimal
        Public Property Signature As Byte()
        Public Property Status As String ' Added To fix frmMain.vb login dup checks
    End Class
End Namespace
