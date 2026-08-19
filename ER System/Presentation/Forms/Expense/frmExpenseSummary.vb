Public Class frmExpenseSummary

    Private Sub frmExpenseSummary_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadingDepartment()
    End Sub

    Private Sub btnView_Click(sender As Object, e As EventArgs) Handles btnView.Click
        ExpenseSummaryDateFrom = dtpStartDate.Text
        ExpenseSummaryDateTo = dtpEndDate.Text
        frmrptExpenseSummary.ShowDialog()
    End Sub
End Class