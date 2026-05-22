Option Strict On

Imports System.IO
Imports ERSystem.AppServices
Imports ERSystem.Domain

Public Class frmAccountSettings
    Inherits Form

    Private ReadOnly _service As New AccountSettingsService(
        New ERSystem.Infrastructure.Data.AccountSettingsRepository(),
        New AccountSettingsSessionContext(),
        New AccountSettingsValueProtector())
    Private _account As AccountSettingsDto

    Private ReadOnly txtId As New TextBox()
    Private ReadOnly txtUserId As New TextBox()
    Private ReadOnly txtUserName As New TextBox()
    Private ReadOnly txtFullName As New TextBox()
    Private ReadOnly txtPosition As New TextBox()
    Private ReadOnly cboUserLevel As New ComboBox()
    Private ReadOnly cboDepartment As New ComboBox()
    Private ReadOnly txtEmailAdd As New TextBox()
    Private ReadOnly txtEmailPass As New TextBox()
    Private ReadOnly txtEmailTo As New TextBox()
    Private ReadOnly txtEmailBcc As New TextBox()
    Private ReadOnly txtStatus As New TextBox()
    Private ReadOnly cboApprover1 As New ComboBox()
    Private ReadOnly cboApprover2 As New ComboBox()
    Private ReadOnly txtReportNumberStatus As New TextBox()
    Private ReadOnly txtWorkWithStatus As New TextBox()
    Private ReadOnly txtSuperApprover As New TextBox()
    Private ReadOnly txtTranspoRate As New TextBox()
    Private ReadOnly txtBreakFastRate As New TextBox()
    Private ReadOnly txtLunchRate As New TextBox()
    Private ReadOnly txtDinnerRate As New TextBox()
    Private ReadOnly txtOtMeal As New TextBox()
    Private ReadOnly picSignature As New PictureBox()
    Private ReadOnly dgvAuthorities As New DataGridView()
    Private ReadOnly authorityTable As New DataTable()
    Private authorityUsers As DataTable

    Public Sub New()
        BuildAccountSettingsForm()
    End Sub

    Private Sub BuildAccountSettingsForm()
        Text = "Account Settings"
        StartPosition = FormStartPosition.CenterParent
        Size = New Size(1000, 720)
        MinimumSize = New Size(900, 640)
        KeyPreview = True

        Dim mainPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4,
            .Padding = New Padding(12)
        }
        mainPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 310.0F))
        mainPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 95.0F))
        mainPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        mainPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 44.0F))

        Dim profileGroup As New GroupBox With {.Text = "User Details", .Dock = DockStyle.Fill}
        Dim profileLayout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 6,
            .RowCount = 6,
            .Padding = New Padding(8)
        }
        profileLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120.0F))
        profileLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.0F))
        profileLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120.0F))
        profileLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.0F))
        profileLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 130.0F))
        profileLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 34.0F))
        profileLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38.0F))
        profileLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38.0F))
        profileLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38.0F))
        profileLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38.0F))
        profileLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38.0F))
        profileLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 74.0F))

        AddField(profileLayout, "ID", txtId, 0, 0)
        AddField(profileLayout, "User ID", txtUserId, 2, 0)
        AddField(profileLayout, "Username", txtUserName, 4, 0)
        AddField(profileLayout, "Full Name", txtFullName, 0, 1)
        AddField(profileLayout, "Position", txtPosition, 2, 1)
        AddField(profileLayout, "User Level", cboUserLevel, 4, 1)
        AddField(profileLayout, "Department", cboDepartment, 0, 2)
        AddField(profileLayout, "Email Address", txtEmailAdd, 2, 2)
        AddField(profileLayout, "Email Password", txtEmailPass, 4, 2)
        AddField(profileLayout, "Email To", txtEmailTo, 0, 3)
        AddField(profileLayout, "Email BCC", txtEmailBcc, 2, 3)
        AddField(profileLayout, "Status", txtStatus, 4, 3)
        AddField(profileLayout, "Approver 1", cboApprover1, 0, 4)
        AddField(profileLayout, "Approver 2", cboApprover2, 2, 4)
        AddField(profileLayout, "Report No. Status", txtReportNumberStatus, 4, 4)
        AddField(profileLayout, "Work With Status", txtWorkWithStatus, 0, 5)
        AddField(profileLayout, "Super Approver", txtSuperApprover, 2, 5)

        Dim signaturePanel As New FlowLayoutPanel With {.Dock = DockStyle.Top, .FlowDirection = FlowDirection.LeftToRight, .Margin = New Padding(0, 0, 0, 0), .Height = 62}
        picSignature.BorderStyle = BorderStyle.FixedSingle
        picSignature.Size = New Size(170, 54)
        picSignature.SizeMode = PictureBoxSizeMode.Zoom

        Dim btnChooseSignature As New Button With {.Text = "Choose Signature", .AutoSize = True}
        AddHandler btnChooseSignature.Click, AddressOf ChooseSignature_Click

        Dim btnClearSignature As New Button With {.Text = "Clear", .AutoSize = True}
        AddHandler btnClearSignature.Click, AddressOf ClearSignature_Click

        signaturePanel.Controls.Add(picSignature)
        signaturePanel.Controls.Add(btnChooseSignature)
        signaturePanel.Controls.Add(btnClearSignature)
        profileLayout.Controls.Add(New Label With {.Text = "Signature", .Dock = DockStyle.Top, .TextAlign = ContentAlignment.TopLeft, .AutoSize = False, .Height = 24, .Padding = New Padding(0, 4, 0, 0)}, 4, 5)
        profileLayout.Controls.Add(signaturePanel, 5, 5)

        profileGroup.Controls.Add(profileLayout)

        Dim rateGroup As New GroupBox With {.Text = "Employee Rates", .Dock = DockStyle.Fill}
        Dim rateLayout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 5, .RowCount = 2, .Padding = New Padding(8)}
        rateLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 24.0F))
        rateLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        For columnIndex As Integer = 0 To 4
            rateLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20.0F))
        Next
        AddRateField(rateLayout, "Transpo", txtTranspoRate, 0)
        AddRateField(rateLayout, "Breakfast", txtBreakFastRate, 1)
        AddRateField(rateLayout, "Lunch", txtLunchRate, 2)
        AddRateField(rateLayout, "Dinner", txtDinnerRate, 3)
        AddRateField(rateLayout, "OT Meal", txtOtMeal, 4)
        rateGroup.Controls.Add(rateLayout)

        Dim authorityGroup As New GroupBox With {.Text = "User Authority", .Dock = DockStyle.Fill}
        dgvAuthorities.Dock = DockStyle.Fill
        dgvAuthorities.AllowUserToAddRows = True
        dgvAuthorities.AllowUserToDeleteRows = True
        dgvAuthorities.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvAuthorities.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvAuthorities.RowHeadersVisible = False
        dgvAuthorities.ScrollBars = ScrollBars.Vertical
        dgvAuthorities.AllowUserToResizeColumns = False
        dgvAuthorities.AllowUserToResizeRows = False
        AddHandler dgvAuthorities.CellContentClick, AddressOf dgvAuthorities_CellContentClick
        AddHandler dgvAuthorities.DataError, AddressOf dgvAuthorities_DataError
        authorityGroup.Controls.Add(dgvAuthorities)

        Dim buttonPanel As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .Padding = New Padding(0, 8, 0, 0)
        }
        Dim btnSave As New Button With {.Text = "Save", .Width = 110}
        Dim btnCancel As New Button With {.Text = "Cancel", .Width = 110}
        AddHandler btnSave.Click, AddressOf Save_Click
        AddHandler btnCancel.Click, AddressOf Cancel_Click
        buttonPanel.Controls.Add(btnSave)
        buttonPanel.Controls.Add(btnCancel)

        mainPanel.Controls.Add(profileGroup, 0, 0)
        mainPanel.Controls.Add(rateGroup, 0, 1)
        mainPanel.Controls.Add(authorityGroup, 0, 2)
        mainPanel.Controls.Add(buttonPanel, 0, 3)
        Controls.Add(mainPanel)

        txtId.ReadOnly = True
        txtUserId.ReadOnly = True
        txtUserName.ReadOnly = True
        txtEmailPass.UseSystemPasswordChar = True

        cboUserLevel.DropDownStyle = ComboBoxStyle.DropDown
        cboUserLevel.Items.AddRange(New Object() {"Admin", "User", "Finance"})
        cboApprover1.DropDownStyle = ComboBoxStyle.DropDown
        cboApprover2.DropDownStyle = ComboBoxStyle.DropDown

        ConfigureAuthorityTable()
        AddHandler Load, AddressOf frmAccountSettings_Load
        AddHandler KeyDown, AddressOf frmAccountSettings_KeyDown
    End Sub

    Private Sub AddField(layout As TableLayoutPanel, labelText As String, control As Control, labelColumn As Integer, row As Integer)
        Dim label As New Label With {
            .Text = labelText,
            .Dock = DockStyle.Top,
            .TextAlign = ContentAlignment.TopLeft,
            .AutoSize = False,
            .Height = 24,
            .Padding = New Padding(0, 4, 0, 0)
        }

        control.Dock = DockStyle.Top
        control.Margin = New Padding(0, 1, 8, 0)
        layout.Controls.Add(label, labelColumn, row)
        layout.Controls.Add(control, labelColumn + 1, row)
    End Sub

    Private Sub AddRateField(layout As TableLayoutPanel, labelText As String, control As Control, column As Integer)
        Dim label As New Label With {
            .Text = labelText,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.BottomLeft,
            .AutoSize = False
        }

        control.Dock = DockStyle.Top
        control.Margin = New Padding(0, 2, 8, 0)
        layout.Controls.Add(label, column, 0)
        layout.Controls.Add(control, column, 1)
    End Sub

    Private Sub ConfigureAuthorityTable()
        authorityTable.Columns.Add("id", GetType(Long))
        authorityTable.Columns.Add("UserID", GetType(Integer))
        authorityTable.Columns.Add("AuthorityID", GetType(Integer))
        authorityTable.Columns.Add("AuthorityName", GetType(String))
        authorityTable.Columns.Add("Sort", GetType(Integer))

        dgvAuthorities.AutoGenerateColumns = False
        dgvAuthorities.DataSource = authorityTable
        dgvAuthorities.Columns.Clear()
        dgvAuthorities.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "id",
            .DataPropertyName = "id",
            .Visible = False
        })
        dgvAuthorities.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "UserID",
            .DataPropertyName = "UserID",
            .Visible = False
        })
        dgvAuthorities.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "AuthorityName",
            .DataPropertyName = "AuthorityName",
            .Visible = False
        })
        dgvAuthorities.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Sort",
            .HeaderText = "Sort",
            .DataPropertyName = "Sort",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            .Width = 80
        })
        dgvAuthorities.Columns.Add(New DataGridViewButtonColumn With {
            .Name = "RemoveAuthority",
            .HeaderText = "",
            .Text = "Delete",
            .UseColumnTextForButtonValue = True,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            .Width = 90
        })
    End Sub

    Private Sub frmAccountSettings_Load(sender As Object, e As EventArgs)
        Try
            cboDepartment.DataSource = ToDepartmentTable(_service.LoadDepartments())
            cboDepartment.DisplayMember = "emp_Dept"
            cboDepartment.ValueMember = "ID"
            authorityUsers = ToAuthorityUsersTable(_service.LoadAuthorityUsers())
            BindUserDropDown(cboApprover1)
            BindUserDropDown(cboApprover2)
            ConfigureAuthorityUserColumn()

            _account = _service.LoadCurrentAccount()
            BindAccount()
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Account Settings", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Close()
        End Try
    End Sub

    Private Shared Function ToDepartmentTable(departments As List(Of DepartmentDto)) As DataTable
        Dim table As New DataTable()
        table.Columns.Add("ID", GetType(Integer))
        table.Columns.Add("emp_Dept", GetType(String))

        For Each department As DepartmentDto In departments
            table.Rows.Add(department.Id, department.Name)
        Next

        Return table
    End Function

    Private Shared Function ToAuthorityUsersTable(users As List(Of AuthorityUserDto)) As DataTable
        Dim table As New DataTable()
        table.Columns.Add("UserID", GetType(Integer))
        table.Columns.Add("username", GetType(String))
        table.Columns.Add("Fullname", GetType(String))

        For Each user As AuthorityUserDto In users
            table.Rows.Add(user.UserId, user.UserName, user.FullName)
        Next

        Return table
    End Function

    Private Sub BindUserDropDown(comboBox As ComboBox)
        comboBox.DataSource = AuthorityUsersWithBlank()
        comboBox.DisplayMember = "username"
        comboBox.ValueMember = "username"
    End Sub

    Private Function AuthorityUsersWithBlank() As DataTable
        Dim users As New DataTable()
        users.Columns.Add("UserID", GetType(Integer))
        users.Columns.Add("username", GetType(String))
        users.Columns.Add("Fullname", GetType(String))

        Dim blankRow As DataRow = users.NewRow()
        blankRow("UserID") = DBNull.Value
        blankRow("username") = String.Empty
        blankRow("Fullname") = String.Empty
        users.Rows.Add(blankRow)

        For Each sourceRow As DataRow In authorityUsers.Rows
            Dim userRow As DataRow = users.NewRow()
            userRow("UserID") = sourceRow("UserID")
            userRow("username") = sourceRow("username")
            userRow("Fullname") = sourceRow("Fullname")
            users.Rows.Add(userRow)
        Next

        Return users
    End Function

    Private Sub ConfigureAuthorityUserColumn()
        If dgvAuthorities.Columns.Contains("AuthorityID") Then
            dgvAuthorities.Columns.Remove("AuthorityID")
        End If

        Dim authorityColumn As New DataGridViewComboBoxColumn With {
            .Name = "AuthorityID",
            .HeaderText = "Approver",
            .DataPropertyName = "AuthorityID",
            .DataSource = AuthorityUsersWithBlank(),
            .DisplayMember = "username",
            .ValueMember = "UserID",
            .DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .MinimumWidth = 220
        }

        dgvAuthorities.Columns.Insert(3, authorityColumn)
    End Sub

    Private Sub BindAccount()
        txtId.Text = If(_account.Id.HasValue, _account.Id.Value.ToString(), String.Empty)
        txtUserId.Text = _account.UserId.ToString()
        txtUserName.Text = _account.UserName
        txtFullName.Text = _account.FullName
        txtPosition.Text = _account.Position
        cboUserLevel.Text = _account.UserLevel
        If _account.DeptId.HasValue Then
            cboDepartment.SelectedValue = _account.DeptId.Value
        End If
        txtEmailAdd.Text = _account.EmailAdd
        txtEmailPass.Text = _account.EmailPass
        txtEmailTo.Text = _account.EmailTo
        txtEmailBcc.Text = _account.EmailBcc
        txtStatus.Text = _account.Status
        cboApprover1.Text = _account.Approver1
        cboApprover2.Text = _account.Approver2
        txtReportNumberStatus.Text = NullableIntegerToString(_account.ReportNumberStatus)
        txtWorkWithStatus.Text = _account.WorkWithStatus
        txtSuperApprover.Text = _account.SuperApprover
        txtTranspoRate.Text = NullableDoubleToString(_account.TranspoRate)
        txtBreakFastRate.Text = NullableDoubleToString(_account.BreakFastRate)
        txtLunchRate.Text = NullableDoubleToString(_account.LunchRate)
        txtDinnerRate.Text = NullableDoubleToString(_account.DinnerRate)
        txtOtMeal.Text = NullableDoubleToString(_account.OtMeal)
        LoadSignature(_account.Signature)
        LoadAuthorities()
    End Sub

    Private Sub LoadAuthorities()
        authorityTable.Rows.Clear()
        For Each authority As UserAuthorityDto In _account.AuthorityRows
            Dim row As DataRow = authorityTable.NewRow()
            row("id") = If(authority.Id.HasValue, CType(authority.Id.Value, Object), DBNull.Value)
            row("UserID") = _account.UserId
            row("AuthorityID") = If(authority.AuthorityId.HasValue, CType(authority.AuthorityId.Value, Object), DBNull.Value)
            row("AuthorityName") = If(authority.AuthorityName, String.Empty)
            row("Sort") = If(authority.Sort.HasValue, CType(authority.Sort.Value, Object), DBNull.Value)
            authorityTable.Rows.Add(row)
        Next
    End Sub

    Private Sub Save_Click(sender As Object, e As EventArgs)
        Try
            dgvAuthorities.EndEdit()
            BindingContext(authorityTable).EndCurrentEdit()
            ApplyFormValues()
            _service.SaveCurrentAccount(_account)
            MessageBox.Show("Account settings saved successfully.", "Account Settings", MessageBoxButtons.OK, MessageBoxIcon.Information)
            DialogResult = DialogResult.OK
            Close()
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Account Settings", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ApplyFormValues()
        _account.FullName = txtFullName.Text.Trim()
        _account.Position = txtPosition.Text.Trim()
        _account.UserLevel = cboUserLevel.Text.Trim()
        _account.DeptId = SelectedDepartmentId()
        _account.EmailAdd = txtEmailAdd.Text.Trim()
        _account.EmailPass = txtEmailPass.Text.Trim()
        _account.EmailTo = txtEmailTo.Text.Trim()
        _account.EmailBcc = txtEmailBcc.Text.Trim()
        _account.Status = LeftOrEmpty(txtStatus.Text, 1)
        _account.Approver1 = LeftOrEmpty(cboApprover1.Text, 5)
        _account.Approver2 = LeftOrEmpty(cboApprover2.Text, 5)
        _account.ReportNumberStatus = ParseOptionalInteger(txtReportNumberStatus.Text, "Report No. Status")
        _account.WorkWithStatus = LeftOrEmpty(txtWorkWithStatus.Text, 1)
        _account.SuperApprover = LeftOrEmpty(txtSuperApprover.Text, 5)
        _account.TranspoRate = ParseOptionalDouble(txtTranspoRate.Text, "Transpo Rate")
        _account.BreakFastRate = ParseOptionalDouble(txtBreakFastRate.Text, "Breakfast Rate")
        _account.LunchRate = ParseOptionalDouble(txtLunchRate.Text, "Lunch Rate")
        _account.DinnerRate = ParseOptionalDouble(txtDinnerRate.Text, "Dinner Rate")
        _account.OtMeal = ParseOptionalDouble(txtOtMeal.Text, "OT Meal")
        _account.Signature = SignatureBytes()

        _account.AuthorityRows.Clear()
        For Each row As DataRow In authorityTable.Rows
            If row.RowState = DataRowState.Deleted OrElse IsAuthorityRowEmpty(row) Then
                Continue For
            End If

            _account.AuthorityRows.Add(New UserAuthorityDto With {
                .UserId = _account.UserId,
                .AuthorityId = ReadOptionalInteger(row, "AuthorityID"),
                .AuthorityName = ResolveAuthorityName(ReadOptionalInteger(row, "AuthorityID"), ReadOptionalString(row, "AuthorityName", 10)),
                .Sort = ReadOptionalInteger(row, "Sort")
            })
        Next
    End Sub

    Private Function ResolveAuthorityName(authorityId As Integer?, fallbackName As String) As String
        If authorityId.HasValue AndAlso authorityUsers IsNot Nothing Then
            For Each userRow As DataRow In authorityUsers.Rows
                If Not userRow.IsNull("UserID") AndAlso Convert.ToInt32(userRow("UserID")) = authorityId.Value Then
                    Return LeftOrEmpty(userRow("username").ToString(), 10)
                End If
            Next
        End If

        Return LeftOrEmpty(fallbackName, 10)
    End Function

    Private Function SelectedDepartmentId() As Integer?
        If cboDepartment.SelectedValue Is Nothing OrElse TypeOf cboDepartment.SelectedValue Is DataRowView Then
            Return Nothing
        End If

        Return Convert.ToInt32(cboDepartment.SelectedValue)
    End Function

    Private Function SignatureBytes() As Byte()
        If picSignature.Image Is Nothing Then
            Return Nothing
        End If

        Using ms As New MemoryStream()
            picSignature.Image.Save(ms, Imaging.ImageFormat.Png)
            Return ms.ToArray()
        End Using
    End Function

    Private Sub LoadSignature(signature As Byte())
        If signature Is Nothing OrElse signature.Length = 0 Then
            picSignature.Image = Nothing
            Return
        End If

        Using ms As New MemoryStream(signature)
        Using image As Image = Image.FromStream(ms)
            picSignature.Image = New Bitmap(image)
        End Using
        End Using
    End Sub

    Private Sub ChooseSignature_Click(sender As Object, e As EventArgs)
        Using dialog As New OpenFileDialog()
            dialog.Title = "Select signature image"
            dialog.Filter = "Image Files|*.bmp;*.gif;*.jpg;*.jpeg;*.png|All Files|*.*"
            dialog.RestoreDirectory = True

            If dialog.ShowDialog(Me) = DialogResult.OK Then
                Using image As Image = Image.FromFile(dialog.FileName)
                    picSignature.Image = New Bitmap(image)
                End Using
            End If
        End Using
    End Sub

    Private Sub ClearSignature_Click(sender As Object, e As EventArgs)
        picSignature.Image = Nothing
    End Sub

    Private Sub Cancel_Click(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

    Private Sub frmAccountSettings_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            Close()
        End If
    End Sub

    Private Sub dgvAuthorities_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 OrElse Not dgvAuthorities.Columns(e.ColumnIndex).Name.Equals("RemoveAuthority", StringComparison.OrdinalIgnoreCase) Then
            Return
        End If

        If dgvAuthorities.Rows(e.RowIndex).IsNewRow Then
            Return
        End If

        dgvAuthorities.Rows.RemoveAt(e.RowIndex)
    End Sub

    Private Sub dgvAuthorities_DataError(sender As Object, e As DataGridViewDataErrorEventArgs)
        e.ThrowException = False
    End Sub

    Private Shared Function ParseOptionalDouble(value As String, fieldName As String) As Double?
        If String.IsNullOrWhiteSpace(value) Then
            Return Nothing
        End If

        Dim parsed As Double
        If Not Double.TryParse(value.Trim(), parsed) Then
            Throw New InvalidOperationException(fieldName & " must be a valid number.")
        End If

        Return parsed
    End Function

    Private Shared Function ParseOptionalInteger(value As String, fieldName As String) As Integer?
        If String.IsNullOrWhiteSpace(value) Then
            Return Nothing
        End If

        Dim parsed As Integer
        If Not Integer.TryParse(value.Trim(), parsed) Then
            Throw New InvalidOperationException(fieldName & " must be a valid whole number.")
        End If

        Return parsed
    End Function

    Private Shared Function ReadOptionalInteger(row As DataRow, columnName As String) As Integer?
        If row.IsNull(columnName) OrElse String.IsNullOrWhiteSpace(row(columnName).ToString()) Then
            Return Nothing
        End If

        Dim parsed As Integer
        If Not Integer.TryParse(row(columnName).ToString(), parsed) Then
            Throw New InvalidOperationException(columnName & " must be a valid whole number.")
        End If

        Return parsed
    End Function

    Private Shared Function ReadOptionalString(row As DataRow, columnName As String, maxLength As Integer) As String
        If row.IsNull(columnName) Then
            Return String.Empty
        End If

        Return LeftOrEmpty(row(columnName).ToString(), maxLength)
    End Function

    Private Shared Function IsAuthorityRowEmpty(row As DataRow) As Boolean
        Return (row.IsNull("AuthorityID") OrElse String.IsNullOrWhiteSpace(row("AuthorityID").ToString())) AndAlso
               (row.IsNull("AuthorityName") OrElse String.IsNullOrWhiteSpace(row("AuthorityName").ToString())) AndAlso
               (row.IsNull("Sort") OrElse String.IsNullOrWhiteSpace(row("Sort").ToString()))
    End Function

    Private Shared Function NullableDoubleToString(value As Double?) As String
        If value.HasValue Then
            Return value.Value.ToString()
        End If

        Return String.Empty
    End Function

    Private Shared Function NullableIntegerToString(value As Integer?) As String
        If value.HasValue Then
            Return value.Value.ToString()
        End If

        Return String.Empty
    End Function

    Private Shared Function LeftOrEmpty(value As String, maxLength As Integer) As String
        If String.IsNullOrWhiteSpace(value) Then
            Return String.Empty
        End If

        Dim trimmed As String = value.Trim()
        If trimmed.Length <= maxLength Then
            Return trimmed
        End If

        Return trimmed.Substring(0, maxLength)
    End Function
End Class
