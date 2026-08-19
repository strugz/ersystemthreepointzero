Option Strict On

Imports System.IO
Imports ERSystem.Domain

Public NotInheritable Class ScannedReceiptSelectionState
    Private ReadOnly _originalLegacyPaths As List(Of String)
    Private ReadOnly _storedReceipts As List(Of ScannedReceiptAttachmentMetadataDto)
    Private ReadOnly _pendingLocalPaths As New List(Of String)()
    Private ReadOnly _pathKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _isReadOnly As Boolean
    Private _replaceExisting As Boolean

    Public Sub New(originalReportAttachment As String,
                   storedReceipts As IEnumerable(Of ScannedReceiptAttachmentMetadataDto),
                   isReadOnly As Boolean)
        _originalLegacyPaths = SplitPaths(originalReportAttachment)
        _storedReceipts = If(storedReceipts, Enumerable.Empty(Of ScannedReceiptAttachmentMetadataDto)()).ToList()
        _isReadOnly = isReadOnly

        For Each path As String In _originalLegacyPaths
            _pathKeys.Add(BuildPathKey(path))
        Next

        For Each receipt As ScannedReceiptAttachmentMetadataDto In _storedReceipts
            If receipt IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(receipt.StoredFilePath) Then
                _pathKeys.Add(BuildPathKey(receipt.StoredFilePath))
            End If
        Next
    End Sub

    Public ReadOnly Property IsReadOnly As Boolean
        Get
            Return _isReadOnly
        End Get
    End Property

    Public ReadOnly Property HasReceipts As Boolean
        Get
            If _replaceExisting Then
                Return _pendingLocalPaths.Count > 0
            End If

            Return _storedReceipts.Count > 0 OrElse _originalLegacyPaths.Count > 0 OrElse _pendingLocalPaths.Count > 0
        End Get
    End Property

    Public ReadOnly Property UpdateMode As ScannedReceiptAttachmentUpdateMode
        Get
            If _replaceExisting Then
                Return ScannedReceiptAttachmentUpdateMode.Replace
            End If

            If _pendingLocalPaths.Count > 0 Then
                Return ScannedReceiptAttachmentUpdateMode.Append
            End If

            Return ScannedReceiptAttachmentUpdateMode.Unchanged
        End Get
    End Property

    Public Function AddLocalPaths(paths As IEnumerable(Of String)) As Integer
        EnsureEditable()

        If paths Is Nothing Then
            Return 0
        End If

        Dim added As Integer
        For Each path As String In paths
            Dim normalizedPath As String = If(path, String.Empty).Trim()
            If normalizedPath.Length = 0 Then
                Continue For
            End If

            Dim pathKey As String = BuildPathKey(normalizedPath)
            If _pathKeys.Add(pathKey) Then
                _pendingLocalPaths.Add(normalizedPath)
                added += 1
            End If
        Next

        Return added
    End Function

    Public Sub Clear()
        EnsureEditable()
        _replaceExisting = True
        _pendingLocalPaths.Clear()
        _pathKeys.Clear()
    End Sub

    Public Function GetPendingLocalPaths() As List(Of String)
        Return _pendingLocalPaths.ToList()
    End Function

    Public Function BuildLegacyAttachmentValue(copiedPendingPaths As IEnumerable(Of String)) As String
        Dim paths As New List(Of String)()
        If Not _replaceExisting Then
            paths.AddRange(_originalLegacyPaths)
        End If

        If copiedPendingPaths IsNot Nothing Then
            paths.AddRange(copiedPendingPaths.Where(Function(path) Not String.IsNullOrWhiteSpace(path)))
        End If

        Return String.Join(";", paths)
    End Function

    Public Function BuildDisplayText() As String
        Dim names As New List(Of String)()
        Dim nameKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        If Not _replaceExisting Then
            For Each receipt As ScannedReceiptAttachmentMetadataDto In _storedReceipts
                AddDisplayName(names, nameKeys, If(receipt Is Nothing, String.Empty, receipt.OriginalFileName))
            Next

            For Each path As String In _originalLegacyPaths
                AddDisplayName(names, nameKeys, IO.Path.GetFileName(path))
            Next
        End If

        For Each path As String In _pendingLocalPaths
            AddDisplayName(names, nameKeys, IO.Path.GetFileName(path))
        Next

        Return String.Join("; ", names)
    End Function

    Private Sub EnsureEditable()
        If _isReadOnly Then
            Throw New InvalidOperationException("Scanned receipts cannot be changed after final approval.")
        End If
    End Sub

    Private Shared Function SplitPaths(value As String) As List(Of String)
        If String.IsNullOrWhiteSpace(value) Then
            Return New List(Of String)()
        End If

        Return value.Split(";"c).
            Select(Function(path) path.Trim()).
            Where(Function(path) path.Length > 0).
            ToList()
    End Function

    Private Shared Function BuildPathKey(path As String) As String
        Try
            Return IO.Path.GetFullPath(path)
        Catch ex As Exception
            Return path.Trim()
        End Try
    End Function

    Private Shared Sub AddDisplayName(names As IList(Of String), nameKeys As ISet(Of String), name As String)
        Dim normalizedName As String = If(name, String.Empty).Trim()
        If normalizedName.Length > 0 AndAlso nameKeys.Add(normalizedName) Then
            names.Add(normalizedName)
        End If
    End Sub
End Class
