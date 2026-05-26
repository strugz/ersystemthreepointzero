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
        Me.mainLayout = New System.Windows.Forms.TableLayoutPanel()
        Me.headerPanel = New System.Windows.Forms.Panel()
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.lblSubtitle = New System.Windows.Forms.Label()
        Me.actionPanel = New System.Windows.Forms.FlowLayoutPanel()
        Me.contentLayout = New System.Windows.Forms.TableLayoutPanel()
        Me.userPanel = New System.Windows.Forms.Panel()
        Me.reportPanel = New System.Windows.Forms.Panel()
        CType(Me.dgvUser, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvUserReportDetails, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.CMSEditUserExpense.SuspendLayout()
        Me.mainLayout.SuspendLayout()
        Me.headerPanel.SuspendLayout()
        Me.actionPanel.SuspendLayout()
        Me.contentLayout.SuspendLayout()
        Me.userPanel.SuspendLayout()
        Me.reportPanel.SuspendLayout()
        Me.SuspendLayout()
        '
        'dgvUser
        '
        Me.dgvUser.AllowUserToAddRows = False
        Me.dgvUser.AllowUserToDeleteRows = False
        Me.dgvUser.AllowUserToResizeColumns = False
        Me.dgvUser.AllowUserToResizeRows = False
        Me.dgvUser.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvUser.BackgroundColor = System.Drawing.Color.White
        Me.dgvUser.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.dgvUser.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvUser.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvUser.Location = New System.Drawing.Point(0, 36)
        Me.dgvUser.MultiSelect = False
        Me.dgvUser.Name = "dgvUser"
        Me.dgvUser.ReadOnly = True
        Me.dgvUser.RowHeadersVisible = False
        Me.dgvUser.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Me.dgvUser.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.dgvUser.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvUser.Size = New System.Drawing.Size(300, 493)
        Me.dgvUser.TabIndex = 0
        '
        'dgvUserReportDetails
        '
        Me.dgvUserReportDetails.AllowUserToAddRows = False
        Me.dgvUserReportDetails.AllowUserToDeleteRows = False
        Me.dgvUserReportDetails.AllowUserToResizeColumns = False
        Me.dgvUserReportDetails.AllowUserToResizeRows = False
        Me.dgvUserReportDetails.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvUserReportDetails.BackgroundColor = System.Drawing.Color.White
        Me.dgvUserReportDetails.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.dgvUserReportDetails.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvUserReportDetails.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvUserReportDetails.Location = New System.Drawing.Point(0, 36)
        Me.dgvUserReportDetails.MultiSelect = False
        Me.dgvUserReportDetails.Name = "dgvUserReportDetails"
        Me.dgvUserReportDetails.ReadOnly = True
        Me.dgvUserReportDetails.RowHeadersVisible = False
        Me.dgvUserReportDetails.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Me.dgvUserReportDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.dgvUserReportDetails.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvUserReportDetails.Size = New System.Drawing.Size(640, 493)
        Me.dgvUserReportDetails.TabIndex = 1
        '
        'Label1
        '
        Me.Label1.AutoSize = False
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Dock = System.Windows.Forms.DockStyle.Top
        Me.Label1.Font = New System.Drawing.Font("Segoe UI Semibold", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(37, Byte), Integer), CType(CType(41, Byte), Integer))
        Me.Label1.Location = New System.Drawing.Point(0, 0)
        Me.Label1.Padding = New System.Windows.Forms.Padding(0, 8, 0, 8)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(300, 36)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Users awaiting review"
        '
        'Label2
        '
        Me.Label2.AutoSize = False
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.Dock = System.Windows.Forms.DockStyle.Top
        Me.Label2.Font = New System.Drawing.Font("Segoe UI Semibold", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(37, Byte), Integer), CType(CType(41, Byte), Integer))
        Me.Label2.Location = New System.Drawing.Point(0, 0)
        Me.Label2.Padding = New System.Windows.Forms.Padding(0, 8, 0, 8)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(640, 36)
        Me.Label2.TabIndex = 3
        Me.Label2.Text = "Report details"
        '
        'btnApprove
        '
        Me.btnApprove.BackColor = System.Drawing.Color.FromArgb(CType(CType(43, Byte), Integer), CType(CType(142, Byte), Integer), CType(CType(85, Byte), Integer))
        Me.btnApprove.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnApprove.Enabled = False
        Me.btnApprove.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnApprove.Font = New System.Drawing.Font("Segoe UI Semibold", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnApprove.ForeColor = System.Drawing.Color.White
        Me.btnApprove.Location = New System.Drawing.Point(355, 0)
        Me.btnApprove.Margin = New System.Windows.Forms.Padding(8, 0, 0, 0)
        Me.btnApprove.Name = "btnApprove"
        Me.btnApprove.Size = New System.Drawing.Size(104, 40)
        Me.btnApprove.TabIndex = 4
        Me.btnApprove.Text = "Approve"
        Me.btnApprove.UseVisualStyleBackColor = False
        '
        'btnReportViewer
        '
        Me.btnReportViewer.BackColor = System.Drawing.Color.White
        Me.btnReportViewer.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnReportViewer.Enabled = False
        Me.btnReportViewer.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnReportViewer.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnReportViewer.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(37, Byte), Integer), CType(CType(41, Byte), Integer))
        Me.btnReportViewer.Location = New System.Drawing.Point(246, 0)
        Me.btnReportViewer.Margin = New System.Windows.Forms.Padding(8, 0, 0, 0)
        Me.btnReportViewer.Name = "btnReportViewer"
        Me.btnReportViewer.Size = New System.Drawing.Size(101, 40)
        Me.btnReportViewer.TabIndex = 5
        Me.btnReportViewer.Text = "Preview"
        Me.btnReportViewer.UseVisualStyleBackColor = False
        '
        'btnCancel
        '
        Me.btnCancel.BackColor = System.Drawing.Color.White
        Me.btnCancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnCancel.Enabled = False
        Me.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCancel.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(37, Byte), Integer), CType(CType(41, Byte), Integer))
        Me.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.btnCancel.Location = New System.Drawing.Point(157, 0)
        Me.btnCancel.Margin = New System.Windows.Forms.Padding(8, 0, 0, 0)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(81, 40)
        Me.btnCancel.TabIndex = 6
        Me.btnCancel.Text = "Reset"
        Me.btnCancel.UseVisualStyleBackColor = False
        '
        'btnReject
        '
        Me.btnReject.BackColor = System.Drawing.Color.White
        Me.btnReject.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnReject.Enabled = False
        Me.btnReject.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnReject.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnReject.ForeColor = System.Drawing.Color.FromArgb(CType(CType(140, Byte), Integer), CType(CType(38, Byte), Integer), CType(CType(38, Byte), Integer))
        Me.btnReject.Location = New System.Drawing.Point(467, 0)
        Me.btnReject.Margin = New System.Windows.Forms.Padding(8, 0, 0, 0)
        Me.btnReject.Name = "btnReject"
        Me.btnReject.Size = New System.Drawing.Size(155, 40)
        Me.btnReject.TabIndex = 8
        Me.btnReject.Text = "Return for Modification"
        Me.btnReject.UseVisualStyleBackColor = False
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
        Me.BTNRefresh.BackColor = System.Drawing.Color.White
        Me.BTNRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.BTNRefresh.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.BTNRefresh.ForeColor = System.Drawing.Color.FromArgb(CType(CType(33, Byte), Integer), CType(CType(37, Byte), Integer), CType(CType(41, Byte), Integer))
        Me.BTNRefresh.Location = New System.Drawing.Point(0, 0)
        Me.BTNRefresh.Margin = New System.Windows.Forms.Padding(0)
        Me.BTNRefresh.Name = "BTNRefresh"
        Me.BTNRefresh.Size = New System.Drawing.Size(149, 40)
        Me.BTNRefresh.TabIndex = 9
        Me.BTNRefresh.Text = "Refresh Users"
        Me.BTNRefresh.UseVisualStyleBackColor = False
        '
        'mainLayout
        '
        Me.mainLayout.ColumnCount = 1
        Me.mainLayout.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.mainLayout.Controls.Add(Me.headerPanel, 0, 0)
        Me.mainLayout.Controls.Add(Me.contentLayout, 0, 1)
        Me.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill
        Me.mainLayout.Location = New System.Drawing.Point(0, 0)
        Me.mainLayout.Name = "mainLayout"
        Me.mainLayout.Padding = New System.Windows.Forms.Padding(16)
        Me.mainLayout.RowCount = 2
        Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 74.0!))
        Me.mainLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.mainLayout.Size = New System.Drawing.Size(1000, 640)
        Me.mainLayout.TabIndex = 10
        '
        'headerPanel
        '
        Me.headerPanel.Controls.Add(Me.actionPanel)
        Me.headerPanel.Controls.Add(Me.lblSubtitle)
        Me.headerPanel.Controls.Add(Me.lblTitle)
        Me.headerPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.headerPanel.Location = New System.Drawing.Point(16, 16)
        Me.headerPanel.Margin = New System.Windows.Forms.Padding(0, 0, 0, 12)
        Me.headerPanel.Name = "headerPanel"
        Me.headerPanel.Size = New System.Drawing.Size(968, 62)
        Me.headerPanel.TabIndex = 0
        '
        'lblTitle
        '
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI Semibold", 15.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblTitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(31, Byte), Integer), CType(CType(40, Byte), Integer))
        Me.lblTitle.Location = New System.Drawing.Point(0, 0)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(188, 30)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "Approval Review"
        '
        'lblSubtitle
        '
        Me.lblSubtitle.AutoSize = True
        Me.lblSubtitle.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(86, Byte), Integer), CType(CType(94, Byte), Integer), CType(CType(104, Byte), Integer))
        Me.lblSubtitle.Location = New System.Drawing.Point(3, 34)
        Me.lblSubtitle.Name = "lblSubtitle"
        Me.lblSubtitle.Size = New System.Drawing.Size(248, 15)
        Me.lblSubtitle.TabIndex = 1
        Me.lblSubtitle.Text = "Review filed expense reports before approval"
        '
        'actionPanel
        '
        Me.actionPanel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.actionPanel.Controls.Add(Me.BTNRefresh)
        Me.actionPanel.Controls.Add(Me.btnCancel)
        Me.actionPanel.Controls.Add(Me.btnReportViewer)
        Me.actionPanel.Controls.Add(Me.btnApprove)
        Me.actionPanel.Controls.Add(Me.btnReject)
        Me.actionPanel.Location = New System.Drawing.Point(346, 8)
        Me.actionPanel.Margin = New System.Windows.Forms.Padding(0)
        Me.actionPanel.Name = "actionPanel"
        Me.actionPanel.Size = New System.Drawing.Size(622, 40)
        Me.actionPanel.TabIndex = 2
        '
        'contentLayout
        '
        Me.contentLayout.ColumnCount = 2
        Me.contentLayout.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 312.0!))
        Me.contentLayout.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.contentLayout.Controls.Add(Me.userPanel, 0, 0)
        Me.contentLayout.Controls.Add(Me.reportPanel, 1, 0)
        Me.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill
        Me.contentLayout.Location = New System.Drawing.Point(16, 90)
        Me.contentLayout.Margin = New System.Windows.Forms.Padding(0)
        Me.contentLayout.Name = "contentLayout"
        Me.contentLayout.RowCount = 1
        Me.contentLayout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.contentLayout.Size = New System.Drawing.Size(968, 534)
        Me.contentLayout.TabIndex = 1
        '
        'userPanel
        '
        Me.userPanel.BackColor = System.Drawing.Color.White
        Me.userPanel.Controls.Add(Me.dgvUser)
        Me.userPanel.Controls.Add(Me.Label1)
        Me.userPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.userPanel.Location = New System.Drawing.Point(0, 0)
        Me.userPanel.Margin = New System.Windows.Forms.Padding(0, 0, 12, 0)
        Me.userPanel.Name = "userPanel"
        Me.userPanel.Padding = New System.Windows.Forms.Padding(0, 0, 0, 5)
        Me.userPanel.Size = New System.Drawing.Size(300, 534)
        Me.userPanel.TabIndex = 0
        '
        'reportPanel
        '
        Me.reportPanel.BackColor = System.Drawing.Color.White
        Me.reportPanel.Controls.Add(Me.dgvUserReportDetails)
        Me.reportPanel.Controls.Add(Me.Label2)
        Me.reportPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.reportPanel.Location = New System.Drawing.Point(312, 0)
        Me.reportPanel.Margin = New System.Windows.Forms.Padding(0)
        Me.reportPanel.Name = "reportPanel"
        Me.reportPanel.Padding = New System.Windows.Forms.Padding(0, 0, 0, 5)
        Me.reportPanel.Size = New System.Drawing.Size(656, 534)
        Me.reportPanel.TabIndex = 1
        '
        'frmApprove
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(236, Byte), Integer), CType(CType(240, Byte), Integer), CType(CType(244, Byte), Integer))
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.ClientSize = New System.Drawing.Size(1000, 640)
        Me.Controls.Add(Me.mainLayout)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable
        Me.KeyPreview = True
        Me.MaximizeBox = True
        Me.MinimizeBox = False
        Me.MinimumSize = New System.Drawing.Size(900, 560)
        Me.Name = "frmApprove"
        Me.Padding = New System.Windows.Forms.Padding(0)
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        CType(Me.dgvUser, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvUserReportDetails, System.ComponentModel.ISupportInitialize).EndInit()
        Me.CMSEditUserExpense.ResumeLayout(False)
        Me.mainLayout.ResumeLayout(False)
        Me.headerPanel.ResumeLayout(False)
        Me.headerPanel.PerformLayout()
        Me.actionPanel.ResumeLayout(False)
        Me.contentLayout.ResumeLayout(False)
        Me.userPanel.ResumeLayout(False)
        Me.userPanel.PerformLayout()
        Me.reportPanel.ResumeLayout(False)
        Me.reportPanel.PerformLayout()
        Me.ResumeLayout(False)

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
    Friend WithEvents mainLayout As TableLayoutPanel
    Friend WithEvents headerPanel As Panel
    Friend WithEvents lblTitle As Label
    Friend WithEvents lblSubtitle As Label
    Friend WithEvents actionPanel As FlowLayoutPanel
    Friend WithEvents contentLayout As TableLayoutPanel
    Friend WithEvents userPanel As Panel
    Friend WithEvents reportPanel As Panel
End Class
