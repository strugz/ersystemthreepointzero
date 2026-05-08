Public Class frmPreviousER
    Public userID As String
    Public sdate As String
    Public edate As String
    Public reportID As String

    Private Sub frmPreviousER_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Me.TopMost = False
        frmMain.TopMost = True
        conn.Close()
    End Sub

    Private Sub frmPreviousER_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Me.KeyPreview = True
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub

    Private Sub frmPreviousER_Leave(sender As Object, e As EventArgs) Handles Me.Leave

    End Sub
    Private Sub frmPreviousER_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ConnectionPreviousER()
        Me.TopMost = True
        frmMain.TopMost = False
    End Sub
    Private Sub btnViewReport_Click(sender As Object, e As EventArgs) Handles btnViewReport.Click
        loadingPreviousER(txtUserID.Text, "01/01/1990", Date.Now.ToString("MM/dd/yyyy"))
        DataGridView1.Columns(3).Visible = False
        DataGridView1.Columns(4).Visible = False
        DataGridView1.Columns(5).Visible = False
        DataGridView1.Columns(6).Visible = False
        DataGridView1.Columns(7).Visible = False
        DataGridView1.Columns(8).Visible = False
        DataGridView1.Columns(9).Visible = False
        'DataGridView1.Columns(10).Visible = False
        DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Me.TopMost = False
        frmPreviousERExpense.TopMost = True
    End Sub

    Private Sub DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick

    End Sub

    Private Sub DataGridView1_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellDoubleClick
        Try
            sdate = DataGridView1.Rows(e.RowIndex).Cells(0).Value
            edate = DataGridView1.Rows(e.RowIndex).Cells(1).Value
            userID = DataGridView1.Rows(e.RowIndex).Cells(3).Value
            reportID = DataGridView1.Rows(e.RowIndex).Cells(9).Value
        Catch ex As Exception
        End Try
        LoadingExpenseER(Me.reportID, Me.userID, Me.sdate, Me.edate)
        frmPreviousERExpense.ShowDialog()
    End Sub
End Class