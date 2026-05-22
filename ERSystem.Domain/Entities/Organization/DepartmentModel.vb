Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tblDept")>
Public Class DepartmentModel
    <Key>
    Public Property ID As Integer

    Public Property emp_Dept As String
End Class
