Imports System

Public Class FwmsTransactionDto
    Public Property ACCMSC As String
    Public Property ACCMNM As String
    Public Property CNTMNN As String
    Public Property TRDMTY As String
    Public Property TRDMMC As String
    Public Property TRDMDE As String
    Public Property TRDMTT As String
    Public Property TRDSEC As String
    Public Property TRDSTS As String
    Public Property TRDMCD As Nullable(Of Date)

    Public ReadOnly Property HospitalName As String
        Get
            Return ACCMNM
        End Get
    End Property

    Public ReadOnly Property InstrumentModel As String
        Get
            Return TRDMDE
        End Get
    End Property

    Public ReadOnly Property SRNumber As String
        Get
            Return TRDMTT
        End Get
    End Property

    Public ReadOnly Property SerialNumber As String
        Get
            Return TRDMMC
        End Get
    End Property
End Class
