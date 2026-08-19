Public Class frmSignature
    Dim picname As String
    Private Sub frmSignature_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'DataGridView1.SortedColumn.SortMode = DataGridViewColumnSortMode.NotSortable
    End Sub
    Private Sub DataGridView1_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellDoubleClick
        Try
            If IsDBNull(DataGridView1.Rows(e.RowIndex).Cells("Signature").Value) = True Then
                frmDeptSignature.picSignature.Image = Nothing
                MsgBox("No Signature")
            Else
                Dim ms As New IO.MemoryStream(CType(DataGridView1.Rows(e.RowIndex).Cells("Signature").Value, Byte()))
                Dim img As Image = Image.FromStream(ms)
                picname = img.ToString
                frmDeptSignature.picSignature.Image = img
            End If
        Catch ex As Exception

        End Try
  
        Me.Hide()
    End Sub
End Class