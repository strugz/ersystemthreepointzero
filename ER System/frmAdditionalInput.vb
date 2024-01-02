Public Class frmAdditionalInput
    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Dim StrLength As String = ""
        Dim IntLength As Integer = 0
        For i As Integer = 0 To dgvUser.RowCount - 1
            If dgvUser.Item(0, i).Value = "True" Then
                StrLength += dgvUser.Item(1, i).Value + "/"
            End If
        Next
        If StrLength <> "" Then
            IntLength = StrLength.Length
            StrLength = StrLength.Remove(IntLength - 1)
            If ModDataStore.ExpenseReportEdit = "1" Then
                FrmUserExpenseList.txtWorkWith.Text = StrLength
            Else
                frmEReport.txtWorkWith.Text = StrLength
            End If
            modLoadingData.WorkWith = StrLength
            Me.Close()
        Else
            If ModDataStore.ExpenseReportEdit = "1" Then
                FrmUserExpenseList.txtWorkWith.Text = "NONE"
            Else
                frmEReport.txtWorkWith.Text = "NONE"
            End If
            modLoadingData.WorkWith = "NONE"
            Me.Close()
        End If
    End Sub
    Public Sub CheckedDataGrid()
        Dim StrSplitLenght() As String = {""}
        If ModDataStore.ExpenseReportEdit = "1" Then
            StrSplitLenght = IIf(modLoadingData.WorkWith = "", FrmUserExpenseList.txtWorkWith.Text.Split("/"),
                     modLoadingData.WorkWith.Split("/"))
        Else
            StrSplitLenght = IIf(modLoadingData.WorkWith = "", frmEReport.txtWorkWith.Text.Split("/"),
                     modLoadingData.WorkWith.Split("/"))
        End If
        For index As Integer = 0 To IIf(StrSplitLenght.Length = 0, 0, StrSplitLenght.Length - 1)
            For i As Integer = 0 To Me.dgvUser.Rows.Count - 1
                If Me.dgvUser.Rows(i).Cells(1).Value = IIf(StrSplitLenght(index) = "", modLoadingData.WorkWith,
                                                           StrSplitLenght(index)) Then
                    Me.dgvUser.Rows(i).Cells(0).Value = True
                    Exit For
                End If
            Next
        Next
    End Sub

    Private Sub frmAdditionalInput_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        If modLoadingData.WorkWith = "" Then
            If ModDataStore.ExpenseReportEdit = "1" Then
                FrmUserExpenseList.txtWorkWith.Text = "NONE"
            Else
                frmEReport.txtWorkWith.Text = "NONE"
            End If
            modLoadingData.WorkWith = "NONE"

        End If
        Me.dgvUser.DataSource = Nothing
        Me.dgvUser.Columns.Clear()
    End Sub

    Private Sub frmAdditionalInput_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        CheckedDataGrid()
    End Sub
End Class