Public Class frmPreviousERExpense

    Private Sub frmPreviousERExpense_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Me.TopMost = False
        frmPreviousER.TopMost = True
    End Sub

    Private Sub frmPreviousERExpense_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Me.KeyPreview = True
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub

    Private Sub frmPreviousERExpense_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.DataGridView1.Columns(0).Visible = False
        Me.DataGridView1.Columns(5).Visible = False
        Me.DataGridView1.Columns(10).Visible = False
        Me.DataGridView1.Columns(11).Visible = False
        Me.DataGridView1.Columns(14).Visible = False
        Me.DataGridView1.Columns(15).Visible = False
        Me.DataGridView1.Columns(16).Visible = False
        Me.DataGridView1.Columns(3).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        Me.DataGridView1.Columns(3).DefaultCellStyle.WrapMode = DataGridViewTriState.True
        Me.DataGridView1.Columns(13).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        Me.DataGridView1.Columns(3).DefaultCellStyle.WrapMode = DataGridViewTriState.True
        Me.TopMost = True
        frmPreviousER.TopMost = False
    End Sub

    Private Sub DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick

    End Sub
End Class