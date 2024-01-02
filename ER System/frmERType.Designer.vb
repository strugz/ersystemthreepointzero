<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmERType
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmERType))
        Me.rbtERF = New System.Windows.Forms.RadioButton()
        Me.RBLocation = New System.Windows.Forms.RadioButton()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.txtLocationCode = New System.Windows.Forms.TextBox()
        Me.txtLocationName = New System.Windows.Forms.TextBox()
        Me.btnSend = New System.Windows.Forms.Button()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.ToolStripStatusLabel1 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.PbbLoading = New System.Windows.Forms.PictureBox()
        Me.BWSending = New System.ComponentModel.BackgroundWorker()
        Me.StatusStrip1.SuspendLayout()
        CType(Me.PbbLoading, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'rbtERF
        '
        Me.rbtERF.AutoSize = True
        Me.rbtERF.BackColor = System.Drawing.Color.Transparent
        Me.rbtERF.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.rbtERF.Location = New System.Drawing.Point(62, 13)
        Me.rbtERF.Name = "rbtERF"
        Me.rbtERF.Size = New System.Drawing.Size(38, 17)
        Me.rbtERF.TabIndex = 0
        Me.rbtERF.TabStop = True
        Me.rbtERF.Text = "ER"
        Me.rbtERF.UseVisualStyleBackColor = False
        '
        'RBLocation
        '
        Me.RBLocation.AutoSize = True
        Me.RBLocation.BackColor = System.Drawing.Color.Transparent
        Me.RBLocation.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RBLocation.Location = New System.Drawing.Point(130, 13)
        Me.RBLocation.Name = "RBLocation"
        Me.RBLocation.Size = New System.Drawing.Size(84, 17)
        Me.RBLocation.TabIndex = 1
        Me.RBLocation.TabStop = True
        Me.RBLocation.Text = "Liquidation"
        Me.RBLocation.UseVisualStyleBackColor = False
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(8, 39)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(81, 13)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Location Code"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(8, 66)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(83, 13)
        Me.Label2.TabIndex = 3
        Me.Label2.Text = "Location Name"
        '
        'txtLocationCode
        '
        Me.txtLocationCode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtLocationCode.Enabled = False
        Me.txtLocationCode.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtLocationCode.Location = New System.Drawing.Point(91, 37)
        Me.txtLocationCode.Name = "txtLocationCode"
        Me.txtLocationCode.Size = New System.Drawing.Size(153, 22)
        Me.txtLocationCode.TabIndex = 4
        '
        'txtLocationName
        '
        Me.txtLocationName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtLocationName.Enabled = False
        Me.txtLocationName.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtLocationName.Location = New System.Drawing.Point(91, 63)
        Me.txtLocationName.Name = "txtLocationName"
        Me.txtLocationName.Size = New System.Drawing.Size(153, 22)
        Me.txtLocationName.TabIndex = 5
        '
        'btnSend
        '
        Me.btnSend.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSend.Location = New System.Drawing.Point(169, 89)
        Me.btnSend.Name = "btnSend"
        Me.btnSend.Size = New System.Drawing.Size(75, 29)
        Me.btnSend.TabIndex = 6
        Me.btnSend.Text = "Send"
        Me.btnSend.UseVisualStyleBackColor = True
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripStatusLabel1})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 104)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(252, 22)
        Me.StatusStrip1.TabIndex = 7
        Me.StatusStrip1.Visible = False
        '
        'ToolStripStatusLabel1
        '
        Me.ToolStripStatusLabel1.Name = "ToolStripStatusLabel1"
        Me.ToolStripStatusLabel1.Size = New System.Drawing.Size(74, 17)
        Me.ToolStripStatusLabel1.Text = "Sending . . . ."
        '
        'PbbLoading
        '
        Me.PbbLoading.Image = CType(resources.GetObject("PbbLoading.Image"), System.Drawing.Image)
        Me.PbbLoading.Location = New System.Drawing.Point(11, 93)
        Me.PbbLoading.Name = "PbbLoading"
        Me.PbbLoading.Size = New System.Drawing.Size(152, 20)
        Me.PbbLoading.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage
        Me.PbbLoading.TabIndex = 8
        Me.PbbLoading.TabStop = False
        Me.PbbLoading.Visible = False
        '
        'BWSending
        '
        '
        'frmERType
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center
        Me.ClientSize = New System.Drawing.Size(252, 126)
        Me.Controls.Add(Me.PbbLoading)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Controls.Add(Me.btnSend)
        Me.Controls.Add(Me.txtLocationName)
        Me.Controls.Add(Me.txtLocationCode)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.RBLocation)
        Me.Controls.Add(Me.rbtERF)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.KeyPreview = True
        Me.MaximizeBox = False
        Me.Name = "frmERType"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Send to Email"
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        CType(Me.PbbLoading, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents rbtERF As System.Windows.Forms.RadioButton
    Friend WithEvents RBLocation As System.Windows.Forms.RadioButton
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtLocationCode As System.Windows.Forms.TextBox
    Friend WithEvents txtLocationName As System.Windows.Forms.TextBox
    Friend WithEvents btnSend As System.Windows.Forms.Button
    Friend WithEvents StatusStrip1 As System.Windows.Forms.StatusStrip
    Friend WithEvents ToolStripStatusLabel1 As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents PbbLoading As PictureBox
    Friend WithEvents BWSending As System.ComponentModel.BackgroundWorker
End Class
