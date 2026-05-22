<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FrmEReportDetailsV2
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TxtPurpose = New System.Windows.Forms.RichTextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.DtpReportFrom = New System.Windows.Forms.DateTimePicker()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.DtpReportTo = New System.Windows.Forms.DateTimePicker()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.CboReportType = New System.Windows.Forms.ComboBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.TxtReferenceNo = New System.Windows.Forms.TextBox()
        Me.GroupBoxCash = New System.Windows.Forms.GroupBox()
        Me.TxtRevolvingFund = New System.Windows.Forms.TextBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.TxtAmount = New System.Windows.Forms.TextBox()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.TxtRefDoc = New System.Windows.Forms.RichTextBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.DtpCashDate = New System.Windows.Forms.DateTimePicker()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.GroupBoxAttachment = New System.Windows.Forms.GroupBox()
        Me.BtnClearAttachment = New System.Windows.Forms.Button()
        Me.BtnBrowseAttachment = New System.Windows.Forms.Button()
        Me.TxtAttachment = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.BtnCancel = New System.Windows.Forms.Button()
        Me.BtnSave = New System.Windows.Forms.Button()
        Me.GroupBoxCash.SuspendLayout()
        Me.GroupBoxAttachment.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.Label1.Location = New System.Drawing.Point(12, 43)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(110, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Purpose Of Expense"
        '
        'TxtPurpose
        '
        Me.TxtPurpose.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.TxtPurpose.Location = New System.Drawing.Point(12, 59)
        Me.TxtPurpose.Name = "TxtPurpose"
        Me.TxtPurpose.Size = New System.Drawing.Size(412, 40)
        Me.TxtPurpose.TabIndex = 1
        Me.TxtPurpose.Text = ""
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.Label2.Location = New System.Drawing.Point(12, 113)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(33, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "From"
        '
        'DtpReportFrom
        '
        Me.DtpReportFrom.CustomFormat = "MM/dd/yyyy"
        Me.DtpReportFrom.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.DtpReportFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.DtpReportFrom.Location = New System.Drawing.Point(86, 107)
        Me.DtpReportFrom.Name = "DtpReportFrom"
        Me.DtpReportFrom.Size = New System.Drawing.Size(132, 22)
        Me.DtpReportFrom.TabIndex = 3
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.Label3.Location = New System.Drawing.Point(238, 113)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(18, 13)
        Me.Label3.TabIndex = 4
        Me.Label3.Text = "To"
        '
        'DtpReportTo
        '
        Me.DtpReportTo.CustomFormat = "MM/dd/yyyy"
        Me.DtpReportTo.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.DtpReportTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.DtpReportTo.Location = New System.Drawing.Point(292, 107)
        Me.DtpReportTo.Name = "DtpReportTo"
        Me.DtpReportTo.Size = New System.Drawing.Size(132, 22)
        Me.DtpReportTo.TabIndex = 5
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.Label4.Location = New System.Drawing.Point(12, 15)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(67, 13)
        Me.Label4.TabIndex = 6
        Me.Label4.Text = "Report Type"
        '
        'CboReportType
        '
        Me.CboReportType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.CboReportType.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.CboReportType.FormattingEnabled = True
        Me.CboReportType.Location = New System.Drawing.Point(86, 12)
        Me.CboReportType.Name = "CboReportType"
        Me.CboReportType.Size = New System.Drawing.Size(338, 21)
        Me.CboReportType.TabIndex = 7
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.Label5.Location = New System.Drawing.Point(12, 144)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(58, 13)
        Me.Label5.TabIndex = 8
        Me.Label5.Text = "Reference"
        '
        'TxtReferenceNo
        '
        Me.TxtReferenceNo.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.TxtReferenceNo.Location = New System.Drawing.Point(86, 141)
        Me.TxtReferenceNo.Name = "TxtReferenceNo"
        Me.TxtReferenceNo.ReadOnly = True
        Me.TxtReferenceNo.Size = New System.Drawing.Size(338, 22)
        Me.TxtReferenceNo.TabIndex = 9
        '
        'GroupBoxCash
        '
        Me.GroupBoxCash.Controls.Add(Me.TxtRevolvingFund)
        Me.GroupBoxCash.Controls.Add(Me.Label10)
        Me.GroupBoxCash.Controls.Add(Me.TxtAmount)
        Me.GroupBoxCash.Controls.Add(Me.Label9)
        Me.GroupBoxCash.Controls.Add(Me.TxtRefDoc)
        Me.GroupBoxCash.Controls.Add(Me.Label8)
        Me.GroupBoxCash.Controls.Add(Me.DtpCashDate)
        Me.GroupBoxCash.Controls.Add(Me.Label7)
        Me.GroupBoxCash.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.GroupBoxCash.Location = New System.Drawing.Point(12, 169)
        Me.GroupBoxCash.Name = "GroupBoxCash"
        Me.GroupBoxCash.Size = New System.Drawing.Size(412, 187)
        Me.GroupBoxCash.TabIndex = 10
        Me.GroupBoxCash.TabStop = False
        Me.GroupBoxCash.Text = "Cash Details"
        '
        'TxtRevolvingFund
        '
        Me.TxtRevolvingFund.Location = New System.Drawing.Point(126, 154)
        Me.TxtRevolvingFund.Name = "TxtRevolvingFund"
        Me.TxtRevolvingFund.Size = New System.Drawing.Size(275, 22)
        Me.TxtRevolvingFund.TabIndex = 7
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(6, 157)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(87, 13)
        Me.Label10.TabIndex = 6
        Me.Label10.Text = "Revolving Fund"
        '
        'TxtAmount
        '
        Me.TxtAmount.Location = New System.Drawing.Point(126, 126)
        Me.TxtAmount.Name = "TxtAmount"
        Me.TxtAmount.Size = New System.Drawing.Size(275, 22)
        Me.TxtAmount.TabIndex = 5
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(6, 129)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(48, 13)
        Me.Label9.TabIndex = 4
        Me.Label9.Text = "Amount"
        '
        'TxtRefDoc
        '
        Me.TxtRefDoc.Location = New System.Drawing.Point(126, 50)
        Me.TxtRefDoc.Name = "TxtRefDoc"
        Me.TxtRefDoc.Size = New System.Drawing.Size(275, 70)
        Me.TxtRefDoc.TabIndex = 3
        Me.TxtRefDoc.Text = ""
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(6, 53)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(114, 13)
        Me.Label8.TabIndex = 2
        Me.Label8.Text = "Reference Document"
        '
        'DtpCashDate
        '
        Me.DtpCashDate.CustomFormat = "MM/dd/yyyy"
        Me.DtpCashDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.DtpCashDate.Location = New System.Drawing.Point(126, 22)
        Me.DtpCashDate.Name = "DtpCashDate"
        Me.DtpCashDate.Size = New System.Drawing.Size(132, 22)
        Me.DtpCashDate.TabIndex = 1
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(6, 28)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(31, 13)
        Me.Label7.TabIndex = 0
        Me.Label7.Text = "Date"
        '
        'GroupBoxAttachment
        '
        Me.GroupBoxAttachment.Controls.Add(Me.BtnClearAttachment)
        Me.GroupBoxAttachment.Controls.Add(Me.BtnBrowseAttachment)
        Me.GroupBoxAttachment.Controls.Add(Me.TxtAttachment)
        Me.GroupBoxAttachment.Controls.Add(Me.Label6)
        Me.GroupBoxAttachment.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.GroupBoxAttachment.Location = New System.Drawing.Point(12, 362)
        Me.GroupBoxAttachment.Name = "GroupBoxAttachment"
        Me.GroupBoxAttachment.Size = New System.Drawing.Size(412, 76)
        Me.GroupBoxAttachment.TabIndex = 11
        Me.GroupBoxAttachment.TabStop = False
        Me.GroupBoxAttachment.Text = "Scanned Receipts"
        '
        'BtnClearAttachment
        '
        Me.BtnClearAttachment.Location = New System.Drawing.Point(329, 43)
        Me.BtnClearAttachment.Name = "BtnClearAttachment"
        Me.BtnClearAttachment.Size = New System.Drawing.Size(72, 24)
        Me.BtnClearAttachment.TabIndex = 3
        Me.BtnClearAttachment.Text = "Clear"
        Me.BtnClearAttachment.UseVisualStyleBackColor = True
        '
        'BtnBrowseAttachment
        '
        Me.BtnBrowseAttachment.Location = New System.Drawing.Point(251, 43)
        Me.BtnBrowseAttachment.Name = "BtnBrowseAttachment"
        Me.BtnBrowseAttachment.Size = New System.Drawing.Size(72, 24)
        Me.BtnBrowseAttachment.TabIndex = 2
        Me.BtnBrowseAttachment.Text = "Browse"
        Me.BtnBrowseAttachment.UseVisualStyleBackColor = True
        '
        'TxtAttachment
        '
        Me.TxtAttachment.Location = New System.Drawing.Point(71, 19)
        Me.TxtAttachment.Name = "TxtAttachment"
        Me.TxtAttachment.Size = New System.Drawing.Size(330, 22)
        Me.TxtAttachment.TabIndex = 1
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(6, 22)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(51, 13)
        Me.Label6.TabIndex = 0
        Me.Label6.Text = "File Path"
        '
        'BtnCancel
        '
        Me.BtnCancel.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.BtnCancel.Location = New System.Drawing.Point(246, 450)
        Me.BtnCancel.Name = "BtnCancel"
        Me.BtnCancel.Size = New System.Drawing.Size(86, 28)
        Me.BtnCancel.TabIndex = 12
        Me.BtnCancel.Text = "Cancel"
        Me.BtnCancel.UseVisualStyleBackColor = True
        '
        'BtnSave
        '
        Me.BtnSave.Font = New System.Drawing.Font("Segoe UI", 8.25!)
        Me.BtnSave.Location = New System.Drawing.Point(338, 450)
        Me.BtnSave.Name = "BtnSave"
        Me.BtnSave.Size = New System.Drawing.Size(86, 28)
        Me.BtnSave.TabIndex = 13
        Me.BtnSave.Text = "Save"
        Me.BtnSave.UseVisualStyleBackColor = True
        '
        'FrmEReportDetailsV2
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(436, 490)
        Me.Controls.Add(Me.BtnSave)
        Me.Controls.Add(Me.BtnCancel)
        Me.Controls.Add(Me.GroupBoxAttachment)
        Me.Controls.Add(Me.GroupBoxCash)
        Me.Controls.Add(Me.TxtReferenceNo)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.CboReportType)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.DtpReportTo)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.DtpReportFrom)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.TxtPurpose)
        Me.Controls.Add(Me.Label1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FrmEReportDetailsV2"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Report Details"
        Me.GroupBoxCash.ResumeLayout(False)
        Me.GroupBoxCash.PerformLayout()
        Me.GroupBoxAttachment.ResumeLayout(False)
        Me.GroupBoxAttachment.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents TxtPurpose As RichTextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents DtpReportFrom As DateTimePicker
    Friend WithEvents Label3 As Label
    Friend WithEvents DtpReportTo As DateTimePicker
    Friend WithEvents Label4 As Label
    Friend WithEvents CboReportType As ComboBox
    Friend WithEvents Label5 As Label
    Friend WithEvents TxtReferenceNo As TextBox
    Friend WithEvents GroupBoxCash As GroupBox
    Friend WithEvents TxtRevolvingFund As TextBox
    Friend WithEvents Label10 As Label
    Friend WithEvents TxtAmount As TextBox
    Friend WithEvents Label9 As Label
    Friend WithEvents TxtRefDoc As RichTextBox
    Friend WithEvents Label8 As Label
    Friend WithEvents DtpCashDate As DateTimePicker
    Friend WithEvents Label7 As Label
    Friend WithEvents GroupBoxAttachment As GroupBox
    Friend WithEvents BtnClearAttachment As Button
    Friend WithEvents BtnBrowseAttachment As Button
    Friend WithEvents TxtAttachment As TextBox
    Friend WithEvents Label6 As Label
    Friend WithEvents BtnCancel As Button
    Friend WithEvents BtnSave As Button
End Class
