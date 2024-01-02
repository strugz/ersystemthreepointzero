<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmExpenseDetails
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
        Me.dgvViewingExpenseDetails = New System.Windows.Forms.DataGridView()
        Me.cbbEmployeeDept = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.btnView = New System.Windows.Forms.Button()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.cbbClientName = New System.Windows.Forms.ComboBox()
        CType(Me.dgvViewingExpenseDetails, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'dgvViewingExpenseDetails
        '
        Me.dgvViewingExpenseDetails.AllowUserToAddRows = False
        Me.dgvViewingExpenseDetails.AllowUserToDeleteRows = False
        Me.dgvViewingExpenseDetails.AllowUserToResizeColumns = False
        Me.dgvViewingExpenseDetails.AllowUserToResizeRows = False
        Me.dgvViewingExpenseDetails.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgvViewingExpenseDetails.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvViewingExpenseDetails.Location = New System.Drawing.Point(0, 91)
        Me.dgvViewingExpenseDetails.Name = "dgvViewingExpenseDetails"
        Me.dgvViewingExpenseDetails.ReadOnly = True
        Me.dgvViewingExpenseDetails.RowHeadersVisible = False
        Me.dgvViewingExpenseDetails.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Me.dgvViewingExpenseDetails.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvViewingExpenseDetails.Size = New System.Drawing.Size(881, 380)
        Me.dgvViewingExpenseDetails.TabIndex = 0
        Me.dgvViewingExpenseDetails.TabStop = False
        '
        'cbbEmployeeDept
        '
        Me.cbbEmployeeDept.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbbEmployeeDept.FormattingEnabled = True
        Me.cbbEmployeeDept.Location = New System.Drawing.Point(106, 12)
        Me.cbbEmployeeDept.Name = "cbbEmployeeDept"
        Me.cbbEmployeeDept.Size = New System.Drawing.Size(200, 21)
        Me.cbbEmployeeDept.TabIndex = 3
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.Label1.Location = New System.Drawing.Point(13, 15)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(62, 13)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Department"
        '
        'btnView
        '
        Me.btnView.Location = New System.Drawing.Point(312, 15)
        Me.btnView.Name = "btnView"
        Me.btnView.Size = New System.Drawing.Size(111, 52)
        Me.btnView.TabIndex = 13
        Me.btnView.Text = "View"
        Me.btnView.UseVisualStyleBackColor = True
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.PictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PictureBox1.ErrorImage = Nothing
        Me.PictureBox1.Location = New System.Drawing.Point(0, 0)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(881, 471)
        Me.PictureBox1.TabIndex = 14
        Me.PictureBox1.TabStop = False
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.Label2.Location = New System.Drawing.Point(13, 54)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(64, 13)
        Me.Label2.TabIndex = 16
        Me.Label2.Text = "Client Name"
        '
        'cbbClientName
        '
        Me.cbbClientName.FormattingEnabled = True
        Me.cbbClientName.Location = New System.Drawing.Point(106, 51)
        Me.cbbClientName.Name = "cbbClientName"
        Me.cbbClientName.Size = New System.Drawing.Size(200, 21)
        Me.cbbClientName.TabIndex = 15
        '
        'frmExpenseDetails
        '
        Me.AcceptButton = Me.btnView
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(881, 471)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.cbbClientName)
        Me.Controls.Add(Me.btnView)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.cbbEmployeeDept)
        Me.Controls.Add(Me.dgvViewingExpenseDetails)
        Me.Controls.Add(Me.PictureBox1)
        Me.Name = "frmExpenseDetails"
        Me.Text = "Expense Details"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        CType(Me.dgvViewingExpenseDetails, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents dgvViewingExpenseDetails As System.Windows.Forms.DataGridView
    Friend WithEvents cbbEmployeeDept As System.Windows.Forms.ComboBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents btnView As System.Windows.Forms.Button
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents cbbClientName As System.Windows.Forms.ComboBox
End Class
