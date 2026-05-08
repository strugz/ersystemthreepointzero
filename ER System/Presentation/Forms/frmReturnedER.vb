Public Class frmReturnedER

    Private Sub frmReturnedER_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.TopMost = True
        frmMain.TopMost = False
        Dim dt As New DataTable
        dt.Reset()
        LoadReturnedER(modLoadingData.LoginuserID, dt)
        DataGridView1.DataSource = dt
        DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    End Sub
End Class