Public Class frmHelp
    Dim status As String = "0"
    Dim status1 As String = "0"
    Dim status2 As String = "0"
    Dim status3 As String = "0"
    Dim status4 As String = "0"
    Dim status5 As String = "0"
    Dim status6 As String = "0"
    Dim status7 As String = "0"
    Dim status8 As String = "0"
    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked

        If status = "0" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "AccountSettings-Signatory.png")
            status = "1"
        Else
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "Add Signatory.png")
            status = "0"
        End If
    End Sub

    Private Sub LinkLabel2_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel2.LinkClicked
        If status1 = "0" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "AccountSettings-ChangePassword.png")
            status1 = "1"
        ElseIf status1 = "1" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "ChangePassword.png")
            status1 = "2"
        Else
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "ChangeEmailSettings.png")
            status1 = "0"
        End If
    End Sub

    Private Sub LinkLabel3_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel3.LinkClicked
        If status2 = "0" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "AddReport-Expense.png")
            status2 = "1"
        ElseIf status2 = "1" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "Add ER-Reimbursment Title.png")
            status2 = "2"
        ElseIf status2 = "2" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "ER-Reimbursment added.png")
            status2 = "3"
        ElseIf status2 = "3" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "Adding Expense.png")
            status2 = "4"
        Else
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "updateExpenseData.png")
            status2 = "0"
        End If
    End Sub

    Private Sub LinkLabel7_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel7.LinkClicked
        If status3 = "0" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "ER-Reimbursment added.png")
            status3 = "1"
        ElseIf status3 = "1" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "File the report.png")
            status3 = "2"
        Else
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "FiledReportDisplay.png")
            status3 = "0"
        End If
    End Sub
    Private Sub LinkLabel6_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel6.LinkClicked
        If status4 = "0" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "ER-Reimbursment added.png")
            status4 = "1"
        Else
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "Sending your ER.png")
            status4 = "0"
        End If
    End Sub

    Private Sub LinkLabel8_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel8.LinkClicked
        If status5 = "0" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "ER-Reimbursment added.png")
            status5 = "1"
        Else
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "Approve report.png")
            status5 = "0"
        End If
    End Sub

    Private Sub LinkLabel4_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel4.LinkClicked
        If status6 = "0" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "PendingER.png")
            status6 = "1"
        ElseIf status6 = "1" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "PendingFiledER.png")
            status6 = "2"
        Else
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "Approved the FileER.png")
            status6 = "0"
        End If
    End Sub

    Private Sub LinkLabel9_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel9.LinkClicked
        If status7 = "0" Then
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "PreviousER.png")
            status7 = "1"
        Else
            PictureBox1.Image = Image.FromFile(Application.StartupPath + "\Help\" + "PreviousER2.png")
            status7 = "0"
        End If
    End Sub
End Class