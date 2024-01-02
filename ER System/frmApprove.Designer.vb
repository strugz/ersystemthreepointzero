<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmApprove
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
        Me.components = New System.ComponentModel.Container()
        Me.dgvUser = New System.Windows.Forms.DataGridView()
        Me.dgvUserReportDetails = New System.Windows.Forms.DataGridView()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.btnApprove = New System.Windows.Forms.Button()
        Me.btnReportViewer = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.btnReject = New System.Windows.Forms.Button()
        Me.CMSEditUserExpense = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.EditExpenseToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.BTNRefresh = New System.Windows.Forms.Button()
        CType(Me.dgvUser, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvUserReportDetails, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.CMSEditUserExpense.SuspendLayout()
        Me.SuspendLayout()
        '
        'dgvUser
        '
        Me.dgvUser.AllowUserToAddRows = False
        Me.dgvUser.AllowUserToDeleteRows = False
        Me.dgvUser.AllowUserToResizeColumns = False
        Me.dgvUser.AllowUserToResizeRows = False
        Me.dgvUser.BackgroundColor = System.Drawing.SystemColors.Control
        Me.dgvUser.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvUser.Location = New System.Drawing.Point(12, 81)
        Me.dgvUser.Name = "dgvUser"
        Me.dgvUser.ReadOnly = True
        Me.dgvUser.RowHeadersVisible = False
        Me.dgvUser.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Me.dgvUser.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.dgvUser.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvUser.Size = New System.Drawing.Size(254, 443)
        Me.dgvUser.TabIndex = 0
        '
        'dgvUserReportDetails
        '
        Me.dgvUserReportDetails.AllowUserToAddRows = False
        Me.dgvUserReportDetails.AllowUserToDeleteRows = False
        Me.dgvUserReportDetails.AllowUserToResizeColumns = False
        Me.dgvUserReportDetails.AllowUserToResizeRows = False
        Me.dgvUserReportDetails.BackgroundColor = System.Drawing.Color.WhiteSmoke
        Me.dgvUserReportDetails.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvUserReportDetails.Location = New System.Drawing.Point(272, 81)
        Me.dgvUserReportDetails.Name = "dgvUserReportDetails"
        Me.dgvUserReportDetails.ReadOnly = True
        Me.dgvUserReportDetails.RowHeadersVisible = False
        Me.dgvUserReportDetails.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Me.dgvUserReportDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.dgvUserReportDetails.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvUserReportDetails.Size = New System.Drawing.Size(515, 443)
        Me.dgvUserReportDetails.TabIndex = 1
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.Black
        Me.Label1.Location = New System.Drawing.Point(12, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(56, 20)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Users"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.Black
        Me.Label2.Location = New System.Drawing.Point(268, 9)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(125, 20)
        Me.Label2.TabIndex = 3
        Me.Label2.Text = "Report Details"
        '
        'btnApprove
        '
        Me.btnApprove.BackColor = System.Drawing.Color.Lime
        Me.btnApprove.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnApprove.Enabled = False
        Me.btnApprove.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnApprove.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnApprove.ForeColor = System.Drawing.Color.Black
        Me.btnApprove.Location = New System.Drawing.Point(486, 34)
        Me.btnApprove.Name = "btnApprove"
        Me.btnApprove.Size = New System.Drawing.Size(105, 41)
        Me.btnApprove.TabIndex = 4
        Me.btnApprove.Text = "Approve"
        Me.btnApprove.UseVisualStyleBackColor = False
        '
        'btnReportViewer
        '
        Me.btnReportViewer.BackColor = System.Drawing.Color.White
        Me.btnReportViewer.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnReportViewer.Enabled = False
        Me.btnReportViewer.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnReportViewer.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnReportViewer.ForeColor = System.Drawing.Color.Black
        Me.btnReportViewer.Location = New System.Drawing.Point(359, 34)
        Me.btnReportViewer.Name = "btnReportViewer"
        Me.btnReportViewer.Size = New System.Drawing.Size(121, 41)
        Me.btnReportViewer.TabIndex = 5
        Me.btnReportViewer.Text = "Preview"
        Me.btnReportViewer.UseVisualStyleBackColor = False
        '
        'btnCancel
        '
        Me.btnCancel.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.btnCancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnCancel.Enabled = False
        Me.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCancel.ForeColor = System.Drawing.Color.Black
        Me.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnCancel.Location = New System.Drawing.Point(272, 34)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(81, 41)
        Me.btnCancel.TabIndex = 6
        Me.btnCancel.Text = "Reset"
        Me.btnCancel.UseVisualStyleBackColor = False
        '
        'btnReject
        '
        Me.btnReject.BackColor = System.Drawing.Color.White
        Me.btnReject.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnReject.Enabled = False
        Me.btnReject.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.btnReject.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnReject.ForeColor = System.Drawing.Color.Black
        Me.btnReject.Location = New System.Drawing.Point(633, 34)
        Me.btnReject.Name = "btnReject"
        Me.btnReject.Size = New System.Drawing.Size(145, 41)
        Me.btnReject.TabIndex = 8
        Me.btnReject.Text = "Return File for Modification"
        Me.btnReject.UseVisualStyleBackColor = True
        '
        'CMSEditUserExpense
        '
        Me.CMSEditUserExpense.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.EditExpenseToolStripMenuItem})
        Me.CMSEditUserExpense.Name = "CMSEditUserExpense"
        Me.CMSEditUserExpense.Size = New System.Drawing.Size(141, 26)
        '
        'EditExpenseToolStripMenuItem
        '
        Me.EditExpenseToolStripMenuItem.Name = "EditExpenseToolStripMenuItem"
        Me.EditExpenseToolStripMenuItem.Size = New System.Drawing.Size(140, 22)
        Me.EditExpenseToolStripMenuItem.Text = "Edit Expense"
        '
        'BTNRefresh
        '
        Me.BTNRefresh.BackColor = System.Drawing.Color.LightGreen
        Me.BTNRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.BTNRefresh.Location = New System.Drawing.Point(12, 34)
        Me.BTNRefresh.Name = "BTNRefresh"
        Me.BTNRefresh.Size = New System.Drawing.Size(254, 41)
        Me.BTNRefresh.TabIndex = 9
        Me.BTNRefresh.Text = "Refresh User"
        Me.BTNRefresh.UseVisualStyleBackColor = False
        '
        'frmApprove
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.ClientSize = New System.Drawing.Size(790, 528)
        Me.Controls.Add(Me.BTNRefresh)
        Me.Controls.Add(Me.btnReject)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.btnReportViewer)
        Me.Controls.Add(Me.btnApprove)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.dgvUserReportDetails)
        Me.Controls.Add(Me.dgvUser)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.KeyPreview = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmApprove"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        CType(Me.dgvUser, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvUserReportDetails, System.ComponentModel.ISupportInitialize).EndInit()
        Me.CMSEditUserExpense.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents dgvUser As System.Windows.Forms.DataGridView
    Friend WithEvents dgvUserReportDetails As System.Windows.Forms.DataGridView
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents btnApprove As System.Windows.Forms.Button
    Friend WithEvents btnReportViewer As System.Windows.Forms.Button
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents btnReject As System.Windows.Forms.Button
    Friend WithEvents CMSEditUserExpense As ContextMenuStrip
    Friend WithEvents EditExpenseToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents BTNRefresh As Button
End Class
