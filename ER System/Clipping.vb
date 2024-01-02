Imports System.Drawing
Imports System.Windows.Forms

Public Class Clipping

#Region "Class Description"
    ' 
    ' Written by Brian Yule
    '
    ' Created on the 24th of August, 2002
    ' 
    ' This class allows the .Net programmer to use images to create regions.
    ' It also allows the regions created to be applied to forms.  There are also
    ' some extra functions for dealing with bitmaps and forms.
    '
    ' This code may be used and or changed freely.
    ' I do not however accept any liability for the use of this code.
    '
#End Region

    Private reg As Region
    Private bm As Bitmap

    Public ReadOnly Property ScannedBitmapRegion() As Region
        Get
            Return reg
        End Get
    End Property
    Public ReadOnly Property ScannedBitmap() As Bitmap
        Get
            Return bm
        End Get
    End Property

    Public Function CopyScannedBitmapRegion() As Region
        Return reg.Clone()
    End Function
    Public Sub ApplyScannedBitmapRegion(ByRef frmForm As Form)
        '
        ' If we dont copy the region when applied the 
        ' scanned bitmap region would no longer be useable.
        ' The basics of form clipping come from the Windows 
        ' 32 API.  The API function is SetWindowsRgn and when 
        ' called it takes a region and applies it and then 
        ' deletes the region
        '
        frmForm.Region = reg.Clone()
    End Sub
    Public Sub ApplyScannedBitmapRegion(ByRef rgnRegion As Region)
        '
        ' If we dont copy the region when applied the 
        ' scanned bitmap region would no longer be useable.
        ' The basics of form clipping come from the Windows 
        ' 32 API.  The API function is SetWindowsRgn and when 
        ' called it takes a region and applies it and then 
        ' deletes the region
        '
        rgnRegion = reg.Clone()
    End Sub
    Public Sub ApplyScannedBitmap(ByRef oldImgBitmap As Bitmap)
        oldImgBitmap = bm
    End Sub
    Public Sub ApplyScannedBitmap(ByRef frmForm As Form)
        frmForm.BackgroundImage = bm
    End Sub
    Public Sub SetFormScannedBitmapSize(ByRef frmForm As Form)
        frmForm.Height = bm.Height
        frmForm.Width = bm.Width
    End Sub
    Public Sub SetFormScannedBitmapSize(ByRef frmForm As Size)
        frmForm.Height = bm.Height
        frmForm.Width = bm.Width
    End Sub

    Public Sub ScanBitmap(ByVal BitmapFileName As String)
        Dim imgBitmap As Bitmap = Bitmap.FromFile(BitmapFileName)

        If Not (imgBitmap Is Nothing) Then
            baseScanBitmap(imgBitmap, imgBitmap.GetPixel(0, 0))
        End If
    End Sub
    Public Sub ScanBitmap(ByVal BitmapFileName As String, ByVal TransparentColor As Color)
        Dim imgBitmap As Bitmap = Bitmap.FromFile(BitmapFileName)

        If Not (imgBitmap Is Nothing) Then
            baseScanBitmap(imgBitmap, TransparentColor)
        End If
    End Sub
    Public Sub ScanBitmap(ByVal imgBitmap As Bitmap)
        If Not (imgBitmap Is Nothing) Then
            baseScanBitmap(imgBitmap, imgBitmap.GetPixel(0, 0))
        End If
    End Sub
    Public Sub ScanBitmap(ByVal imgBitmap As Bitmap, ByVal TransparentColor As Color)
        If Not (imgBitmap Is Nothing) Then
            baseScanBitmap(imgBitmap, TransparentColor)
        End If
    End Sub
    Public Sub ScanBitmap(ByVal frmForm As Form, ByVal TransparentColor As Color)
        If Not (frmForm.BackgroundImage Is Nothing) Then
            baseScanBitmap(frmForm.BackgroundImage, TransparentColor)
        End If
    End Sub
    Private Sub baseScanBitmap(ByVal imgBitmap As Bitmap, ByVal TransparentColor As Color)
        If Not (imgBitmap Is Nothing) Then
            bm = imgBitmap
            Dim BackColor As Color, row As Integer, col As Integer
            reg = New Region(New Rectangle(0, 0, 0, 0))

            BackColor = TransparentColor

            For row = 0 To imgBitmap.Height - 1
                Dim tmpStartCol As Integer = -1
                Dim tmpStopCol As Integer = -1

                For col = 0 To imgBitmap.Width
                    If col = imgBitmap.Width Then
                        If tmpStartCol <> -1 Then
                            tmpStopCol = col
                            Dim regUnion As New Region(New Rectangle(tmpStartCol, row, tmpStopCol - tmpStartCol, 1))
                            reg.Union(regUnion)
                            regUnion = Nothing
                        End If
                    Else
                        If imgBitmap.GetPixel(col, row).Equals(BackColor) = False Then
                            If tmpStartCol = -1 Then tmpStartCol = col
                        ElseIf imgBitmap.GetPixel(col, row).Equals(BackColor) = True Then
                            If tmpStartCol <> -1 Then
                                tmpStopCol = col

                                Dim regUnion As New Region(New Rectangle(tmpStartCol, row, tmpStopCol - tmpStartCol, 1))
                                reg.Union(regUnion)
                                regunion = Nothing

                                tmpStartCol = -1
                                tmpStopCol = -1
                            End If
                        End If
                    End If
                Next
            Next
        End If
    End Sub

    Public Sub SetFormBitmapSize(ByRef frmForm As Form, ByVal imgBitmap As Bitmap)
        frmForm.Height = imgBitmap.Height
        frmForm.Width = imgBitmap.Width
    End Sub
    Public Sub SetFormBitmapSize(ByRef frmForm As Form, ByVal BitmapFileName As String)
        Dim imgBitmap As Bitmap = Bitmap.FromFile(BitmapFileName)

        frmForm.Height = imgBitmap.Height
        frmForm.Width = imgBitmap.Width
    End Sub
    Public Sub SetFormBitmapSize(ByRef frmForm As Size, ByVal imgBitmap As Bitmap)
        frmForm.Height = imgBitmap.Height
        frmForm.Width = imgBitmap.Width
    End Sub
    Public Sub SetFormBitmapSize(ByRef frmForm As Size, ByVal BitmapFileName As String)
        Dim imgBitmap As Bitmap = Bitmap.FromFile(BitmapFileName)

        frmForm.Height = imgBitmap.Height
        frmForm.Width = imgBitmap.Width
    End Sub

    Public Function LoadBitmapFile(ByVal BitmapFileName As String) As Bitmap
        Return Bitmap.FromFile(BitmapFileName)
    End Function
    Public Sub ApplyBitmapFile(ByRef frmForm As Form, ByVal BitmapFileName As String)
        Dim tmpBitmap As Bitmap = Bitmap.FromFile(BitmapFileName)

        frmForm.BackgroundImage = tmpBitmap
    End Sub
    Public Sub ApplyBitmapFile(ByRef imgBitmap As Bitmap, ByVal BitmapFileName As String)
        imgBitmap = Bitmap.FromFile(BitmapFileName)
    End Sub
    Public Sub ApplyBitmap(ByRef oldImgBitmap As Bitmap, ByRef newImgBitmap As Bitmap)
        oldImgBitmap = newImgBitmap
    End Sub
    Public Sub ApplyBitmap(ByRef frmForm As Form, ByRef newImgBitmap As Bitmap)
        frmForm.BackgroundImage = newImgBitmap
    End Sub
    Public Sub ApplyRegion(ByRef frmForm As Form, ByRef newRegion As Region)
        frmForm.Region = newRegion
    End Sub
    Public Sub ApplyRegion(ByRef oldRegion As Region, ByRef newRegion As Region)
        oldRegion = newRegion
    End Sub
    'Public Function GetBitmapRegion(ByVal BitmapFileName As String) As Region
    '    Dim imgBitmap As Bitmap = Bitmap.FromFile(BitmapFileName)

    '    If Not (imgBitmap Is Nothing) Then
    '        Return baseGetBitmapRegion(imgBitmap, imgBitmap.GetPixel(0, 0))
    '    End If
    'End Function
    'Public Function GetBitmapRegion(ByVal imgBitmap As Bitmap) As Region
    '    If Not (imgBitmap Is Nothing) Then
    '        Return baseGetBitmapRegion(imgBitmap, imgBitmap.GetPixel(0, 0))
    '    End If
    'End Function
    'Public Function GetBitmapRegion(ByVal imgBitmap As Bitmap, ByVal TransparentColor As Color) As Region
    '    If Not (imgBitmap Is Nothing) Then
    '        Return baseGetBitmapRegion(imgBitmap, TransparentColor)
    '    End If
    'End Function
    'Public Function GetBitmapRegion(ByVal BitmapFileName As String, ByVal TransparentColor As Color) As Region
    '    Dim imgBitmap As Bitmap = Bitmap.FromFile(BitmapFileName)

    '    If Not (imgBitmap Is Nothing) Then
    '        Return baseGetBitmapRegion(imgBitmap, TransparentColor)
    '    End If
    'End Function
    'Public Function GetBitmapRegion(ByVal frmForm As Form, ByVal TransparentColor As Color) As Region
    '    If Not (frmForm.BackgroundImage Is Nothing) Then
    '        Return baseGetBitmapRegion(frmForm.BackgroundImage, TransparentColor)
    '    End If
    'End Function
    'Private Function baseGetBitmapRegion(ByVal imgBitmap As Bitmap, ByVal TransparentColor As Color) As Region
    '    Dim tmpLocalReg As Region
    '    If Not (imgBitmap Is Nothing) Then
    '        Dim BackColor As Color, row As Integer, col As Integer
    '        tmpLocalReg = New Region(New Rectangle(0, 0, 0, 0))

    '        BackColor = TransparentColor

    '        For row = 0 To imgBitmap.Height - 1
    '            Dim tmpStartCol As Integer = -1
    '            Dim tmpStopCol As Integer = -1

    '            For col = 0 To imgBitmap.Width
    '                If col = imgBitmap.Width Then
    '                    If tmpStartCol <> -1 Then
    '                        tmpStopCol = col

    '                        Dim regUnion As New Region(New Rectangle(tmpStartCol, row, tmpStopCol - tmpStartCol, 1))
    '                        tmpLocalReg.Union(regUnion)
    '                        regUnion = Nothing
    '                    End If
    '                Else
    '                    If imgBitmap.GetPixel(col, row).Equals(BackColor) = False Then
    '                        If tmpStartCol = -1 Then tmpStartCol = col
    '                    ElseIf imgBitmap.GetPixel(col, row).Equals(BackColor) = True Then
    '                        If tmpStartCol <> -1 Then
    '                            tmpStopCol = col

    '                            Dim regUnion As New Region(New Rectangle(tmpStartCol, row, tmpStopCol - tmpStartCol, 1))
    '                            tmpLocalReg.Union(regUnion)
    '                            regunion = Nothing

    '                            tmpStartCol = -1
    '                            tmpStopCol = -1
    '                        End If
    '                    End If
    '                End If
    '            Next
    '        Next
    '    End If
    '    Return tmpLocalReg
    'End Function
End Class