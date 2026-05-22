Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbUserRegistration")>
Public Class UserRegistrationModel
    <Key>
    Public Property UserID As Integer

    Public Property Fullname As String
End Class
