Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("tbUserAuthority")>
Public Class UserAuthorityModel
    <Key>
    <Column("id")>
    Public Property Id As Long

    Public Property UserID As Nullable(Of Integer)
    Public Property AuthorityID As Nullable(Of Integer)
    Public Property AuthorityName As String
    Public Property Sort As Nullable(Of Integer)
End Class
