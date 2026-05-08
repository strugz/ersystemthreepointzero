<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmConnection
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.cmbboxDataSource = New System.Windows.Forms.ComboBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.txtboxServerName = New System.Windows.Forms.TextBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.rdbtnLogOnDbWin = New System.Windows.Forms.RadioButton()
        Me.rdbtnLogOnDbSQL = New System.Windows.Forms.RadioButton()
        Me.txtboxUserName = New System.Windows.Forms.TextBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.txtboxPassword = New System.Windows.Forms.TextBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.txtboxDatabase = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.btnTest = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(12, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(66, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "DataSource"
        '
        'cmbboxDataSource
        '
        Me.cmbboxDataSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbboxDataSource.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.cmbboxDataSource.FormattingEnabled = True
        Me.cmbboxDataSource.Items.AddRange(New Object() {"Microsoft Access", "Miscrosoft SQL Server", "MYSQL", "ODBC"})
        Me.cmbboxDataSource.Location = New System.Drawing.Point(12, 25)
        Me.cmbboxDataSource.Name = "cmbboxDataSource"
        Me.cmbboxDataSource.Size = New System.Drawing.Size(288, 21)
        Me.cmbboxDataSource.TabIndex = 1
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(12, 49)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(66, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "Servername"
        '
        'txtboxServerName
        '
        Me.txtboxServerName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtboxServerName.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtboxServerName.Location = New System.Drawing.Point(12, 65)
        Me.txtboxServerName.Name = "txtboxServerName"
        Me.txtboxServerName.Size = New System.Drawing.Size(288, 22)
        Me.txtboxServerName.TabIndex = 3
        Me.txtboxServerName.Text = "192.168.4.96"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.BackColor = System.Drawing.Color.Transparent
        Me.Label3.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(12, 92)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(84, 13)
        Me.Label3.TabIndex = 4
        Me.Label3.Text = "Authentication"
        '
        'rdbtnLogOnDbWin
        '
        Me.rdbtnLogOnDbWin.AutoSize = True
        Me.rdbtnLogOnDbWin.BackColor = System.Drawing.Color.Transparent
        Me.rdbtnLogOnDbWin.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.rdbtnLogOnDbWin.Location = New System.Drawing.Point(46, 117)
        Me.rdbtnLogOnDbWin.Name = "rdbtnLogOnDbWin"
        Me.rdbtnLogOnDbWin.Size = New System.Drawing.Size(154, 17)
        Me.rdbtnLogOnDbWin.TabIndex = 5
        Me.rdbtnLogOnDbWin.Text = "Windows Authentication"
        Me.rdbtnLogOnDbWin.UseVisualStyleBackColor = False
        '
        'rdbtnLogOnDbSQL
        '
        Me.rdbtnLogOnDbSQL.AutoSize = True
        Me.rdbtnLogOnDbSQL.BackColor = System.Drawing.Color.Transparent
        Me.rdbtnLogOnDbSQL.Checked = True
        Me.rdbtnLogOnDbSQL.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.rdbtnLogOnDbSQL.Location = New System.Drawing.Point(46, 147)
        Me.rdbtnLogOnDbSQL.Name = "rdbtnLogOnDbSQL"
        Me.rdbtnLogOnDbSQL.Size = New System.Drawing.Size(158, 17)
        Me.rdbtnLogOnDbSQL.TabIndex = 6
        Me.rdbtnLogOnDbSQL.TabStop = True
        Me.rdbtnLogOnDbSQL.Text = "SQL Server Authentication"
        Me.rdbtnLogOnDbSQL.UseVisualStyleBackColor = False
        '
        'txtboxUserName
        '
        Me.txtboxUserName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtboxUserName.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtboxUserName.Location = New System.Drawing.Point(15, 194)
        Me.txtboxUserName.Name = "txtboxUserName"
        Me.txtboxUserName.Size = New System.Drawing.Size(288, 22)
        Me.txtboxUserName.TabIndex = 8
        Me.txtboxUserName.Text = "sa"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.BackColor = System.Drawing.Color.Transparent
        Me.Label4.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.Location = New System.Drawing.Point(15, 178)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(58, 13)
        Me.Label4.TabIndex = 7
        Me.Label4.Text = "Username"
        '
        'txtboxPassword
        '
        Me.txtboxPassword.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtboxPassword.Location = New System.Drawing.Point(15, 233)
        Me.txtboxPassword.Name = "txtboxPassword"
        Me.txtboxPassword.PasswordChar = Global.Microsoft.VisualBasic.ChrW(42)
        Me.txtboxPassword.Size = New System.Drawing.Size(288, 22)
        Me.txtboxPassword.TabIndex = 10
        Me.txtboxPassword.Text = "instantiation"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.BackColor = System.Drawing.Color.Transparent
        Me.Label5.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label5.Location = New System.Drawing.Point(15, 217)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(56, 13)
        Me.Label5.TabIndex = 9
        Me.Label5.Text = "Password"
        '
        'txtboxDatabase
        '
        Me.txtboxDatabase.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtboxDatabase.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtboxDatabase.Location = New System.Drawing.Point(15, 272)
        Me.txtboxDatabase.Name = "txtboxDatabase"
        Me.txtboxDatabase.Size = New System.Drawing.Size(288, 22)
        Me.txtboxDatabase.TabIndex = 12
        Me.txtboxDatabase.Text = "ExpenseReportService"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.BackColor = System.Drawing.Color.Transparent
        Me.Label6.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label6.Location = New System.Drawing.Point(15, 257)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(55, 13)
        Me.Label6.TabIndex = 11
        Me.Label6.Text = "Database"
        '
        'btnSave
        '
        Me.btnSave.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSave.Location = New System.Drawing.Point(147, 301)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(75, 40)
        Me.btnSave.TabIndex = 13
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCancel.Location = New System.Drawing.Point(228, 302)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(75, 40)
        Me.btnCancel.TabIndex = 14
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'btnTest
        '
        Me.btnTest.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnTest.Location = New System.Drawing.Point(15, 301)
        Me.btnTest.Name = "btnTest"
        Me.btnTest.Size = New System.Drawing.Size(75, 40)
        Me.btnTest.TabIndex = 15
        Me.btnTest.Text = "Test Connection"
        Me.btnTest.UseVisualStyleBackColor = True
        '
        'frmConnection
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.ClientSize = New System.Drawing.Size(312, 350)
        Me.Controls.Add(Me.btnTest)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.btnSave)
        Me.Controls.Add(Me.txtboxDatabase)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.txtboxPassword)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.txtboxUserName)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.rdbtnLogOnDbSQL)
        Me.Controls.Add(Me.rdbtnLogOnDbWin)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.txtboxServerName)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.cmbboxDataSource)
        Me.Controls.Add(Me.Label1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "frmConnection"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = " "
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents cmbboxDataSource As System.Windows.Forms.ComboBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtboxServerName As System.Windows.Forms.TextBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents rdbtnLogOnDbWin As System.Windows.Forms.RadioButton
    Friend WithEvents rdbtnLogOnDbSQL As System.Windows.Forms.RadioButton
    Friend WithEvents txtboxUserName As System.Windows.Forms.TextBox
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents txtboxPassword As System.Windows.Forms.TextBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents txtboxDatabase As System.Windows.Forms.TextBox
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents btnTest As System.Windows.Forms.Button
End Class
