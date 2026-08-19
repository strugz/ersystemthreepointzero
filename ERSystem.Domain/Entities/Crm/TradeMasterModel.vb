Imports System
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

<Table("TRDMST")>
Public Class TradeMasterModel
    <Key>
    Public Property ID As Integer

    <Column("_id")>
    Public Property ExternalId As String

    <Column("TRDMTI")>
    Public Property TRDMTI As String

    <Column("TRDMAC")>
    Public Property TRDMAC As String

    <Column("TRDMTY")>
    Public Property TRDMTY As String

    <Column("TRDSEC")>
    Public Property TRDSEC As String

    <Column("TRDMMC")>
    Public Property TRDMMC As String

    <Column("TRDMCL")>
    Public Property TRDMCL As String

    <Column("TRDMTT")>
    Public Property TRDMTT As String

    <Column("TRDMDE")>
    Public Property TRDMDE As String

    <Column("TRDADT")>
    Public Property TRDADT As Nullable(Of Date)

    <Column("TRDMUI")>
    Public Property TRDMUI As String

    <Column("TRDSTS")>
    Public Property TRDSTS As String

    <Column("TRDMST")>
    Public Property TRDMST As Nullable(Of Boolean)

    <Column("TRDCNT")>
    Public Property TRDCNT As Nullable(Of Integer)

    <Column("TRDMCD")>
    Public Property TRDMCD As Nullable(Of Date)

    <Column("TRDUBY")>
    Public Property TRDUBY As String

    <Column("TRDUPD")>
    Public Property TRDUPD As Nullable(Of Date)

    <Column("TRDCRN")>
    Public Property TRDCRN As String

    <Column("TRDLOC")>
    Public Property TRDLOC As String

    <Column("TRDITI")>
    Public Property TRDITI As String

    <Column("status")>
    Public Property Status As Nullable(Of Long)

    <Column("TRDLOCOUT")>
    Public Property TRDLOCOUT As String

    <Column("TRDITISVC")>
    Public Property TRDITISVC As String

    <Column("TRDINSCAT")>
    Public Property TRDINSCAT As String

    <Column("TRDARE")>
    Public Property TRDARE As String
End Class
