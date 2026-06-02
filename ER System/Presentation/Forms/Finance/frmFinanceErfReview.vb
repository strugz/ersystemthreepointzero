Imports ERSystem.Domain
Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data

Public Class frmFinanceErfReview
    Inherits Form

    Private Const SmsColumnName As String = "SendSms"

    Private ReadOnly _financeReviewService As IFinanceReviewService
    Private ReadOnly _smsNotificationService As Global.ERSystem.AppServices.ISmsNotificationService
    Private ReadOnly _userAccountRegistryProvider As New UserAccountRegistryProvider()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _txtEmployee As New TextBox()
    Private ReadOnly _cboStatus As New ComboBox()
    Private ReadOnly _cboReceipt As New ComboBox()
    Private ReadOnly _cboReportType As New ComboBox()
    Private ReadOnly _dtpFrom As New DateTimePicker()
    Private ReadOnly _dtpTo As New DateTimePicker()
    Private ReadOnly _chkUseDate As New CheckBox()
    Private ReadOnly _txtDetails As New TextBox()
    Private ReadOnly _txtRemarks As New TextBox()
    Private ReadOnly _btnRefresh As New Button()
    Private ReadOnly _btnMarkReceived As New Button()

    Private _selectedReportId As String = String.Empty

    Public Sub New()
        Me.New(New FinanceReviewService(), New Global.ERSystem.AppServices.SmsNotificationService())
    End Sub

    Public Sub New(financeReviewService As IFinanceReviewService)
        Me.New(financeReviewService, New Global.ERSystem.AppServices.SmsNotificationService())
    End Sub

    Friend Sub New(financeReviewService As IFinanceReviewService, smsNotificationService As Global.ERSystem.AppServices.ISmsNotificationService)
        _financeReviewService = financeReviewService
        _smsNotificationService = smsNotificationService
        InitializeFinanceReviewForm()
    End Sub

    Private Sub InitializeFinanceReviewForm()
        Text = "Finance ERF Review"
        StartPosition = FormStartPosition.CenterParent
        Size = New Size(1100, 700)
        MinimumSize = New Size(900, 560)
        WindowState = FormWindowState.Maximized
        KeyPreview = True

        Dim filtersPanel As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .Height = 76,
            .Padding = New Padding(10),
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True
        }

        _txtEmployee.Width = 160

        ConfigureCombo(_cboStatus, {"Pending", "Receipts Received", "All"}, "All")
        ConfigureCombo(_cboReceipt, {"Missing", "Received", "All"}, "All")
        ConfigureCombo(_cboReportType, {"All", "Replenishment of Revolving fund", "Liquidation for Cash Advance", "Reimbursement"}, "All")

        _chkUseDate.Text = "Date filter"
        _chkUseDate.AutoSize = True
        _dtpFrom.Format = DateTimePickerFormat.Short
        _dtpTo.Format = DateTimePickerFormat.Short

        _btnRefresh.Text = "Refresh"
        _btnRefresh.Width = 90

        filtersPanel.Controls.AddRange({
            BuildLabeledControl("Employee", _txtEmployee),
            BuildLabeledControl("Status", _cboStatus),
            BuildLabeledControl("Receipts", _cboReceipt),
            BuildLabeledControl("ERF Type", _cboReportType),
            _chkUseDate,
            BuildLabeledControl("From", _dtpFrom),
            BuildLabeledControl("To", _dtpTo),
            _btnRefresh
        })

        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.ReadOnly = True
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.MultiSelect = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

        Dim rightPanel As New Panel() With {
            .Dock = DockStyle.Right,
            .Width = 360,
            .Padding = New Padding(10)
        }

        _txtDetails.Dock = DockStyle.Top
        _txtDetails.Height = 320
        _txtDetails.Multiline = True
        _txtDetails.ReadOnly = True
        _txtDetails.ScrollBars = ScrollBars.Vertical

        _txtRemarks.Dock = DockStyle.Top
        _txtRemarks.Height = 90
        _txtRemarks.Multiline = True
        _txtRemarks.ScrollBars = ScrollBars.Vertical

        _btnMarkReceived.Text = "Receipts Received"
        _btnMarkReceived.Dock = DockStyle.Top
        _btnMarkReceived.Height = 40

        rightPanel.Controls.Add(_btnMarkReceived)
        rightPanel.Controls.Add(_txtRemarks)
        rightPanel.Controls.Add(New Label() With {.Text = "Finance Remarks", .Dock = DockStyle.Top, .Height = 22})
        rightPanel.Controls.Add(_txtDetails)
        rightPanel.Controls.Add(New Label() With {.Text = "ERF Details", .Dock = DockStyle.Top, .Height = 22})

        Controls.Add(_grid)
        Controls.Add(rightPanel)
        Controls.Add(filtersPanel)
        AcceptButton = _btnRefresh

        AddHandler Load, AddressOf frmFinanceErfReview_Load
        AddHandler KeyDown, AddressOf frmFinanceErfReview_KeyDown
        AddHandler _btnRefresh.Click, AddressOf btnRefresh_Click
        AddHandler _grid.SelectionChanged, AddressOf grid_SelectionChanged
        AddHandler _grid.CellContentClick, AddressOf grid_CellContentClick
        AddHandler _btnMarkReceived.Click, AddressOf btnMarkReceived_Click
    End Sub

    Private Shared Sub ConfigureCombo(combo As ComboBox, values As Object(), defaultValue As String)
        combo.DropDownStyle = ComboBoxStyle.DropDownList
        combo.Width = 170
        combo.Items.AddRange(values)

        Dim defaultIndex As Integer = combo.Items.IndexOf(defaultValue)
        If defaultIndex >= 0 Then
            combo.SelectedIndex = defaultIndex
        Else
            combo.SelectedIndex = 0
        End If
    End Sub

    Private Shared Function BuildLabeledControl(labelText As String, control As Control) As Control
        Dim panel As New Panel() With {.Width = Math.Max(control.Width, 120), .Height = 52}
        Dim label As New Label() With {.Text = labelText, .Dock = DockStyle.Top, .Height = 18}
        control.Dock = DockStyle.Bottom
        panel.Controls.Add(control)
        panel.Controls.Add(label)
        Return panel
    End Function

    Private Sub frmFinanceErfReview_Load(sender As Object, e As EventArgs)
        LoadQueue()
    End Sub

    Private Sub frmFinanceErfReview_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            Close()
        End If
    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs)
        LoadQueue()
    End Sub

    Private Sub grid_SelectionChanged(sender As Object, e As EventArgs)
        If _grid.CurrentRow Is Nothing OrElse _grid.CurrentRow.DataBoundItem Is Nothing Then
            _selectedReportId = String.Empty
            _txtDetails.Clear()
            UpdateActionButtons(Nothing)
            Return
        End If

        Dim row As FinanceErfQueueDto = TryCast(_grid.CurrentRow.DataBoundItem, FinanceErfQueueDto)
        If row Is Nothing Then
            Return
        End If

        _selectedReportId = row.ReportID
        LoadDetail(_selectedReportId)
    End Sub

    Private Sub grid_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then
            Return
        End If

        If Not String.Equals(_grid.Columns(e.ColumnIndex).Name, SmsColumnName, StringComparison.OrdinalIgnoreCase) Then
            Return
        End If

        Dim row As FinanceErfQueueDto = TryCast(_grid.Rows(e.RowIndex).DataBoundItem, FinanceErfQueueDto)
        If row Is Nothing Then
            Return
        End If

        Using smsDialog As New frmSmsNotification(row, _smsNotificationService)
            smsDialog.ShowDialog(Me)
        End Using
    End Sub

    Private Sub btnMarkReceived_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(_selectedReportId) Then
            MessageBox.Show("Please select an ERF.")
            Return
        End If

        Try
            _financeReviewService.MarkPhysicalReceiptsReceived(New MarkPhysicalReceiptsReceivedDto With {
                .ReportID = _selectedReportId,
                .ReviewerUserID = GetCurrentUserId(),
                .Remarks = _txtRemarks.Text
            })
            MessageBox.Show("Physical receipts received.")
            LoadQueue()
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub LoadQueue()
        Try
            Dim fromDate As Nullable(Of Date) = Nothing
            Dim toDate As Nullable(Of Date) = Nothing

            If _chkUseDate.Checked Then
                fromDate = _dtpFrom.Value.Date
                toDate = _dtpTo.Value.Date
            End If
            Dim rows = _financeReviewService.LoadQueue(
                Convert.ToString(_cboStatus.SelectedItem),
                Convert.ToString(_cboReceipt.SelectedItem),
                _txtEmployee.Text,
                fromDate,
                toDate,
                Convert.ToString(_cboReportType.SelectedItem))

            _grid.DataSource = rows
            ConfigureGridColumns()
        Catch ex As Exception
            MessageBox.Show("Unable to load Finance ERFs. Please make sure the finance tracking database script has been applied. " & ex.Message)
        End Try
    End Sub

    Private Sub ConfigureGridColumns()
        If _grid.Columns.Contains("ReportID") Then
            _grid.Columns("ReportID").Visible = False
        End If
        If _grid.Columns.Contains("Username") Then
            _grid.Columns("Username").Visible = False
        End If

        SetColumnHeader("UserID", "User ID")
        SetColumnHeader("EmployeeName", "Employee")
        SetColumnHeader("ReportDateFrom", "From")
        SetColumnHeader("ReportDateTo", "To")
        SetColumnHeader("ReportDescription", "Description")
        SetColumnHeader("ReportType", "ERF Type")
        SetColumnHeader("CashRefNo", "Ref No.")
        SetColumnHeader("FinanceStatus", "Status")
        SetColumnHeader("PhysicalReceiptsReceived", "Receipts")
        SetColumnHeader("PhysicalReceiptsReceivedDate", "Received Date")
        SetColumnHeader("FinanceRemarks", "Remarks")

        SetColumnWidth("UserID", 70)
        SetColumnWidth("ReportDateFrom", 90)
        SetColumnWidth("ReportDateTo", 90)
        SetColumnWidth("CashRefNo", 110)
        SetColumnWidth("FinanceStatus", 130)
        SetColumnWidth("PhysicalReceiptsReceived", 80)
        SetColumnWidth("PhysicalReceiptsReceivedDate", 120)

        If _grid.Columns.Contains("ReportDescription") Then
            _grid.Columns("ReportDescription").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        End If

        EnsureSmsColumn()
    End Sub

    Private Sub EnsureSmsColumn()
        If Not _grid.Columns.Contains(SmsColumnName) Then
            Dim smsColumn As New DataGridViewButtonColumn() With {
                .Name = SmsColumnName,
                .HeaderText = "SMS",
                .Text = "Send",
                .UseColumnTextForButtonValue = True,
                .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                .Width = 70
            }
            _grid.Columns.Add(smsColumn)
        End If

        _grid.Columns(SmsColumnName).DisplayIndex = _grid.Columns.Count - 1
    End Sub

    Private Sub SetColumnHeader(columnName As String, headerText As String)
        If _grid.Columns.Contains(columnName) Then
            _grid.Columns(columnName).HeaderText = headerText
        End If
    End Sub

    Private Sub SetColumnWidth(columnName As String, width As Integer)
        If _grid.Columns.Contains(columnName) Then
            _grid.Columns(columnName).AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            _grid.Columns(columnName).Width = width
        End If
    End Sub

    Private Sub LoadDetail(reportId As String)
        Try
            Dim detail As FinanceErfDetailDto = _financeReviewService.GetDetail(reportId)

            If detail Is Nothing Then
                _txtDetails.Clear()
                UpdateActionButtons(Nothing)
                Return
            End If

            _txtDetails.Text =
                "Employee: " & detail.EmployeeName & Environment.NewLine &
                "Description: " & detail.ReportDescription & Environment.NewLine &
                "Type: " & detail.ReportType & Environment.NewLine &
                "Date: " & FormatDate(detail.ReportDateFrom) & " to " & FormatDate(detail.ReportDateTo) & Environment.NewLine &
                "Reference No.: " & detail.CashRefNo & Environment.NewLine &
                "Cash Amount: " & If(detail.CashAmount.HasValue, detail.CashAmount.Value.ToString("N2"), String.Empty) & Environment.NewLine &
                "Revolving Fund: " & detail.RevolvingFund & Environment.NewLine &
                "Finance Status: " & detail.FinanceStatus & Environment.NewLine &
                "Physical Receipts: " & If(detail.PhysicalReceiptsReceived, "Received", "Missing") & Environment.NewLine &
                "Received Date: " & FormatDateTime(detail.PhysicalReceiptsReceivedDate) & Environment.NewLine &
                "Scanned Receipts Deleted: " & FormatDateTime(detail.ScannedReceiptsDeletedDate) & Environment.NewLine &
                "Remarks: " & detail.FinanceRemarks

            _txtRemarks.Text = If(detail.FinanceRemarks, String.Empty)
            UpdateActionButtons(detail)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub UpdateActionButtons(detail As FinanceErfDetailDto)
        Dim hasSelection As Boolean = detail IsNot Nothing
        _btnMarkReceived.Enabled = hasSelection AndAlso Not detail.PhysicalReceiptsReceived
    End Sub

    Private Function GetCurrentUserId() As Integer
        Dim value As String = _userAccountRegistryProvider.GetValue("UserID")
        Dim userId As Integer
        If Integer.TryParse(value, userId) Then
            Return userId
        End If

        Return 0
    End Function

    Private Shared Function FormatDate(value As Nullable(Of Date)) As String
        If Not value.HasValue Then
            Return String.Empty
        End If

        Return value.Value.ToString("MM/dd/yyyy")
    End Function

    Private Shared Function FormatDateTime(value As Nullable(Of DateTime)) As String
        If Not value.HasValue Then
            Return String.Empty
        End If

        Return value.Value.ToString("MM/dd/yyyy hh:mm tt")
    End Function

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        '
        'frmFinanceErfReview
        '
        Me.ClientSize = New System.Drawing.Size(743, 392)
        Me.Name = "frmFinanceErfReview"
        Me.ResumeLayout(False)

    End Sub
End Class
