Public Class frmExpenseDetails
    Public dtLoadExpenseDetails As New DataTable

    Private Sub frmExpenseDetails_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        dgvViewingExpenseDetails.DataSource = Nothing
        dtLoadExpenseDetails.Reset()
    End Sub
    Private Sub frmExpenseDetails_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadingDepartment()
        LoadClient()
    End Sub
    Private Sub btnView_Click(sender As Object, e As EventArgs) Handles btnView.Click

        LoadExpenseDetails(cbbClientName.Text, cbbEmployeeDept.SelectedValue.ToString)
        dgvViewingExpenseDetails.Columns("Description").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvViewingExpenseDetails.Columns("Particulars").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvViewingExpenseDetails.Columns("Service Number").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvViewingExpenseDetails.Columns("Location").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvViewingExpenseDetails.Columns("Employee").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvViewingExpenseDetails.Columns(0).SortMode = DataGridViewColumnSortMode.NotSortable
        dgvViewingExpenseDetails.Columns(1).SortMode = DataGridViewColumnSortMode.NotSortable
        dgvViewingExpenseDetails.Columns(2).SortMode = DataGridViewColumnSortMode.NotSortable
        dgvViewingExpenseDetails.Columns(3).SortMode = DataGridViewColumnSortMode.NotSortable
        dgvViewingExpenseDetails.Columns(4).SortMode = DataGridViewColumnSortMode.NotSortable
        dgvViewingExpenseDetails.Columns(5).SortMode = DataGridViewColumnSortMode.NotSortable
        dgvViewingExpenseDetails.Columns(6).SortMode = DataGridViewColumnSortMode.NotSortable
        dgvViewingExpenseDetails.Columns(7).SortMode = DataGridViewColumnSortMode.NotSortable
    End Sub
    Private Sub cbbEmployeeDept_SelectedValueChanged(sender As Object, e As EventArgs) Handles cbbEmployeeDept.SelectedValueChanged
        If cbbEmployeeDept.SelectedValue.ToString = "System.Data.DataRowView" Then
        Else
            LoadingUserAccount(cbbEmployeeDept.SelectedValue.ToString)
        End If
    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click

    End Sub
End Class