Imports ERSystem.Domain
Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data
Imports System.Collections.Generic

Public Class frmSmsNotification
    Inherits Form

    Private ReadOnly _queueItem As FinanceErfQueueDto
    Private ReadOnly _smsNotificationService As Global.ERSystem.AppServices.ISmsNotificationService
    Private ReadOnly _userRegistrationService As IUserRegistrationService
    Private ReadOnly _userAccountRegistryProvider As New UserAccountRegistryProvider()
    Private ReadOnly _recipients As New List(Of SmsRecipientDto)()
    Private ReadOnly _lblUserIdentifier As New Label()
    Private ReadOnly _lstRecipients As New ListBox()
    Private ReadOnly _cboTemplate As New ComboBox()
    Private ReadOnly _txtMessage As New TextBox()
    Private ReadOnly _btnSend As New Button()
    Private ReadOnly _btnCancel As New Button()

    Friend Sub New(queueItem As FinanceErfQueueDto, smsNotificationService As Global.ERSystem.AppServices.ISmsNotificationService)
        Me.New(queueItem, smsNotificationService, New UserRegistrationService())
    End Sub

    Friend Sub New(queueItem As FinanceErfQueueDto,
                   smsNotificationService As Global.ERSystem.AppServices.ISmsNotificationService,
                   userRegistrationService As IUserRegistrationService)
        _queueItem = queueItem
        _smsNotificationService = smsNotificationService
        _userRegistrationService = userRegistrationService
        InitializeSmsNotificationForm()
    End Sub

    Private Sub InitializeSmsNotificationForm()
        Text = "Send to SMS"
        StartPosition = FormStartPosition.CenterParent
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ClientSize = New Size(520, 390)

        Dim contentPanel As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(12),
            .ColumnCount = 1,
            .RowCount = 8
        }
        contentPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 26))
        contentPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 24))
        contentPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 78))
        contentPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 24))
        contentPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 36))
        contentPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 24))
        contentPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        contentPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))

        _lblUserIdentifier.Dock = DockStyle.Fill
        _lblUserIdentifier.Text = "ERF User: " & GetUserIdentifier()

        _lstRecipients.Dock = DockStyle.Fill

        _cboTemplate.DropDownStyle = ComboBoxStyle.DropDownList
        _cboTemplate.Dock = DockStyle.Fill
        _cboTemplate.Items.AddRange({
            "Physical receipts reminder",
            "Receipts received notice",
            "Finance follow-up"
        })

        _txtMessage.Dock = DockStyle.Fill
        _txtMessage.Multiline = True
        _txtMessage.ScrollBars = ScrollBars.Vertical

        Dim buttonPanel As New FlowLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False
        }

        _btnSend.Text = "Send"
        _btnSend.Width = 90
        _btnCancel.Text = "Cancel"
        _btnCancel.Width = 90
        _btnCancel.DialogResult = DialogResult.Cancel
        buttonPanel.Controls.Add(_btnSend)
        buttonPanel.Controls.Add(_btnCancel)

        contentPanel.Controls.Add(_lblUserIdentifier, 0, 0)
        contentPanel.Controls.Add(New Label() With {.Text = "Recipients", .Dock = DockStyle.Fill}, 0, 1)
        contentPanel.Controls.Add(_lstRecipients, 0, 2)
        contentPanel.Controls.Add(New Label() With {.Text = "Notification Message", .Dock = DockStyle.Fill}, 0, 3)
        contentPanel.Controls.Add(_cboTemplate, 0, 4)
        contentPanel.Controls.Add(New Label() With {.Text = "Preview", .Dock = DockStyle.Fill}, 0, 5)
        contentPanel.Controls.Add(_txtMessage, 0, 6)
        contentPanel.Controls.Add(buttonPanel, 0, 7)

        Controls.Add(contentPanel)
        AcceptButton = _btnSend
        CancelButton = _btnCancel

        AddHandler _cboTemplate.SelectedIndexChanged, AddressOf cboTemplate_SelectedIndexChanged
        AddHandler _btnSend.Click, AddressOf btnSend_Click

        LoadRecipients()
        _cboTemplate.SelectedIndex = 0
    End Sub

    Private Sub LoadRecipients()
        _recipients.Clear()
        _lstRecipients.Items.Clear()

        Try
            If _queueItem IsNot Nothing Then
                Dim resolvedRecipients As List(Of SmsRecipientDto) = _userRegistrationService.ResolveSmsRecipientsForUser(_queueItem.UserID)
                _recipients.AddRange(resolvedRecipients)
            End If

            If _recipients.Count = 0 Then
                _lstRecipients.Items.Add("No SMS recipients found.")
                _btnSend.Enabled = False
                Return
            End If

            For Each recipient As SmsRecipientDto In _recipients
                _lstRecipients.Items.Add(FormatRecipient(recipient))
            Next

            _btnSend.Enabled = True
        Catch ex As Exception
            _lstRecipients.Items.Add("Unable to load SMS recipients.")
            _btnSend.Enabled = False
            MessageBox.Show("Unable to load SMS recipients. " & ex.Message)
        End Try
    End Sub

    Private Sub cboTemplate_SelectedIndexChanged(sender As Object, e As EventArgs)
        _txtMessage.Text = BuildTemplateMessage(Convert.ToString(_cboTemplate.SelectedItem))
    End Sub

    Private Sub btnSend_Click(sender As Object, e As EventArgs)
        Dim result As Global.ERSystem.AppServices.SmsNotificationResult = _smsNotificationService.Send(New SmsNotificationRequestDto With {
            .Recipients = New List(Of SmsRecipientDto)(_recipients),
            .Sender = GetCurrentSenderIdentifier(),
            .UserInitial = GetCurrentSenderIdentifier(),
            .Message = _txtMessage.Text,
            .ReportID = If(_queueItem Is Nothing, String.Empty, _queueItem.ReportID),
            .ReferenceNo = GetReferenceText()
        })

        MessageBox.Show(result.Message)
        If result.IsSuccess Then
            DialogResult = DialogResult.OK
            Close()
        End If
    End Sub

    Private Function BuildTemplateMessage(templateName As String) As String
        Dim userIdentifier As String = GetUserIdentifier()
        Dim referenceText As String = GetReferenceText()

        If String.Equals(templateName, "Receipts received notice", StringComparison.OrdinalIgnoreCase) Then
            Return userIdentifier & ", Finance has received the physical receipts for ERF " & referenceText & "."
        End If

        If String.Equals(templateName, "Finance follow-up", StringComparison.OrdinalIgnoreCase) Then
            Return userIdentifier & ", please contact Finance regarding ERF " & referenceText & "."
        End If

        Return userIdentifier & ", please submit your physical receipts to Finance for ERF " & referenceText & "."
    End Function

    Private Function GetUserIdentifier() As String
        If _queueItem Is Nothing OrElse String.IsNullOrWhiteSpace(_queueItem.Username) Then
            Return String.Empty
        End If

        Return _queueItem.Username.Trim()
    End Function

    Private Function GetReferenceText() As String
        If _queueItem Is Nothing Then
            Return String.Empty
        End If

        If Not String.IsNullOrWhiteSpace(_queueItem.ERFReferenceNo) Then
            Return _queueItem.ERFReferenceNo.Trim()
        End If

        If Not String.IsNullOrWhiteSpace(_queueItem.CashRefNo) Then
            Return _queueItem.CashRefNo.Trim()
        End If

        If Not String.IsNullOrWhiteSpace(_queueItem.ReportDescription) Then
            Return _queueItem.ReportDescription.Trim()
        End If

        Return _queueItem.ReportID
    End Function

    Private Function GetCurrentSenderIdentifier() As String
        Dim username As String = _userAccountRegistryProvider.GetValue("username")
        If Not String.IsNullOrWhiteSpace(username) Then
            Return username.Trim()
        End If

        Return GetUserIdentifier()
    End Function

    Private Shared Function FormatRecipient(recipient As SmsRecipientDto) As String
        If recipient Is Nothing Then
            Return String.Empty
        End If

        Dim displayText As String = recipient.RecipientType & ": " & recipient.Username
        If Not String.IsNullOrWhiteSpace(recipient.Fullname) Then
            displayText &= " - " & recipient.Fullname
        End If

        Return displayText
    End Function
End Class
