Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("ACCMST_")>
Public Class AccountMasterModel
    <Key>
    Public Property ID As Integer

    <Column("_id")>
    Public Property ExternalId As String

    <Column("ACCMID")>
    Public Property ACCMID As String

    <Column("ACCMSC")>
    Public Property ACCMSC As String

    <Column("ACCMNM")>
    Public Property ACCMNM As String

    <Column("ACCMBC")>
    Public Property ACCMBC As String

    <Column("ACCMAD")>
    Public Property ACCMAD As String

    <Column("ACCMPH")>
    Public Property ACCMPH As String

    <Column("ACCMEM")>
    Public Property ACCMEM As String

    <Column("ACCMWS")>
    Public Property ACCMWS As String

    <Column("ACCSTS")>
    Public Property ACCSTS As String

    <Column("ACCOWN")>
    Public Property ACCOWN As String
End Class
