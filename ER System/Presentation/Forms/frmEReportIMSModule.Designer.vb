<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmEReportIMSModule
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
        Me.txtTotalNumberOfDays = New System.Windows.Forms.TextBox()
        Me.txtComputation = New System.Windows.Forms.TextBox()
        Me.Label21 = New System.Windows.Forms.Label()
        Me.txtType = New System.Windows.Forms.ComboBox()
        Me.txtMDays = New System.Windows.Forms.TextBox()
        Me.Label20 = New System.Windows.Forms.Label()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.Label19 = New System.Windows.Forms.Label()
        Me.txtRemarks = New System.Windows.Forms.RichTextBox()
        Me.txtStatus = New System.Windows.Forms.ComboBox()
        Me.Label18 = New System.Windows.Forms.Label()
        Me.txtInvoice = New System.Windows.Forms.TextBox()
        Me.Label17 = New System.Windows.Forms.Label()
        Me.txtMultiplier = New System.Windows.Forms.TextBox()
        Me.lblMultiplier = New System.Windows.Forms.Label()
        Me.txtTotal = New System.Windows.Forms.TextBox()
        Me.txtExpenseAmount = New System.Windows.Forms.TextBox()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.txtCategory = New System.Windows.Forms.ComboBox()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.txtParticulars = New System.Windows.Forms.TextBox()
        Me.txtLocation = New System.Windows.Forms.TextBox()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.dtpExpenseDate = New System.Windows.Forms.DateTimePicker()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.CBPerdiem = New System.Windows.Forms.CheckBox()
        Me.btnExpenseSave = New System.Windows.Forms.Button()
        Me.btnExpenseUpdate = New System.Windows.Forms.Button()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'txtTotalNumberOfDays
        '
        Me.txtTotalNumberOfDays.Enabled = False
        Me.txtTotalNumberOfDays.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtTotalNumberOfDays.Location = New System.Drawing.Point(283, 111)
        Me.txtTotalNumberOfDays.MaxLength = 2
        Me.txtTotalNumberOfDays.Multiline = True
        Me.txtTotalNumberOfDays.Name = "txtTotalNumberOfDays"
        Me.txtTotalNumberOfDays.Size = New System.Drawing.Size(33, 19)
        Me.txtTotalNumberOfDays.TabIndex = 89
        '
        'txtComputation
        '
        Me.txtComputation.Enabled = False
        Me.txtComputation.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtComputation.Location = New System.Drawing.Point(100, 111)
        Me.txtComputation.Name = "txtComputation"
        Me.txtComputation.Size = New System.Drawing.Size(112, 22)
        Me.txtComputation.TabIndex = 87
        '
        'Label21
        '
        Me.Label21.AutoSize = True
        Me.Label21.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label21.Location = New System.Drawing.Point(13, 114)
        Me.Label21.Name = "Label21"
        Me.Label21.Size = New System.Drawing.Size(75, 13)
        Me.Label21.TabIndex = 86
        Me.Label21.Text = "Computation"
        '
        'txtType
        '
        Me.txtType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.txtType.FormattingEnabled = True
        Me.txtType.Items.AddRange(New Object() {"Local", "Foreign"})
        Me.txtType.Location = New System.Drawing.Point(100, 142)
        Me.txtType.Name = "txtType"
        Me.txtType.Size = New System.Drawing.Size(112, 21)
        Me.txtType.TabIndex = 61
        '
        'txtMDays
        '
        Me.txtMDays.Enabled = False
        Me.txtMDays.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtMDays.Location = New System.Drawing.Point(259, 142)
        Me.txtMDays.MaxLength = 2
        Me.txtMDays.Multiline = True
        Me.txtMDays.Name = "txtMDays"
        Me.txtMDays.Size = New System.Drawing.Size(57, 19)
        Me.txtMDays.TabIndex = 62
        '
        'Label20
        '
        Me.Label20.AutoSize = True
        Me.Label20.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label20.Location = New System.Drawing.Point(218, 145)
        Me.Label20.Name = "Label20"
        Me.Label20.Size = New System.Drawing.Size(38, 13)
        Me.Label20.TabIndex = 85
        Me.Label20.Text = "- Days"
        '
        'btnCancel
        '
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCancel.Location = New System.Drawing.Point(170, 412)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(70, 24)
        Me.btnCancel.TabIndex = 84
        Me.btnCancel.TabStop = False
        Me.btnCancel.Text = "Reset"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'Label19
        '
        Me.Label19.AutoSize = True
        Me.Label19.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label19.Location = New System.Drawing.Point(13, 336)
        Me.Label19.Name = "Label19"
        Me.Label19.Size = New System.Drawing.Size(50, 13)
        Me.Label19.TabIndex = 82
        Me.Label19.Text = "Remarks"
        '
        'txtRemarks
        '
        Me.txtRemarks.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtRemarks.Location = New System.Drawing.Point(100, 331)
        Me.txtRemarks.Name = "txtRemarks"
        Me.txtRemarks.Size = New System.Drawing.Size(216, 77)
        Me.txtRemarks.TabIndex = 70
        Me.txtRemarks.Text = ""
        '
        'txtStatus
        '
        Me.txtStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.txtStatus.FormattingEnabled = True
        Me.txtStatus.ItemHeight = 13
        Me.txtStatus.Items.AddRange(New Object() {"True", "False"})
        Me.txtStatus.Location = New System.Drawing.Point(100, 301)
        Me.txtStatus.Name = "txtStatus"
        Me.txtStatus.Size = New System.Drawing.Size(216, 21)
        Me.txtStatus.TabIndex = 69
        Me.txtStatus.TabStop = False
        '
        'Label18
        '
        Me.Label18.AutoSize = True
        Me.Label18.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label18.Location = New System.Drawing.Point(13, 304)
        Me.Label18.Name = "Label18"
        Me.Label18.Size = New System.Drawing.Size(39, 13)
        Me.Label18.TabIndex = 81
        Me.Label18.Text = "Status"
        '
        'txtInvoice
        '
        Me.txtInvoice.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtInvoice.Location = New System.Drawing.Point(100, 270)
        Me.txtInvoice.Name = "txtInvoice"
        Me.txtInvoice.Size = New System.Drawing.Size(216, 22)
        Me.txtInvoice.TabIndex = 68
        '
        'Label17
        '
        Me.Label17.AutoSize = True
        Me.Label17.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label17.Location = New System.Drawing.Point(13, 273)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(64, 13)
        Me.Label17.TabIndex = 80
        Me.Label17.Text = "Invoice No."
        '
        'txtMultiplier
        '
        Me.txtMultiplier.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtMultiplier.Location = New System.Drawing.Point(230, 204)
        Me.txtMultiplier.Name = "txtMultiplier"
        Me.txtMultiplier.Size = New System.Drawing.Size(86, 22)
        Me.txtMultiplier.TabIndex = 65
        Me.txtMultiplier.Text = "1"
        '
        'lblMultiplier
        '
        Me.lblMultiplier.AutoSize = True
        Me.lblMultiplier.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblMultiplier.Location = New System.Drawing.Point(171, 207)
        Me.lblMultiplier.Name = "lblMultiplier"
        Me.lblMultiplier.Size = New System.Drawing.Size(57, 13)
        Me.lblMultiplier.TabIndex = 79
        Me.lblMultiplier.Text = "Multiplier"
        '
        'txtTotal
        '
        Me.txtTotal.Enabled = False
        Me.txtTotal.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtTotal.Location = New System.Drawing.Point(100, 237)
        Me.txtTotal.Name = "txtTotal"
        Me.txtTotal.Size = New System.Drawing.Size(216, 22)
        Me.txtTotal.TabIndex = 67
        '
        'txtExpenseAmount
        '
        Me.txtExpenseAmount.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtExpenseAmount.Location = New System.Drawing.Point(100, 204)
        Me.txtExpenseAmount.Name = "txtExpenseAmount"
        Me.txtExpenseAmount.Size = New System.Drawing.Size(70, 22)
        Me.txtExpenseAmount.TabIndex = 64
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label14.Location = New System.Drawing.Point(13, 240)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(32, 13)
        Me.Label14.TabIndex = 78
        Me.Label14.Text = "Total"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label15.Location = New System.Drawing.Point(13, 207)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(48, 13)
        Me.Label15.TabIndex = 77
        Me.Label15.Text = "Amount"
        '
        'txtCategory
        '
        Me.txtCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.txtCategory.FormattingEnabled = True
        Me.txtCategory.Items.AddRange(New Object() {"Transportation", "Meals", "Others"})
        Me.txtCategory.Location = New System.Drawing.Point(100, 172)
        Me.txtCategory.Name = "txtCategory"
        Me.txtCategory.Size = New System.Drawing.Size(216, 21)
        Me.txtCategory.TabIndex = 63
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label12.Location = New System.Drawing.Point(13, 175)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(53, 13)
        Me.Label12.TabIndex = 76
        Me.Label12.Text = "Category"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label13.Location = New System.Drawing.Point(13, 145)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(30, 13)
        Me.Label13.TabIndex = 75
        Me.Label13.Text = "Type"
        '
        'txtParticulars
        '
        Me.txtParticulars.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtParticulars.Location = New System.Drawing.Point(100, 79)
        Me.txtParticulars.Name = "txtParticulars"
        Me.txtParticulars.Size = New System.Drawing.Size(216, 22)
        Me.txtParticulars.TabIndex = 60
        '
        'txtLocation
        '
        Me.txtLocation.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtLocation.Location = New System.Drawing.Point(100, 48)
        Me.txtLocation.Name = "txtLocation"
        Me.txtLocation.Size = New System.Drawing.Size(216, 22)
        Me.txtLocation.TabIndex = 59
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label11.Location = New System.Drawing.Point(13, 82)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(60, 13)
        Me.Label11.TabIndex = 74
        Me.Label11.Text = "Particulars"
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label10.Location = New System.Drawing.Point(13, 51)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(51, 13)
        Me.Label10.TabIndex = 73
        Me.Label10.Text = "Location"
        '
        'dtpExpenseDate
        '
        Me.dtpExpenseDate.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.dtpExpenseDate.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpExpenseDate.Location = New System.Drawing.Point(167, 12)
        Me.dtpExpenseDate.Name = "dtpExpenseDate"
        Me.dtpExpenseDate.Size = New System.Drawing.Size(149, 22)
        Me.dtpExpenseDate.TabIndex = 58
        Me.dtpExpenseDate.TabStop = False
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(131, 15)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(30, 13)
        Me.Label9.TabIndex = 72
        Me.Label9.Text = "Date"
        '
        'CBPerdiem
        '
        Me.CBPerdiem.AutoSize = True
        Me.CBPerdiem.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CBPerdiem.Location = New System.Drawing.Point(16, 14)
        Me.CBPerdiem.Name = "CBPerdiem"
        Me.CBPerdiem.Size = New System.Drawing.Size(79, 17)
        Me.CBPerdiem.TabIndex = 71
        Me.CBPerdiem.TabStop = False
        Me.CBPerdiem.Text = "Allowance"
        Me.CBPerdiem.UseVisualStyleBackColor = True
        '
        'btnExpenseSave
        '
        Me.btnExpenseSave.Location = New System.Drawing.Point(246, 412)
        Me.btnExpenseSave.Name = "btnExpenseSave"
        Me.btnExpenseSave.Size = New System.Drawing.Size(70, 24)
        Me.btnExpenseSave.TabIndex = 83
        Me.btnExpenseSave.Text = "Save"
        Me.btnExpenseSave.UseVisualStyleBackColor = True
        '
        'btnExpenseUpdate
        '
        Me.btnExpenseUpdate.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnExpenseUpdate.Location = New System.Drawing.Point(246, 412)
        Me.btnExpenseUpdate.Name = "btnExpenseUpdate"
        Me.btnExpenseUpdate.Size = New System.Drawing.Size(70, 24)
        Me.btnExpenseUpdate.TabIndex = 66
        Me.btnExpenseUpdate.Text = "Update"
        Me.btnExpenseUpdate.UseVisualStyleBackColor = True
        Me.btnExpenseUpdate.Visible = False
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label16.Location = New System.Drawing.Point(211, 114)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(73, 13)
        Me.Label16.TabIndex = 88
        Me.Label16.Text = "# of Days(22)"
        '
        'frmEReportIMSModule
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1042, 626)
        Me.Controls.Add(Me.txtTotalNumberOfDays)
        Me.Controls.Add(Me.txtComputation)
        Me.Controls.Add(Me.Label21)
        Me.Controls.Add(Me.txtType)
        Me.Controls.Add(Me.txtMDays)
        Me.Controls.Add(Me.Label20)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.Label19)
        Me.Controls.Add(Me.txtRemarks)
        Me.Controls.Add(Me.txtStatus)
        Me.Controls.Add(Me.Label18)
        Me.Controls.Add(Me.txtInvoice)
        Me.Controls.Add(Me.Label17)
        Me.Controls.Add(Me.txtMultiplier)
        Me.Controls.Add(Me.lblMultiplier)
        Me.Controls.Add(Me.txtTotal)
        Me.Controls.Add(Me.txtExpenseAmount)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.Label15)
        Me.Controls.Add(Me.txtCategory)
        Me.Controls.Add(Me.Label12)
        Me.Controls.Add(Me.Label13)
        Me.Controls.Add(Me.txtParticulars)
        Me.Controls.Add(Me.txtLocation)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.Label10)
        Me.Controls.Add(Me.dtpExpenseDate)
        Me.Controls.Add(Me.Label9)
        Me.Controls.Add(Me.CBPerdiem)
        Me.Controls.Add(Me.btnExpenseSave)
        Me.Controls.Add(Me.btnExpenseUpdate)
        Me.Controls.Add(Me.Label16)
        Me.Name = "frmEReportIMSModule"
        Me.Text = "Expense Details"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents txtTotalNumberOfDays As TextBox
    Friend WithEvents txtComputation As TextBox
    Friend WithEvents Label21 As Label
    Friend WithEvents txtType As ComboBox
    Friend WithEvents txtMDays As TextBox
    Friend WithEvents Label20 As Label
    Friend WithEvents btnCancel As Button
    Friend WithEvents Label19 As Label
    Friend WithEvents txtRemarks As RichTextBox
    Friend WithEvents txtStatus As ComboBox
    Friend WithEvents Label18 As Label
    Friend WithEvents txtInvoice As TextBox
    Friend WithEvents Label17 As Label
    Friend WithEvents txtMultiplier As TextBox
    Friend WithEvents lblMultiplier As Label
    Friend WithEvents txtTotal As TextBox
    Friend WithEvents txtExpenseAmount As TextBox
    Friend WithEvents Label14 As Label
    Friend WithEvents Label15 As Label
    Friend WithEvents txtCategory As ComboBox
    Friend WithEvents Label12 As Label
    Friend WithEvents Label13 As Label
    Friend WithEvents txtParticulars As TextBox
    Friend WithEvents txtLocation As TextBox
    Friend WithEvents Label11 As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents dtpExpenseDate As DateTimePicker
    Friend WithEvents Label9 As Label
    Friend WithEvents CBPerdiem As CheckBox
    Friend WithEvents btnExpenseSave As Button
    Friend WithEvents btnExpenseUpdate As Button
    Friend WithEvents Label16 As Label
End Class
