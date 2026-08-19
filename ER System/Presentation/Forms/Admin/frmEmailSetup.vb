Public Class frmEmailSetup

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnAdd.Click

    End Sub

    Private Sub frmEmailSetup_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Me.KeyPreview = True
        If e.KeyCode = Keys.Escape Then
            Me.Close()

        End If
    End Sub

    Private Sub frmEmailSetup_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class