<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmUserRegistration
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.txtUserlevel = New System.Windows.Forms.ComboBox()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.picSignature = New System.Windows.Forms.PictureBox()
        Me.txtEmailBcc = New System.Windows.Forms.TextBox()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.cbDepartment = New System.Windows.Forms.ComboBox()
        Me.txtEmailTo = New System.Windows.Forms.TextBox()
        Me.txtPosition = New System.Windows.Forms.TextBox()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.txtFullname = New System.Windows.Forms.TextBox()
        Me.txtUserID = New System.Windows.Forms.TextBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.txtPassword = New System.Windows.Forms.TextBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.txtUser = New System.Windows.Forms.TextBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnUpdate = New System.Windows.Forms.Button()
        Me.dgvUserAccount = New System.Windows.Forms.DataGridView()
        Me.fd = New System.Windows.Forms.OpenFileDialog()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.txtTransportationRate = New System.Windows.Forms.TextBox()
        Me.txtBreakFastRate = New System.Windows.Forms.TextBox()
        Me.txtLunchRate = New System.Windows.Forms.TextBox()
        Me.txtDinnerRate = New System.Windows.Forms.TextBox()
        Me.txtOTMeal = New System.Windows.Forms.TextBox()
        Me.txtApprover2 = New System.Windows.Forms.TextBox()
        Me.txtApprover1 = New System.Windows.Forms.TextBox()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.Label17 = New System.Windows.Forms.Label()
        CType(Me.picSignature, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvUserAccount, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(2, 4)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(40, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "UserID"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(2, 43)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(49, 13)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Fullname"
        '
        'ComboBox1
        '
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Items.AddRange(New Object() {"Central Team", "North Team", "South Team", "In-House"})
        Me.ComboBox1.Location = New System.Drawing.Point(5, 98)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(174, 21)
        Me.ComboBox1.TabIndex = 24
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(2, 82)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(34, 13)
        Me.Label5.TabIndex = 23
        Me.Label5.Text = "Team"
        '
        'btnCancel
        '
        Me.btnCancel.Location = New System.Drawing.Point(724, 121)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(75, 37)
        Me.btnCancel.TabIndex = 20
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'txtUserlevel
        '
        Me.txtUserlevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.txtUserlevel.FormattingEnabled = True
        Me.txtUserlevel.Items.AddRange(New Object() {"Admin", "User"})
        Me.txtUserlevel.Location = New System.Drawing.Point(185, 98)
        Me.txtUserlevel.Name = "txtUserlevel"
        Me.txtUserlevel.Size = New System.Drawing.Size(174, 21)
        Me.txtUserlevel.TabIndex = 4
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(182, 82)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(58, 13)
        Me.Label9.TabIndex = 16
        Me.Label9.Text = "User Level"
        '
        'picSignature
        '
        Me.picSignature.BackColor = System.Drawing.Color.Silver
        Me.picSignature.Location = New System.Drawing.Point(886, 0)
        Me.picSignature.Name = "picSignature"
        Me.picSignature.Size = New System.Drawing.Size(186, 161)
        Me.picSignature.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.picSignature.TabIndex = 15
        Me.picSignature.TabStop = False
        '
        'txtEmailBcc
        '
        Me.txtEmailBcc.Location = New System.Drawing.Point(365, 98)
        Me.txtEmailBcc.Name = "txtEmailBcc"
        Me.txtEmailBcc.Size = New System.Drawing.Size(174, 20)
        Me.txtEmailBcc.TabIndex = 11
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(267, 4)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(53, 13)
        Me.Label14.TabIndex = 15
        Me.Label14.Text = "Password"
        '
        'cbDepartment
        '
        Me.cbDepartment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cbDepartment.FormattingEnabled = True
        Me.cbDepartment.Location = New System.Drawing.Point(185, 59)
        Me.cbDepartment.Name = "cbDepartment"
        Me.cbDepartment.Size = New System.Drawing.Size(174, 21)
        Me.cbDepartment.TabIndex = 3
        '
        'txtEmailTo
        '
        Me.txtEmailTo.Location = New System.Drawing.Point(365, 59)
        Me.txtEmailTo.Name = "txtEmailTo"
        Me.txtEmailTo.Size = New System.Drawing.Size(174, 20)
        Me.txtEmailTo.TabIndex = 10
        '
        'txtPosition
        '
        Me.txtPosition.Location = New System.Drawing.Point(5, 138)
        Me.txtPosition.Name = "txtPosition"
        Me.txtPosition.Size = New System.Drawing.Size(174, 20)
        Me.txtPosition.TabIndex = 2
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(85, 4)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(55, 13)
        Me.Label13.TabIndex = 14
        Me.Label13.Text = "Username"
        '
        'txtFullname
        '
        Me.txtFullname.Location = New System.Drawing.Point(5, 59)
        Me.txtFullname.Name = "txtFullname"
        Me.txtFullname.Size = New System.Drawing.Size(174, 20)
        Me.txtFullname.TabIndex = 1
        '
        'txtUserID
        '
        Me.txtUserID.Location = New System.Drawing.Point(5, 20)
        Me.txtUserID.Name = "txtUserID"
        Me.txtUserID.Size = New System.Drawing.Size(77, 20)
        Me.txtUserID.TabIndex = 0
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(2, 122)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(44, 13)
        Me.Label3.TabIndex = 2
        Me.Label3.Text = "Position"
        '
        'txtPassword
        '
        Me.txtPassword.Location = New System.Drawing.Point(270, 20)
        Me.txtPassword.Name = "txtPassword"
        Me.txtPassword.PasswordChar = Global.Microsoft.VisualBasic.ChrW(42)
        Me.txtPassword.Size = New System.Drawing.Size(174, 20)
        Me.txtPassword.TabIndex = 7
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(182, 43)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(62, 13)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "Department"
        '
        'txtUser
        '
        Me.txtUser.Location = New System.Drawing.Point(88, 20)
        Me.txtUser.Name = "txtUser"
        Me.txtUser.Size = New System.Drawing.Size(174, 20)
        Me.txtUser.TabIndex = 6
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(362, 43)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(48, 13)
        Me.Label10.TabIndex = 18
        Me.Label10.Text = "Email To"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(362, 82)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(56, 13)
        Me.Label8.TabIndex = 19
        Me.Label8.Text = "Email BCC"
        '
        'btnSave
        '
        Me.btnSave.Location = New System.Drawing.Point(805, 121)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(75, 37)
        Me.btnSave.TabIndex = 12
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        'btnUpdate
        '
        Me.btnUpdate.Location = New System.Drawing.Point(805, 121)
        Me.btnUpdate.Name = "btnUpdate"
        Me.btnUpdate.Size = New System.Drawing.Size(75, 37)
        Me.btnUpdate.TabIndex = 21
        Me.btnUpdate.Text = "Update"
        Me.btnUpdate.UseVisualStyleBackColor = True
        Me.btnUpdate.Visible = False
        '
        'dgvUserAccount
        '
        Me.dgvUserAccount.AllowUserToAddRows = False
        Me.dgvUserAccount.AllowUserToDeleteRows = False
        Me.dgvUserAccount.AllowUserToResizeColumns = False
        Me.dgvUserAccount.AllowUserToResizeRows = False
        Me.dgvUserAccount.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgvUserAccount.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvUserAccount.Location = New System.Drawing.Point(5, 167)
        Me.dgvUserAccount.Name = "dgvUserAccount"
        Me.dgvUserAccount.ReadOnly = True
        Me.dgvUserAccount.RowHeadersVisible = False
        Me.dgvUserAccount.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Me.dgvUserAccount.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvUserAccount.Size = New System.Drawing.Size(1067, 545)
        Me.dgvUserAccount.TabIndex = 5
        Me.dgvUserAccount.TabStop = False
        '
        'fd
        '
        Me.fd.FileName = "OpenFileDialog1"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(542, 43)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(101, 13)
        Me.Label6.TabIndex = 27
        Me.Label6.Text = "Transportation Rate"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(542, 122)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(63, 13)
        Me.Label7.TabIndex = 26
        Me.Label7.Text = "Lunch Rate"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(542, 82)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(81, 13)
        Me.Label11.TabIndex = 25
        Me.Label11.Text = "BreakFast Rate"
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(721, 43)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(64, 13)
        Me.Label12.TabIndex = 28
        Me.Label12.Text = "Dinner Rate"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(721, 82)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(48, 13)
        Me.Label15.TabIndex = 29
        Me.Label15.Text = "OT Meal"
        '
        'txtTransportationRate
        '
        Me.txtTransportationRate.Location = New System.Drawing.Point(545, 59)
        Me.txtTransportationRate.Name = "txtTransportationRate"
        Me.txtTransportationRate.Size = New System.Drawing.Size(173, 20)
        Me.txtTransportationRate.TabIndex = 30
        '
        'txtBreakFastRate
        '
        Me.txtBreakFastRate.Location = New System.Drawing.Point(545, 98)
        Me.txtBreakFastRate.Name = "txtBreakFastRate"
        Me.txtBreakFastRate.Size = New System.Drawing.Size(173, 20)
        Me.txtBreakFastRate.TabIndex = 31
        '
        'txtLunchRate
        '
        Me.txtLunchRate.Location = New System.Drawing.Point(545, 138)
        Me.txtLunchRate.Name = "txtLunchRate"
        Me.txtLunchRate.Size = New System.Drawing.Size(173, 20)
        Me.txtLunchRate.TabIndex = 32
        '
        'txtDinnerRate
        '
        Me.txtDinnerRate.Location = New System.Drawing.Point(724, 59)
        Me.txtDinnerRate.Name = "txtDinnerRate"
        Me.txtDinnerRate.Size = New System.Drawing.Size(156, 20)
        Me.txtDinnerRate.TabIndex = 33
        '
        'txtOTMeal
        '
        Me.txtOTMeal.Location = New System.Drawing.Point(724, 98)
        Me.txtOTMeal.Name = "txtOTMeal"
        Me.txtOTMeal.Size = New System.Drawing.Size(156, 20)
        Me.txtOTMeal.TabIndex = 34
        '
        'txtApprover2
        '
        Me.txtApprover2.Location = New System.Drawing.Point(365, 138)
        Me.txtApprover2.Name = "txtApprover2"
        Me.txtApprover2.Size = New System.Drawing.Size(174, 20)
        Me.txtApprover2.TabIndex = 38
        '
        'txtApprover1
        '
        Me.txtApprover1.Location = New System.Drawing.Point(185, 138)
        Me.txtApprover1.Name = "txtApprover1"
        Me.txtApprover1.Size = New System.Drawing.Size(174, 20)
        Me.txtApprover1.TabIndex = 37
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.Location = New System.Drawing.Point(182, 122)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(59, 13)
        Me.Label16.TabIndex = 35
        Me.Label16.Text = "Approver 1"
        '
        'Label17
        '
        Me.Label17.AutoSize = True
        Me.Label17.Location = New System.Drawing.Point(362, 122)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(59, 13)
        Me.Label17.TabIndex = 36
        Me.Label17.Text = "Approver 2"
        '
        'frmUserRegistration
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1072, 713)
        Me.Controls.Add(Me.txtApprover2)
        Me.Controls.Add(Me.txtApprover1)
        Me.Controls.Add(Me.Label16)
        Me.Controls.Add(Me.Label17)
        Me.Controls.Add(Me.txtOTMeal)
        Me.Controls.Add(Me.txtDinnerRate)
        Me.Controls.Add(Me.txtLunchRate)
        Me.Controls.Add(Me.txtBreakFastRate)
        Me.Controls.Add(Me.txtTransportationRate)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.dgvUserAccount)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.picSignature)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.Label12)
        Me.Controls.Add(Me.Label15)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.ComboBox1)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Label10)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.txtUser)
        Me.Controls.Add(Me.txtUserlevel)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.Label9)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.txtPassword)
        Me.Controls.Add(Me.txtEmailBcc)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.txtUserID)
        Me.Controls.Add(Me.cbDepartment)
        Me.Controls.Add(Me.txtFullname)
        Me.Controls.Add(Me.txtEmailTo)
        Me.Controls.Add(Me.Label13)
        Me.Controls.Add(Me.txtPosition)
        Me.Controls.Add(Me.btnSave)
        Me.Controls.Add(Me.btnUpdate)
        Me.KeyPreview = True
        Me.Name = "frmUserRegistration"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "User Details"
        CType(Me.picSignature, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvUserAccount, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents cbDepartment As System.Windows.Forms.ComboBox
    Friend WithEvents txtPosition As System.Windows.Forms.TextBox
    Friend WithEvents txtFullname As System.Windows.Forms.TextBox
    Friend WithEvents txtUserID As System.Windows.Forms.TextBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents txtEmailBcc As System.Windows.Forms.TextBox
    Friend WithEvents txtEmailTo As System.Windows.Forms.TextBox
    Friend WithEvents txtPassword As System.Windows.Forms.TextBox
    Friend WithEvents txtUser As System.Windows.Forms.TextBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents Label14 As System.Windows.Forms.Label
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents dgvUserAccount As System.Windows.Forms.DataGridView
    Friend WithEvents picSignature As System.Windows.Forms.PictureBox
    Friend WithEvents fd As System.Windows.Forms.OpenFileDialog
    Friend WithEvents txtUserlevel As System.Windows.Forms.ComboBox
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents btnUpdate As System.Windows.Forms.Button
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label6 As Label
    Friend WithEvents Label7 As Label
    Friend WithEvents Label11 As Label
    Friend WithEvents Label12 As Label
    Friend WithEvents Label15 As Label
    Friend WithEvents txtTransportationRate As TextBox
    Friend WithEvents txtBreakFastRate As TextBox
    Friend WithEvents txtLunchRate As TextBox
    Friend WithEvents txtDinnerRate As TextBox
    Friend WithEvents txtOTMeal As TextBox
    Friend WithEvents txtApprover2 As TextBox
    Friend WithEvents txtApprover1 As TextBox
    Friend WithEvents Label16 As Label
    Friend WithEvents Label17 As Label
End Class
