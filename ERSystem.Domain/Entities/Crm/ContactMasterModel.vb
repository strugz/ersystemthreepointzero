Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("CNTMST")>
Public Class ContactMasterModel
    <Key>
    Public Property ID As Integer

    <Column("_id")>
    Public Property ExternalId As String

    <Column("CNTMID")>
    Public Property CNTMID As String

    <Column("CNTMLN")>
    Public Property CNTMLN As String

    <Column("CNTMMN")>
    Public Property CNTMMN As String

    <Column("CNTMFN")>
    Public Property CNTMFN As String

    <Column("CNTMNN")>
    Public Property CNTMNN As String

    <Column("CNTNUM")>
    Public Property CNTNUM As String

    <Column("CNTDPT")>
    Public Property CNTDPT As String

    <Column("CNTMCN")>
    Public Property CNTMCN As String

    <Column("CNTMSX")>
    Public Property CNTMSX As String

    <Column("CNTMPF")>
    Public Property CNTMPF As String

    <Column("CNTMSF")>
    Public Property CNTMSF As String

    <Column("CNTMBD")>
    Public Property CNTMBD As String

    <Column("CNTARE")>
    Public Property CNTARE As String

    <Column("CNTRTH")>
    Public Property CNTRTH As String

    <Column("CNTLDR")>
    Public Property CNTLDR As String

    <Column("CNTEPS")>
    Public Property CNTEPS As String

    <Column("CNTSTS")>
    Public Property CNTSTS As String

    <Column("CNTSEC")>
    Public Property CNTSEC As String

    <Column("CNTTGP")>
    Public Property CNTTGP As String

    <Column("CNTEGP")>
    Public Property CNTEGP As String

    <Column("CNTMGP")>
    Public Property CNTMGP As String

    <Column("CNTDHD")>
    Public Property CNTDHD As String

    <Column("CNTFRM")>
    Public Property CNTFRM As String
End Class
