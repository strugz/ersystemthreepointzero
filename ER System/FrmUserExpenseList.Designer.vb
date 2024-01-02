<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FrmUserExpenseList
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FrmUserExpenseList))
        Me.Label25 = New System.Windows.Forms.Label()
        Me.txtFrom = New System.Windows.Forms.TextBox()
        Me.BTNEditCategory = New System.Windows.Forms.Button()
        Me.txtParticulars = New System.Windows.Forms.RichTextBox()
        Me.txtType = New System.Windows.Forms.ComboBox()
        Me.RBForeign = New System.Windows.Forms.RadioButton()
        Me.RbLocal = New System.Windows.Forms.RadioButton()
        Me.btnWorkWith = New System.Windows.Forms.Button()
        Me.txtWorkWith = New System.Windows.Forms.TextBox()
        Me.Label23 = New System.Windows.Forms.Label()
        Me.txtTo = New System.Windows.Forms.TextBox()
        Me.BTNTransportation = New System.Windows.Forms.Button()
        Me.txtLocation = New System.Windows.Forms.TextBox()
        Me.btnHospital = New System.Windows.Forms.Button()
        Me.btnServiceNo = New System.Windows.Forms.Button()
        Me.btnSerialNumber = New System.Windows.Forms.Button()
        Me.Label24 = New System.Windows.Forms.Label()
        Me.btnInstrumentHistory = New System.Windows.Forms.Button()
        Me.txtSerialNumber = New System.Windows.Forms.TextBox()
        Me.Label22 = New System.Windows.Forms.Label()
        Me.txtInstrument = New System.Windows.Forms.TextBox()
        Me.Label21 = New System.Windows.Forms.Label()
        Me.txtServiceNumber = New System.Windows.Forms.TextBox()
        Me.Label20 = New System.Windows.Forms.Label()
        Me.CBBFare = New System.Windows.Forms.ComboBox()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.BTNAddFare = New System.Windows.Forms.Button()
        Me.lblExpenseRemarks = New System.Windows.Forms.Label()
        Me.txtRemarks = New System.Windows.Forms.RichTextBox()
        Me.txtStatus = New System.Windows.Forms.ComboBox()
        Me.lblExpenseStatus = New System.Windows.Forms.Label()
        Me.txtInvoice = New System.Windows.Forms.TextBox()
        Me.lblExpenseInvoice = New System.Windows.Forms.Label()
        Me.txtMultiplier = New System.Windows.Forms.TextBox()
        Me.lblExpenseMultiplier = New System.Windows.Forms.Label()
        Me.txtTotal = New System.Windows.Forms.TextBox()
        Me.Label26 = New System.Windows.Forms.Label()
        Me.dgvExpense = New System.Windows.Forms.DataGridView()
        Me.txtExpenseAmount = New System.Windows.Forms.TextBox()
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TPExpenseReport = New System.Windows.Forms.TabPage()
        Me.lblTotalExpenseAmount = New System.Windows.Forms.Label()
        Me.lblExpenseAmount = New System.Windows.Forms.Label()
        Me.txtCategory = New System.Windows.Forms.ComboBox()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.dtpExpenseDate = New System.Windows.Forms.DateTimePicker()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.CBPerdiem = New System.Windows.Forms.CheckBox()
        Me.btnExpenseUpdate = New System.Windows.Forms.Button()
        Me.btnExpenseSave = New System.Windows.Forms.Button()
        Me.BTNFareClose = New System.Windows.Forms.Button()
        Me.GBTransportation = New System.Windows.Forms.GroupBox()
        Me.BTNDown = New System.Windows.Forms.Button()
        Me.BTNUp = New System.Windows.Forms.Button()
        Me.GBMeals = New System.Windows.Forms.GroupBox()
        Me.CBDinnerOTMeal = New System.Windows.Forms.CheckBox()
        Me.RBOTMeal = New System.Windows.Forms.RadioButton()
        Me.RBDinner = New System.Windows.Forms.RadioButton()
        Me.BTNMealClose = New System.Windows.Forms.Button()
        Me.CLBPaidBill = New System.Windows.Forms.CheckedListBox()
        Me.CBBPaidFor = New System.Windows.Forms.CheckBox()
        Me.CLBMeals = New System.Windows.Forms.CheckedListBox()
        Me.BTNMeals = New System.Windows.Forms.Button()
        CType(Me.dgvExpense, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabControl1.SuspendLayout()
        Me.TPExpenseReport.SuspendLayout()
        Me.GBTransportation.SuspendLayout()
        Me.GBMeals.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label25
        '
        Me.Label25.AutoSize = True
        Me.Label25.Location = New System.Drawing.Point(6, 93)
        Me.Label25.Name = "Label25"
        Me.Label25.Size = New System.Drawing.Size(20, 13)
        Me.Label25.TabIndex = 83
        Me.Label25.Text = "To"
        '
        'txtFrom
        '
        Me.txtFrom.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtFrom.Location = New System.Drawing.Point(9, 68)
        Me.txtFrom.Name = "txtFrom"
        Me.txtFrom.Size = New System.Drawing.Size(185, 20)
        Me.txtFrom.TabIndex = 80
        '
        'BTNEditCategory
        '
        Me.BTNEditCategory.Enabled = False
        Me.BTNEditCategory.Location = New System.Drawing.Point(300, 186)
        Me.BTNEditCategory.Name = "BTNEditCategory"
        Me.BTNEditCategory.Size = New System.Drawing.Size(24, 22)
        Me.BTNEditCategory.TabIndex = 79
        Me.BTNEditCategory.Text = "..."
        Me.BTNEditCategory.UseVisualStyleBackColor = True
        '
        'txtParticulars
        '
        Me.txtParticulars.BackColor = System.Drawing.Color.White
        Me.txtParticulars.Location = New System.Drawing.Point(99, 213)
        Me.txtParticulars.Name = "txtParticulars"
        Me.txtParticulars.ReadOnly = True
        Me.txtParticulars.Size = New System.Drawing.Size(199, 66)
        Me.txtParticulars.TabIndex = 5
        Me.txtParticulars.TabStop = False
        Me.txtParticulars.Text = ""
        '
        'txtType
        '
        Me.txtType.BackColor = System.Drawing.SystemColors.InactiveCaption
        Me.txtType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.txtType.FormattingEnabled = True
        Me.txtType.Items.AddRange(New Object() {"Local", "Foreign"})
        Me.txtType.Location = New System.Drawing.Point(394, 212)
        Me.txtType.Name = "txtType"
        Me.txtType.Size = New System.Drawing.Size(199, 21)
        Me.txtType.TabIndex = 6
        '
        'RBForeign
        '
        Me.RBForeign.AutoSize = True
        Me.RBForeign.Location = New System.Drawing.Point(228, 163)
        Me.RBForeign.Name = "RBForeign"
        Me.RBForeign.Size = New System.Drawing.Size(65, 17)
        Me.RBForeign.TabIndex = 7
        Me.RBForeign.TabStop = True
        Me.RBForeign.Text = "Foreign"
        Me.RBForeign.UseVisualStyleBackColor = True
        '
        'RbLocal
        '
        Me.RbLocal.AutoSize = True
        Me.RbLocal.Location = New System.Drawing.Point(98, 163)
        Me.RbLocal.Name = "RbLocal"
        Me.RbLocal.Size = New System.Drawing.Size(51, 17)
        Me.RbLocal.TabIndex = 6
        Me.RbLocal.TabStop = True
        Me.RbLocal.Text = "Local"
        Me.RbLocal.UseVisualStyleBackColor = True
        '
        'btnWorkWith
        '
        Me.btnWorkWith.Location = New System.Drawing.Point(301, 24)
        Me.btnWorkWith.Name = "btnWorkWith"
        Me.btnWorkWith.Size = New System.Drawing.Size(24, 22)
        Me.btnWorkWith.TabIndex = 100
        Me.btnWorkWith.Text = "..."
        Me.btnWorkWith.UseVisualStyleBackColor = True
        '
        'txtWorkWith
        '
        Me.txtWorkWith.Enabled = False
        Me.txtWorkWith.Location = New System.Drawing.Point(210, 24)
        Me.txtWorkWith.Name = "txtWorkWith"
        Me.txtWorkWith.Size = New System.Drawing.Size(87, 22)
        Me.txtWorkWith.TabIndex = 68
        '
        'Label23
        '
        Me.Label23.AutoSize = True
        Me.Label23.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label23.Location = New System.Drawing.Point(207, 8)
        Me.Label23.Name = "Label23"
        Me.Label23.Size = New System.Drawing.Size(60, 13)
        Me.Label23.TabIndex = 67
        Me.Label23.Text = "WorkWith"
        '
        'txtTo
        '
        Me.txtTo.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtTo.Location = New System.Drawing.Point(9, 109)
        Me.txtTo.Name = "txtTo"
        Me.txtTo.Size = New System.Drawing.Size(185, 20)
        Me.txtTo.TabIndex = 81
        '
        'BTNTransportation
        '
        Me.BTNTransportation.Location = New System.Drawing.Point(107, 146)
        Me.BTNTransportation.Name = "BTNTransportation"
        Me.BTNTransportation.Size = New System.Drawing.Size(87, 22)
        Me.BTNTransportation.TabIndex = 82
        Me.BTNTransportation.Text = "Done"
        Me.BTNTransportation.UseVisualStyleBackColor = True
        '
        'txtLocation
        '
        Me.txtLocation.Enabled = False
        Me.txtLocation.Location = New System.Drawing.Point(98, 52)
        Me.txtLocation.Name = "txtLocation"
        Me.txtLocation.Size = New System.Drawing.Size(199, 22)
        Me.txtLocation.TabIndex = 66
        '
        'btnHospital
        '
        Me.btnHospital.Location = New System.Drawing.Point(301, 52)
        Me.btnHospital.Name = "btnHospital"
        Me.btnHospital.Size = New System.Drawing.Size(24, 22)
        Me.btnHospital.TabIndex = 101
        Me.btnHospital.Text = "..."
        Me.btnHospital.UseVisualStyleBackColor = True
        '
        'btnServiceNo
        '
        Me.btnServiceNo.Location = New System.Drawing.Point(301, 135)
        Me.btnServiceNo.Name = "btnServiceNo"
        Me.btnServiceNo.Size = New System.Drawing.Size(24, 22)
        Me.btnServiceNo.TabIndex = 104
        Me.btnServiceNo.Text = "..."
        Me.btnServiceNo.UseVisualStyleBackColor = True
        '
        'btnSerialNumber
        '
        Me.btnSerialNumber.Location = New System.Drawing.Point(301, 107)
        Me.btnSerialNumber.Name = "btnSerialNumber"
        Me.btnSerialNumber.Size = New System.Drawing.Size(24, 22)
        Me.btnSerialNumber.TabIndex = 103
        Me.btnSerialNumber.Text = "..."
        Me.btnSerialNumber.UseVisualStyleBackColor = True
        '
        'Label24
        '
        Me.Label24.AutoSize = True
        Me.Label24.Location = New System.Drawing.Point(6, 52)
        Me.Label24.Name = "Label24"
        Me.Label24.Size = New System.Drawing.Size(30, 13)
        Me.Label24.TabIndex = 82
        Me.Label24.Text = "From"
        '
        'btnInstrumentHistory
        '
        Me.btnInstrumentHistory.Location = New System.Drawing.Point(301, 79)
        Me.btnInstrumentHistory.Name = "btnInstrumentHistory"
        Me.btnInstrumentHistory.Size = New System.Drawing.Size(24, 22)
        Me.btnInstrumentHistory.TabIndex = 102
        Me.btnInstrumentHistory.Text = "..."
        Me.btnInstrumentHistory.UseVisualStyleBackColor = True
        '
        'txtSerialNumber
        '
        Me.txtSerialNumber.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtSerialNumber.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtSerialNumber.Location = New System.Drawing.Point(98, 107)
        Me.txtSerialNumber.Name = "txtSerialNumber"
        Me.txtSerialNumber.Size = New System.Drawing.Size(199, 22)
        Me.txtSerialNumber.TabIndex = 3
        '
        'Label22
        '
        Me.Label22.AutoSize = True
        Me.Label22.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label22.Location = New System.Drawing.Point(11, 110)
        Me.Label22.Name = "Label22"
        Me.Label22.Size = New System.Drawing.Size(79, 13)
        Me.Label22.TabIndex = 57
        Me.Label22.Text = "Serial Number"
        '
        'txtInstrument
        '
        Me.txtInstrument.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtInstrument.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtInstrument.Location = New System.Drawing.Point(98, 79)
        Me.txtInstrument.Name = "txtInstrument"
        Me.txtInstrument.Size = New System.Drawing.Size(199, 22)
        Me.txtInstrument.TabIndex = 2
        '
        'Label21
        '
        Me.Label21.AutoSize = True
        Me.Label21.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label21.Location = New System.Drawing.Point(11, 82)
        Me.Label21.Name = "Label21"
        Me.Label21.Size = New System.Drawing.Size(63, 13)
        Me.Label21.TabIndex = 55
        Me.Label21.Text = "Instrument"
        '
        'txtServiceNumber
        '
        Me.txtServiceNumber.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.txtServiceNumber.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtServiceNumber.Location = New System.Drawing.Point(98, 135)
        Me.txtServiceNumber.Name = "txtServiceNumber"
        Me.txtServiceNumber.Size = New System.Drawing.Size(199, 22)
        Me.txtServiceNumber.TabIndex = 4
        '
        'Label20
        '
        Me.Label20.AutoSize = True
        Me.Label20.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label20.Location = New System.Drawing.Point(11, 138)
        Me.Label20.Name = "Label20"
        Me.Label20.Size = New System.Drawing.Size(63, 13)
        Me.Label20.TabIndex = 53
        Me.Label20.Text = "Service No."
        '
        'CBBFare
        '
        Me.CBBFare.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.CBBFare.FormattingEnabled = True
        Me.CBBFare.Location = New System.Drawing.Point(9, 28)
        Me.CBBFare.Name = "CBBFare"
        Me.CBBFare.Size = New System.Drawing.Size(139, 21)
        Me.CBBFare.TabIndex = 84
        '
        'btnCancel
        '
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCancel.Location = New System.Drawing.Point(151, 465)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(70, 24)
        Me.btnCancel.TabIndex = 51
        Me.btnCancel.TabStop = False
        Me.btnCancel.Text = "Clear"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'BTNAddFare
        '
        Me.BTNAddFare.Location = New System.Drawing.Point(154, 28)
        Me.BTNAddFare.Name = "BTNAddFare"
        Me.BTNAddFare.Size = New System.Drawing.Size(40, 22)
        Me.BTNAddFare.TabIndex = 85
        Me.BTNAddFare.Text = "Save"
        Me.BTNAddFare.UseVisualStyleBackColor = True
        '
        'lblExpenseRemarks
        '
        Me.lblExpenseRemarks.AutoSize = True
        Me.lblExpenseRemarks.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblExpenseRemarks.Location = New System.Drawing.Point(11, 389)
        Me.lblExpenseRemarks.Name = "lblExpenseRemarks"
        Me.lblExpenseRemarks.Size = New System.Drawing.Size(50, 13)
        Me.lblExpenseRemarks.TabIndex = 48
        Me.lblExpenseRemarks.Text = "Remarks"
        '
        'txtRemarks
        '
        Me.txtRemarks.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtRemarks.Location = New System.Drawing.Point(98, 385)
        Me.txtRemarks.Name = "txtRemarks"
        Me.txtRemarks.Size = New System.Drawing.Size(199, 74)
        Me.txtRemarks.TabIndex = 14
        Me.txtRemarks.Text = ""
        '
        'txtStatus
        '
        Me.txtStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.txtStatus.FormattingEnabled = True
        Me.txtStatus.Items.AddRange(New Object() {"True", "False"})
        Me.txtStatus.Location = New System.Drawing.Point(210, 357)
        Me.txtStatus.Name = "txtStatus"
        Me.txtStatus.Size = New System.Drawing.Size(87, 21)
        Me.txtStatus.TabIndex = 13
        Me.txtStatus.TabStop = False
        '
        'lblExpenseStatus
        '
        Me.lblExpenseStatus.AutoSize = True
        Me.lblExpenseStatus.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblExpenseStatus.Location = New System.Drawing.Point(207, 338)
        Me.lblExpenseStatus.Name = "lblExpenseStatus"
        Me.lblExpenseStatus.Size = New System.Drawing.Size(39, 13)
        Me.lblExpenseStatus.TabIndex = 45
        Me.lblExpenseStatus.Text = "Status"
        '
        'txtInvoice
        '
        Me.txtInvoice.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtInvoice.Location = New System.Drawing.Point(98, 357)
        Me.txtInvoice.Name = "txtInvoice"
        Me.txtInvoice.Size = New System.Drawing.Size(106, 22)
        Me.txtInvoice.TabIndex = 12
        '
        'lblExpenseInvoice
        '
        Me.lblExpenseInvoice.AutoSize = True
        Me.lblExpenseInvoice.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblExpenseInvoice.Location = New System.Drawing.Point(96, 338)
        Me.lblExpenseInvoice.Name = "lblExpenseInvoice"
        Me.lblExpenseInvoice.Size = New System.Drawing.Size(64, 13)
        Me.lblExpenseInvoice.TabIndex = 43
        Me.lblExpenseInvoice.Text = "Invoice No."
        '
        'txtMultiplier
        '
        Me.txtMultiplier.Enabled = False
        Me.txtMultiplier.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtMultiplier.Location = New System.Drawing.Point(228, 285)
        Me.txtMultiplier.Name = "txtMultiplier"
        Me.txtMultiplier.Size = New System.Drawing.Size(69, 22)
        Me.txtMultiplier.TabIndex = 10
        Me.txtMultiplier.Text = "1"
        '
        'lblExpenseMultiplier
        '
        Me.lblExpenseMultiplier.AutoSize = True
        Me.lblExpenseMultiplier.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblExpenseMultiplier.Location = New System.Drawing.Point(169, 288)
        Me.lblExpenseMultiplier.Name = "lblExpenseMultiplier"
        Me.lblExpenseMultiplier.Size = New System.Drawing.Size(57, 13)
        Me.lblExpenseMultiplier.TabIndex = 41
        Me.lblExpenseMultiplier.Text = "Multiplier"
        '
        'txtTotal
        '
        Me.txtTotal.Enabled = False
        Me.txtTotal.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtTotal.Location = New System.Drawing.Point(98, 313)
        Me.txtTotal.Name = "txtTotal"
        Me.txtTotal.Size = New System.Drawing.Size(199, 22)
        Me.txtTotal.TabIndex = 11
        '
        'Label26
        '
        Me.Label26.AutoSize = True
        Me.Label26.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label26.Location = New System.Drawing.Point(6, 12)
        Me.Label26.Name = "Label26"
        Me.Label26.Size = New System.Drawing.Size(29, 13)
        Me.Label26.TabIndex = 79
        Me.Label26.Text = "Fare"
        '
        'dgvExpense
        '
        Me.dgvExpense.AllowUserToAddRows = False
        Me.dgvExpense.AllowUserToDeleteRows = False
        Me.dgvExpense.AllowUserToResizeColumns = False
        Me.dgvExpense.AllowUserToResizeRows = False
        Me.dgvExpense.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgvExpense.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvExpense.Location = New System.Drawing.Point(339, 0)
        Me.dgvExpense.Name = "dgvExpense"
        Me.dgvExpense.ReadOnly = True
        Me.dgvExpense.RowHeadersVisible = False
        Me.dgvExpense.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Me.dgvExpense.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvExpense.Size = New System.Drawing.Size(665, 523)
        Me.dgvExpense.TabIndex = 79
        Me.dgvExpense.TabStop = False
        Me.dgvExpense.Visible = False
        '
        'txtExpenseAmount
        '
        Me.txtExpenseAmount.Enabled = False
        Me.txtExpenseAmount.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtExpenseAmount.Location = New System.Drawing.Point(98, 285)
        Me.txtExpenseAmount.Name = "txtExpenseAmount"
        Me.txtExpenseAmount.Size = New System.Drawing.Size(70, 22)
        Me.txtExpenseAmount.TabIndex = 9
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TPExpenseReport)
        Me.TabControl1.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(336, 523)
        Me.TabControl1.TabIndex = 78
        '
        'TPExpenseReport
        '
        Me.TPExpenseReport.BackColor = System.Drawing.SystemColors.Control
        Me.TPExpenseReport.Controls.Add(Me.BTNEditCategory)
        Me.TPExpenseReport.Controls.Add(Me.txtParticulars)
        Me.TPExpenseReport.Controls.Add(Me.txtType)
        Me.TPExpenseReport.Controls.Add(Me.RBForeign)
        Me.TPExpenseReport.Controls.Add(Me.RbLocal)
        Me.TPExpenseReport.Controls.Add(Me.btnWorkWith)
        Me.TPExpenseReport.Controls.Add(Me.txtWorkWith)
        Me.TPExpenseReport.Controls.Add(Me.Label23)
        Me.TPExpenseReport.Controls.Add(Me.txtLocation)
        Me.TPExpenseReport.Controls.Add(Me.btnHospital)
        Me.TPExpenseReport.Controls.Add(Me.btnServiceNo)
        Me.TPExpenseReport.Controls.Add(Me.btnSerialNumber)
        Me.TPExpenseReport.Controls.Add(Me.btnInstrumentHistory)
        Me.TPExpenseReport.Controls.Add(Me.txtSerialNumber)
        Me.TPExpenseReport.Controls.Add(Me.Label22)
        Me.TPExpenseReport.Controls.Add(Me.txtInstrument)
        Me.TPExpenseReport.Controls.Add(Me.Label21)
        Me.TPExpenseReport.Controls.Add(Me.txtServiceNumber)
        Me.TPExpenseReport.Controls.Add(Me.Label20)
        Me.TPExpenseReport.Controls.Add(Me.btnCancel)
        Me.TPExpenseReport.Controls.Add(Me.lblExpenseRemarks)
        Me.TPExpenseReport.Controls.Add(Me.txtRemarks)
        Me.TPExpenseReport.Controls.Add(Me.txtStatus)
        Me.TPExpenseReport.Controls.Add(Me.lblExpenseStatus)
        Me.TPExpenseReport.Controls.Add(Me.txtInvoice)
        Me.TPExpenseReport.Controls.Add(Me.lblExpenseInvoice)
        Me.TPExpenseReport.Controls.Add(Me.txtMultiplier)
        Me.TPExpenseReport.Controls.Add(Me.lblExpenseMultiplier)
        Me.TPExpenseReport.Controls.Add(Me.txtTotal)
        Me.TPExpenseReport.Controls.Add(Me.txtExpenseAmount)
        Me.TPExpenseReport.Controls.Add(Me.lblTotalExpenseAmount)
        Me.TPExpenseReport.Controls.Add(Me.lblExpenseAmount)
        Me.TPExpenseReport.Controls.Add(Me.txtCategory)
        Me.TPExpenseReport.Controls.Add(Me.Label12)
        Me.TPExpenseReport.Controls.Add(Me.Label13)
        Me.TPExpenseReport.Controls.Add(Me.Label11)
        Me.TPExpenseReport.Controls.Add(Me.Label10)
        Me.TPExpenseReport.Controls.Add(Me.dtpExpenseDate)
        Me.TPExpenseReport.Controls.Add(Me.Label9)
        Me.TPExpenseReport.Controls.Add(Me.CBPerdiem)
        Me.TPExpenseReport.Controls.Add(Me.btnExpenseUpdate)
        Me.TPExpenseReport.Controls.Add(Me.btnExpenseSave)
        Me.TPExpenseReport.Location = New System.Drawing.Point(4, 22)
        Me.TPExpenseReport.Name = "TPExpenseReport"
        Me.TPExpenseReport.Padding = New System.Windows.Forms.Padding(3)
        Me.TPExpenseReport.Size = New System.Drawing.Size(328, 497)
        Me.TPExpenseReport.TabIndex = 1
        Me.TPExpenseReport.Text = "Expense Report"
        '
        'lblTotalExpenseAmount
        '
        Me.lblTotalExpenseAmount.AutoSize = True
        Me.lblTotalExpenseAmount.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblTotalExpenseAmount.Location = New System.Drawing.Point(11, 316)
        Me.lblTotalExpenseAmount.Name = "lblTotalExpenseAmount"
        Me.lblTotalExpenseAmount.Size = New System.Drawing.Size(32, 13)
        Me.lblTotalExpenseAmount.TabIndex = 38
        Me.lblTotalExpenseAmount.Text = "Total"
        '
        'lblExpenseAmount
        '
        Me.lblExpenseAmount.AutoSize = True
        Me.lblExpenseAmount.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblExpenseAmount.Location = New System.Drawing.Point(11, 288)
        Me.lblExpenseAmount.Name = "lblExpenseAmount"
        Me.lblExpenseAmount.Size = New System.Drawing.Size(48, 13)
        Me.lblExpenseAmount.TabIndex = 37
        Me.lblExpenseAmount.Text = "Amount"
        '
        'txtCategory
        '
        Me.txtCategory.BackColor = System.Drawing.SystemColors.InactiveCaption
        Me.txtCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.txtCategory.FormattingEnabled = True
        Me.txtCategory.Items.AddRange(New Object() {"Transportation", "Meals", "Toll", "Parking", "Others"})
        Me.txtCategory.Location = New System.Drawing.Point(97, 186)
        Me.txtCategory.Name = "txtCategory"
        Me.txtCategory.Size = New System.Drawing.Size(199, 21)
        Me.txtCategory.TabIndex = 8
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label12.Location = New System.Drawing.Point(10, 189)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(53, 13)
        Me.Label12.TabIndex = 34
        Me.Label12.Text = "Category"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label13.Location = New System.Drawing.Point(11, 163)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(30, 13)
        Me.Label13.TabIndex = 33
        Me.Label13.Text = "Type"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label11.Location = New System.Drawing.Point(11, 216)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(60, 13)
        Me.Label11.TabIndex = 30
        Me.Label11.Text = "Particulars"
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label10.Location = New System.Drawing.Point(11, 55)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(82, 13)
        Me.Label10.TabIndex = 29
        Me.Label10.Text = "Hospital Name"
        '
        'dtpExpenseDate
        '
        Me.dtpExpenseDate.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.dtpExpenseDate.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpExpenseDate.Location = New System.Drawing.Point(98, 24)
        Me.dtpExpenseDate.Name = "dtpExpenseDate"
        Me.dtpExpenseDate.Size = New System.Drawing.Size(106, 22)
        Me.dtpExpenseDate.TabIndex = 28
        Me.dtpExpenseDate.TabStop = False
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(12, 29)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(69, 13)
        Me.Label9.TabIndex = 27
        Me.Label9.Text = "Date Service"
        '
        'CBPerdiem
        '
        Me.CBPerdiem.AutoSize = True
        Me.CBPerdiem.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CBPerdiem.Location = New System.Drawing.Point(14, 6)
        Me.CBPerdiem.Name = "CBPerdiem"
        Me.CBPerdiem.Size = New System.Drawing.Size(79, 17)
        Me.CBPerdiem.TabIndex = 26
        Me.CBPerdiem.TabStop = False
        Me.CBPerdiem.Text = "Allowance"
        Me.CBPerdiem.UseVisualStyleBackColor = True
        '
        'btnExpenseUpdate
        '
        Me.btnExpenseUpdate.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnExpenseUpdate.Location = New System.Drawing.Point(227, 465)
        Me.btnExpenseUpdate.Name = "btnExpenseUpdate"
        Me.btnExpenseUpdate.Size = New System.Drawing.Size(70, 24)
        Me.btnExpenseUpdate.TabIndex = 15
        Me.btnExpenseUpdate.Text = "Update"
        Me.btnExpenseUpdate.UseVisualStyleBackColor = True
        Me.btnExpenseUpdate.Visible = False
        '
        'btnExpenseSave
        '
        Me.btnExpenseSave.Location = New System.Drawing.Point(227, 465)
        Me.btnExpenseSave.Name = "btnExpenseSave"
        Me.btnExpenseSave.Size = New System.Drawing.Size(70, 24)
        Me.btnExpenseSave.TabIndex = 15
        Me.btnExpenseSave.Text = "Save"
        Me.btnExpenseSave.UseVisualStyleBackColor = True
        '
        'BTNFareClose
        '
        Me.BTNFareClose.Location = New System.Drawing.Point(6, 146)
        Me.BTNFareClose.Name = "BTNFareClose"
        Me.BTNFareClose.Size = New System.Drawing.Size(87, 22)
        Me.BTNFareClose.TabIndex = 87
        Me.BTNFareClose.Text = "Close"
        Me.BTNFareClose.UseVisualStyleBackColor = True
        '
        'GBTransportation
        '
        Me.GBTransportation.BackColor = System.Drawing.SystemColors.Control
        Me.GBTransportation.Controls.Add(Me.BTNFareClose)
        Me.GBTransportation.Controls.Add(Me.BTNAddFare)
        Me.GBTransportation.Controls.Add(Me.Label26)
        Me.GBTransportation.Controls.Add(Me.CBBFare)
        Me.GBTransportation.Controls.Add(Me.Label25)
        Me.GBTransportation.Controls.Add(Me.Label24)
        Me.GBTransportation.Controls.Add(Me.txtFrom)
        Me.GBTransportation.Controls.Add(Me.txtTo)
        Me.GBTransportation.Controls.Add(Me.BTNTransportation)
        Me.GBTransportation.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.GBTransportation.Location = New System.Drawing.Point(342, 312)
        Me.GBTransportation.Name = "GBTransportation"
        Me.GBTransportation.Size = New System.Drawing.Size(200, 174)
        Me.GBTransportation.TabIndex = 83
        Me.GBTransportation.TabStop = False
        Me.GBTransportation.Visible = False
        '
        'BTNDown
        '
        Me.BTNDown.Anchor = System.Windows.Forms.AnchorStyles.Right
        Me.BTNDown.Image = CType(resources.GetObject("BTNDown.Image"), System.Drawing.Image)
        Me.BTNDown.Location = New System.Drawing.Point(1007, 258)
        Me.BTNDown.Name = "BTNDown"
        Me.BTNDown.Size = New System.Drawing.Size(34, 54)
        Me.BTNDown.TabIndex = 85
        Me.BTNDown.UseVisualStyleBackColor = True
        '
        'BTNUp
        '
        Me.BTNUp.Anchor = System.Windows.Forms.AnchorStyles.Right
        Me.BTNUp.Image = CType(resources.GetObject("BTNUp.Image"), System.Drawing.Image)
        Me.BTNUp.Location = New System.Drawing.Point(1007, 201)
        Me.BTNUp.Name = "BTNUp"
        Me.BTNUp.Size = New System.Drawing.Size(34, 54)
        Me.BTNUp.TabIndex = 84
        Me.BTNUp.UseVisualStyleBackColor = True
        '
        'GBMeals
        '
        Me.GBMeals.BackColor = System.Drawing.SystemColors.Control
        Me.GBMeals.Controls.Add(Me.CBDinnerOTMeal)
        Me.GBMeals.Controls.Add(Me.RBOTMeal)
        Me.GBMeals.Controls.Add(Me.RBDinner)
        Me.GBMeals.Controls.Add(Me.BTNMealClose)
        Me.GBMeals.Controls.Add(Me.CLBPaidBill)
        Me.GBMeals.Controls.Add(Me.CBBPaidFor)
        Me.GBMeals.Controls.Add(Me.CLBMeals)
        Me.GBMeals.Controls.Add(Me.BTNMeals)
        Me.GBMeals.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.GBMeals.Location = New System.Drawing.Point(342, 132)
        Me.GBMeals.Name = "GBMeals"
        Me.GBMeals.Size = New System.Drawing.Size(200, 174)
        Me.GBMeals.TabIndex = 86
        Me.GBMeals.TabStop = False
        Me.GBMeals.Visible = False
        '
        'CBDinnerOTMeal
        '
        Me.CBDinnerOTMeal.AutoSize = True
        Me.CBDinnerOTMeal.Location = New System.Drawing.Point(6, 66)
        Me.CBDinnerOTMeal.Name = "CBDinnerOTMeal"
        Me.CBDinnerOTMeal.Size = New System.Drawing.Size(106, 30)
        Me.CBDinnerOTMeal.TabIndex = 83
        Me.CBDinnerOTMeal.Text = "Check for Dinner" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "or OT Meal"
        Me.CBDinnerOTMeal.UseVisualStyleBackColor = True
        '
        'RBOTMeal
        '
        Me.RBOTMeal.AutoSize = True
        Me.RBOTMeal.Enabled = False
        Me.RBOTMeal.Location = New System.Drawing.Point(6, 123)
        Me.RBOTMeal.Name = "RBOTMeal"
        Me.RBOTMeal.Size = New System.Drawing.Size(66, 17)
        Me.RBOTMeal.TabIndex = 82
        Me.RBOTMeal.TabStop = True
        Me.RBOTMeal.Text = "OT Meal"
        Me.RBOTMeal.UseVisualStyleBackColor = True
        '
        'RBDinner
        '
        Me.RBDinner.AutoSize = True
        Me.RBDinner.Enabled = False
        Me.RBDinner.Location = New System.Drawing.Point(6, 99)
        Me.RBDinner.Name = "RBDinner"
        Me.RBDinner.Size = New System.Drawing.Size(56, 17)
        Me.RBDinner.TabIndex = 81
        Me.RBDinner.Text = "Dinner"
        Me.RBDinner.UseVisualStyleBackColor = True
        '
        'BTNMealClose
        '
        Me.BTNMealClose.Location = New System.Drawing.Point(6, 146)
        Me.BTNMealClose.Name = "BTNMealClose"
        Me.BTNMealClose.Size = New System.Drawing.Size(87, 22)
        Me.BTNMealClose.TabIndex = 80
        Me.BTNMealClose.Text = "Close"
        Me.BTNMealClose.UseVisualStyleBackColor = True
        '
        'CLBPaidBill
        '
        Me.CLBPaidBill.BackColor = System.Drawing.SystemColors.Control
        Me.CLBPaidBill.CheckOnClick = True
        Me.CLBPaidBill.Enabled = False
        Me.CLBPaidBill.FormattingEnabled = True
        Me.CLBPaidBill.Location = New System.Drawing.Point(133, 34)
        Me.CLBPaidBill.Name = "CLBPaidBill"
        Me.CLBPaidBill.Size = New System.Drawing.Size(61, 94)
        Me.CLBPaidBill.TabIndex = 79
        Me.CLBPaidBill.Visible = False
        '
        'CBBPaidFor
        '
        Me.CBBPaidFor.AutoSize = True
        Me.CBBPaidFor.Enabled = False
        Me.CBBPaidFor.Location = New System.Drawing.Point(126, 13)
        Me.CBBPaidFor.Name = "CBBPaidFor"
        Me.CBBPaidFor.Size = New System.Drawing.Size(65, 17)
        Me.CBBPaidFor.TabIndex = 76
        Me.CBBPaidFor.Text = "Paid For"
        Me.CBBPaidFor.UseVisualStyleBackColor = True
        Me.CBBPaidFor.Visible = False
        '
        'CLBMeals
        '
        Me.CLBMeals.BackColor = System.Drawing.SystemColors.Control
        Me.CLBMeals.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.CLBMeals.CheckOnClick = True
        Me.CLBMeals.FormattingEnabled = True
        Me.CLBMeals.Items.AddRange(New Object() {"Breakfast", "Lunch"})
        Me.CLBMeals.Location = New System.Drawing.Point(6, 13)
        Me.CLBMeals.Name = "CLBMeals"
        Me.CLBMeals.Size = New System.Drawing.Size(73, 30)
        Me.CLBMeals.TabIndex = 72
        '
        'BTNMeals
        '
        Me.BTNMeals.Location = New System.Drawing.Point(107, 146)
        Me.BTNMeals.Name = "BTNMeals"
        Me.BTNMeals.Size = New System.Drawing.Size(87, 22)
        Me.BTNMeals.TabIndex = 74
        Me.BTNMeals.Text = "Done"
        Me.BTNMeals.UseVisualStyleBackColor = True
        '
        'FrmUserExpenseList
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1042, 523)
        Me.Controls.Add(Me.GBMeals)
        Me.Controls.Add(Me.BTNDown)
        Me.Controls.Add(Me.BTNUp)
        Me.Controls.Add(Me.TabControl1)
        Me.Controls.Add(Me.GBTransportation)
        Me.Controls.Add(Me.dgvExpense)
        Me.KeyPreview = True
        Me.Name = "FrmUserExpenseList"
        Me.Text = "Update"
        CType(Me.dgvExpense, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabControl1.ResumeLayout(False)
        Me.TPExpenseReport.ResumeLayout(False)
        Me.TPExpenseReport.PerformLayout()
        Me.GBTransportation.ResumeLayout(False)
        Me.GBTransportation.PerformLayout()
        Me.GBMeals.ResumeLayout(False)
        Me.GBMeals.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Label25 As Label
    Friend WithEvents txtFrom As TextBox
    Friend WithEvents BTNEditCategory As Button
    Friend WithEvents txtParticulars As RichTextBox
    Friend WithEvents txtType As ComboBox
    Friend WithEvents RBForeign As RadioButton
    Friend WithEvents RbLocal As RadioButton
    Friend WithEvents btnWorkWith As Button
    Friend WithEvents txtWorkWith As TextBox
    Friend WithEvents Label23 As Label
    Friend WithEvents txtTo As TextBox
    Friend WithEvents BTNTransportation As Button
    Friend WithEvents txtLocation As TextBox
    Friend WithEvents btnHospital As Button
    Friend WithEvents btnServiceNo As Button
    Friend WithEvents btnSerialNumber As Button
    Friend WithEvents Label24 As Label
    Friend WithEvents btnInstrumentHistory As Button
    Friend WithEvents txtSerialNumber As TextBox
    Friend WithEvents Label22 As Label
    Friend WithEvents txtInstrument As TextBox
    Friend WithEvents Label21 As Label
    Friend WithEvents txtServiceNumber As TextBox
    Friend WithEvents Label20 As Label
    Friend WithEvents CBBFare As ComboBox
    Friend WithEvents btnCancel As Button
    Friend WithEvents BTNAddFare As Button
    Friend WithEvents lblExpenseRemarks As Label
    Friend WithEvents txtRemarks As RichTextBox
    Friend WithEvents txtStatus As ComboBox
    Friend WithEvents lblExpenseStatus As Label
    Friend WithEvents txtInvoice As TextBox
    Friend WithEvents lblExpenseInvoice As Label
    Friend WithEvents txtMultiplier As TextBox
    Friend WithEvents lblExpenseMultiplier As Label
    Friend WithEvents txtTotal As TextBox
    Friend WithEvents Label26 As Label
    Friend WithEvents dgvExpense As DataGridView
    Friend WithEvents txtExpenseAmount As TextBox
    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TPExpenseReport As TabPage
    Friend WithEvents lblTotalExpenseAmount As Label
    Friend WithEvents lblExpenseAmount As Label
    Friend WithEvents txtCategory As ComboBox
    Friend WithEvents Label12 As Label
    Friend WithEvents Label13 As Label
    Friend WithEvents Label11 As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents dtpExpenseDate As DateTimePicker
    Friend WithEvents Label9 As Label
    Friend WithEvents CBPerdiem As CheckBox
    Friend WithEvents btnExpenseSave As Button
    Friend WithEvents btnExpenseUpdate As Button
    Friend WithEvents BTNFareClose As Button
    Friend WithEvents GBTransportation As GroupBox
    Friend WithEvents BTNDown As Button
    Friend WithEvents BTNUp As Button
    Friend WithEvents GBMeals As GroupBox
    Friend WithEvents CBDinnerOTMeal As CheckBox
    Friend WithEvents RBOTMeal As RadioButton
    Friend WithEvents RBDinner As RadioButton
    Friend WithEvents BTNMealClose As Button
    Friend WithEvents CLBPaidBill As CheckedListBox
    Friend WithEvents CBBPaidFor As CheckBox
    Friend WithEvents CLBMeals As CheckedListBox
    Friend WithEvents BTNMeals As Button
End Class
