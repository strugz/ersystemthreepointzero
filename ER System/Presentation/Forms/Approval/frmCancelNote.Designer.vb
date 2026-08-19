<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmCancelNote
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.rtbNote = New System.Windows.Forms.RichTextBox()
        Me.btnOkay = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'rtbNote
        '
        Me.rtbNote.Location = New System.Drawing.Point(1, 0)
        Me.rtbNote.Name = "rtbNote"
        Me.rtbNote.Size = New System.Drawing.Size(282, 203)
        Me.rtbNote.TabIndex = 0
        Me.rtbNote.Text = ""
        '
        'btnOkay
        '
        Me.btnOkay.Location = New System.Drawing.Point(177, 209)
        Me.btnOkay.Name = "btnOkay"
        Me.btnOkay.Size = New System.Drawing.Size(95, 23)
        Me.btnOkay.TabIndex = 1
        Me.btnOkay.Text = "Okay"
        Me.btnOkay.UseVisualStyleBackColor = True
        '
        'frmCancelNote
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(284, 239)
        Me.Controls.Add(Me.btnOkay)
        Me.Controls.Add(Me.rtbNote)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.Name = "frmCancelNote"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Cancel Note"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents rtbNote As System.Windows.Forms.RichTextBox
    Friend WithEvents btnOkay As System.Windows.Forms.Button
End Class
