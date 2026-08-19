<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmFwmsTransactionLookup
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.dtpDateFrom = New System.Windows.Forms.DateTimePicker()
        Me.dtpDateTo = New System.Windows.Forms.DateTimePicker()
        Me.lblDateFrom = New System.Windows.Forms.Label()
        Me.lblDateTo = New System.Windows.Forms.Label()
        Me.btnSearch = New System.Windows.Forms.Button()
        Me.dgvTransactions = New System.Windows.Forms.DataGridView()
        Me.btnInsert = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.filterPanel = New System.Windows.Forms.Panel()
        Me.buttonPanel = New System.Windows.Forms.Panel()
        CType(Me.dgvTransactions, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.filterPanel.SuspendLayout()
        Me.buttonPanel.SuspendLayout()
        Me.SuspendLayout()
        '
        'dtpDateFrom
        '
        Me.dtpDateFrom.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.dtpDateFrom.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpDateFrom.Location = New System.Drawing.Point(78, 11)
        Me.dtpDateFrom.Name = "dtpDateFrom"
        Me.dtpDateFrom.Size = New System.Drawing.Size(110, 22)
        Me.dtpDateFrom.TabIndex = 1
        '
        'dtpDateTo
        '
        Me.dtpDateTo.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.dtpDateTo.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpDateTo.Location = New System.Drawing.Point(250, 11)
        Me.dtpDateTo.Name = "dtpDateTo"
        Me.dtpDateTo.Size = New System.Drawing.Size(110, 22)
        Me.dtpDateTo.TabIndex = 3
        '
        'lblDateFrom
        '
        Me.lblDateFrom.AutoSize = True
        Me.lblDateFrom.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.lblDateFrom.Location = New System.Drawing.Point(12, 15)
        Me.lblDateFrom.Name = "lblDateFrom"
        Me.lblDateFrom.Size = New System.Drawing.Size(60, 13)
        Me.lblDateFrom.TabIndex = 0
        Me.lblDateFrom.Text = "Date From"
        '
        'lblDateTo
        '
        Me.lblDateTo.AutoSize = True
        Me.lblDateTo.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.lblDateTo.Location = New System.Drawing.Point(201, 15)
        Me.lblDateTo.Name = "lblDateTo"
        Me.lblDateTo.Size = New System.Drawing.Size(43, 13)
        Me.lblDateTo.TabIndex = 2
        Me.lblDateTo.Text = "Date To"
        '
        'btnSearch
        '
        Me.btnSearch.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.btnSearch.Location = New System.Drawing.Point(376, 10)
        Me.btnSearch.Name = "btnSearch"
        Me.btnSearch.Size = New System.Drawing.Size(75, 24)
        Me.btnSearch.TabIndex = 4
        Me.btnSearch.Text = "Search"
        Me.btnSearch.UseVisualStyleBackColor = True
        '
        'dgvTransactions
        '
        Me.dgvTransactions.AllowUserToAddRows = False
        Me.dgvTransactions.AllowUserToDeleteRows = False
        Me.dgvTransactions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvTransactions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvTransactions.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgvTransactions.Location = New System.Drawing.Point(0, 46)
        Me.dgvTransactions.MultiSelect = False
        Me.dgvTransactions.Name = "dgvTransactions"
        Me.dgvTransactions.ReadOnly = True
        Me.dgvTransactions.RowHeadersVisible = False
        Me.dgvTransactions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvTransactions.Size = New System.Drawing.Size(684, 336)
        Me.dgvTransactions.TabIndex = 1
        '
        'btnInsert
        '
        Me.btnInsert.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.btnInsert.Location = New System.Drawing.Point(516, 8)
        Me.btnInsert.Name = "btnInsert"
        Me.btnInsert.Size = New System.Drawing.Size(75, 26)
        Me.btnInsert.TabIndex = 0
        Me.btnInsert.Text = "Insert"
        Me.btnInsert.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.btnCancel.Location = New System.Drawing.Point(597, 8)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(75, 26)
        Me.btnCancel.TabIndex = 1
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'filterPanel
        '
        Me.filterPanel.Controls.Add(Me.lblDateFrom)
        Me.filterPanel.Controls.Add(Me.dtpDateFrom)
        Me.filterPanel.Controls.Add(Me.lblDateTo)
        Me.filterPanel.Controls.Add(Me.dtpDateTo)
        Me.filterPanel.Controls.Add(Me.btnSearch)
        Me.filterPanel.Dock = System.Windows.Forms.DockStyle.Top
        Me.filterPanel.Location = New System.Drawing.Point(0, 0)
        Me.filterPanel.Name = "filterPanel"
        Me.filterPanel.Size = New System.Drawing.Size(684, 46)
        Me.filterPanel.TabIndex = 0
        '
        'buttonPanel
        '
        Me.buttonPanel.Controls.Add(Me.btnInsert)
        Me.buttonPanel.Controls.Add(Me.btnCancel)
        Me.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.buttonPanel.Location = New System.Drawing.Point(0, 382)
        Me.buttonPanel.Name = "buttonPanel"
        Me.buttonPanel.Size = New System.Drawing.Size(684, 42)
        Me.buttonPanel.TabIndex = 2
        '
        'frmFwmsTransactionLookup
        '
        Me.AcceptButton = Me.btnSearch
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.btnCancel
        Me.ClientSize = New System.Drawing.Size(684, 424)
        Me.Controls.Add(Me.dgvTransactions)
        Me.Controls.Add(Me.buttonPanel)
        Me.Controls.Add(Me.filterPanel)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmFwmsTransactionLookup"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "FWMS Transaction Lookup"
        CType(Me.dgvTransactions, System.ComponentModel.ISupportInitialize).EndInit()
        Me.filterPanel.ResumeLayout(False)
        Me.filterPanel.PerformLayout()
        Me.buttonPanel.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents dtpDateFrom As DateTimePicker
    Friend WithEvents dtpDateTo As DateTimePicker
    Friend WithEvents lblDateFrom As Label
    Friend WithEvents lblDateTo As Label
    Friend WithEvents btnSearch As Button
    Friend WithEvents dgvTransactions As DataGridView
    Friend WithEvents btnInsert As Button
    Friend WithEvents btnCancel As Button
    Friend WithEvents filterPanel As Panel
    Friend WithEvents buttonPanel As Panel
End Class
