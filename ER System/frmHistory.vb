Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class frmHistory

    Private Sub History_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Label1.Text = "Search By: " + modLoadingData.DataToLoad
        Me.txtName.Select()
    End Sub
    Private Sub dgvHistory_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvHistory.CellDoubleClick
        If e.RowIndex > -1 Then
            If modLoadingData.DataToLoad = "Hospital" Then
                frmEReport.txtLocation.Text = dgvHistory.Rows(e.RowIndex).Cells("name").Value
                SaveHospitalID(dgvHistory.Rows(e.RowIndex).Cells("objectid").Value)
                dgvHistory.DataSource = DgvInstrumentSearch(dgvHistory.Rows(e.RowIndex).Cells("objectid").Value)
                If dgvHistory.Rows.Count <> 0 Then
                    Dim chkbox As New DataGridViewCheckBoxColumn With {.FlatStyle = FlatStyle.Standard}
                    Me.dgvHistory.Columns.Insert(0, chkbox)
                    Me.dgvHistory.Columns(0).Width = 30
                    dgvHistory.Columns(1).HeaderText = "Instrument Name"
                    dgvHistory.Columns(3).HeaderText = "Serial Number"
                    dgvHistory.Columns(0).SortMode = DataGridViewColumnSortMode.NotSortable
                    dgvHistory.Columns(1).SortMode = DataGridViewColumnSortMode.NotSortable
                    dgvHistory.Columns(2).SortMode = DataGridViewColumnSortMode.NotSortable
                    dgvHistory.Columns(3).SortMode = DataGridViewColumnSortMode.NotSortable
                    dgvHistory.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                    dgvHistory.Columns(2).Visible = False
                    dgvHistory.Columns(1).ReadOnly = True
                    dgvHistory.Columns(2).ReadOnly = True
                End If
                modLoadingData.DataToLoad = "Instrument"
            End If
            Call FrmValidation(frmEReport.txtLocation.Text)
        End If
    End Sub
    Private Sub frmHistory_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        txtName.Clear()
        dgvHistory.DataSource = Nothing
    End Sub
    Private Sub BTNSearch_Click(sender As Object, e As EventArgs) Handles BTNSearch.Click
        If modLoadingData.DataToLoad = "Hospital" Then
            dgvHistory.DataSource = Nothing
            dgvHistory.Rows.Clear()
            dgvHistory.Columns.Clear()
            dgvHistory.DataSource = DgvHospitalSearch(txtName.Text)
            If dgvHistory.Rows.Count <> 0 Then
                dgvHistory.Columns(0).HeaderText = "ID"
                dgvHistory.Columns(2).HeaderText = "Hospital Name"
                dgvHistory.Columns(2).ReadOnly = True
                dgvHistory.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                dgvHistory.Columns(2).SortMode = DataGridViewColumnSortMode.NotSortable
                dgvHistory.Columns("objectid").Visible = False
                dgvHistory.Columns("acronym").Visible = False
                dgvHistory.Columns("tbinstruments").Visible = False
            End If
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles BTNInsert.Click
        Dim StrInstrument As String = ""
        Dim StrSerial As String = ""
        For i As Integer = 0 To dgvHistory.RowCount - 1
            If dgvHistory.Item(0, i).Value = "True" Then
                StrInstrument = IIf(StrInstrument = "", "", StrInstrument + "/") + dgvHistory.Item(3, i).Value
                StrSerial = IIf(StrSerial = "", "", StrSerial + "/") + dgvHistory.Item(4, i).Value
            End If
        Next
        frmEReport.txtInstrument.Text = StrInstrument
        frmEReport.txtSerialNumber.Text = StrSerial
        Me.Close()
    End Sub

    Private Sub dgvHistory_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvHistory.CellClick
        If modLoadingData.DataToLoad = "Instrument" Then
            If dgvHistory.Item(0, e.RowIndex).Value = True Then
                dgvHistory.Rows(e.RowIndex).Cells(0).Value = False
            Else
                dgvHistory.Rows(e.RowIndex).Cells(0).Value = True
            End If
        End If
    End Sub
End Class