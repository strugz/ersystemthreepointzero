Imports CrystalDecisions.Shared
Imports ERSystem.Domain
Imports ERSystem.Infrastructure.Data
Imports System.IO

Public Class frmRpt
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Public strExportFile As String = Nothing
    Public Property ShowReceiptPreviewPanel As Boolean

    Dim User As String
    Dim password As String

    Private ReadOnly _reportViewerContextService As New AppServices.ReportViewerContextService()
    Private ReadOnly _scannedReceiptAttachmentService As New ScannedReceiptAttachmentService()
    Private ReadOnly _viewerSplitContainer As New SplitContainer()
    Private ReadOnly _receiptPreviewPanel As New Panel()
    Private ReadOnly _receiptList As New ListBox()
    Private ReadOnly _receiptPreviewBox As New PictureBox()
    Private ReadOnly _receiptPreviewMessage As New Label()
    Private ReadOnly _btnOpenReceipt As New Button()
    Private _selectedReceiptImage As Image
    Private _receiptPreviewLayoutEventsAttached As Boolean

    Private Sub frmRpt_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        ClearReceiptImage()
        CrystalReportViewer1.ReportSource = Nothing
        ShowReceiptPreviewPanel = False
        frmApprove.dgvUser.Enabled = True
        frmApprove.dgvUserReportDetails.Enabled = True
        frmApprove.btnCancel.Enabled = True
        frmApprove.btnReportViewer.Enabled = True
        frmApprove.btnReject.Enabled = True
        frmApprove.btnApprove.Enabled = True
        Call ReleasMemory()
    End Sub

    Private Sub frmRpt_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim context As AppServices.ReportViewerContextResult = _reportViewerContextService.Load()

        ConfigureReceiptPreviewLayout(context)

        If Not context.HasSelection Then
            Return
        End If

        If context.IsAdminViewingOwnReport Then
            Me.CrystalReportViewer1.DisplayToolbar = True
            Me.CrystalReportViewer1.ShowPrintButton = False
            Me.CrystalReportViewer1.ShowExportButton = False
            Me.CrystalReportViewer1.ShowNextPage()
            btnSendPrint.Enabled = True
            CreateUserDSN()
        Else
            Call RPTValidation(context.Status, context.PrintStatus)
            CreateUserDSN()
        End If
    End Sub

    Public Sub export()
        Dim ExportER As New ReportDocument
        Dim context As AppServices.ReportViewerContextResult = _reportViewerContextService.Load()

        If Not context.HasSelection Then
            Return
        End If

        User = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
        password = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))

        ExportER.Load(Application.StartupPath & "\ER Report.rpt")
        ExportER.SetDatabaseLogon(User, password)
        ExportER.SetParameterValue("@UserID", ModDataStore.ReportUserID)
        ExportER.SetParameterValue("@reportID", context.ReportId)
        Dim dtp As DateTime = Date.Now
        If modLoadingData.RBT = "0" Then
            strExportFile = Application.StartupPath & "\ERPDF\" & GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) & "ER" & modLoadingData.sDate.ToString("ddMMMyyyy").ToUpper & ".pdf".ToString
        Else
            strExportFile = Application.StartupPath & "\ERPDF\" & GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) & modLoadingData.LocationCode & modLoadingData.sDate.ToString("ddMMMyyyy").ToUpper & ".pdf".ToString
        End If
        Dim ErExportOptions As ExportOptions
        Dim ERDiskDestinationOptions As New DiskFileDestinationOptions()
        Dim ErFormatTypeOptions As New PdfRtfWordFormatOptions()
        ERDiskDestinationOptions.DiskFileName = strExportFile
        ErExportOptions = ExportER.ExportOptions
        With ErExportOptions
            .ExportDestinationType = ExportDestinationType.DiskFile
            .ExportFormatType = ExportFormatType.PortableDocFormat
            .ExportDestinationOptions = ERDiskDestinationOptions
            .ExportFormatOptions = ErFormatTypeOptions
        End With
        ExportER.PrintOptions.PrinterDuplex = PrinterDuplex.Simplex
        ExportER.Export()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnSendPrint.Click
        Dim context As AppServices.ReportViewerContextResult = _reportViewerContextService.Load()

        If Not context.HasSelection Then
            MsgBox("Select Report To Send")
        Else
            Try
                If LoadReportSentStatus(context.ReportId).Rows(0).Item("ReportSentStatus").ToString() = "1" Then
                    CrystalReportViewer1.PrintReport()
                Else
                    frmERType.ShowDialog()
                    If LoadReportSentStatus(context.ReportId).Rows(0).Item("ReportSentStatus").ToString() = "1" Then
                        CrystalReportViewer1.PrintReport()
                    End If
                End If
            Catch ex As Exception
                MessageBox.Show("Sending Error Please Contact ID Administrator.")
            End Try
        End If
    End Sub

    Private Sub CrystalReportViewer1_Load(sender As Object, e As EventArgs) Handles CrystalReportViewer1.Load
        Dim context As AppServices.ReportViewerContextResult = _reportViewerContextService.Load()

        If Not context.HasSelection Then
            CrystalReportViewer1.ReportSource = Nothing
            Return
        End If

        CrystalReportViewer1.ReportSource = context.ViewerReport
        CrystalReportViewer1.Refresh()
    End Sub

    Private Sub ConfigureReceiptPreviewLayout(context As AppServices.ReportViewerContextResult)
        If Not ShowReceiptPreviewPanel Then
            Return
        End If

        InitializeReceiptPreviewPanel()

        If context IsNot Nothing AndAlso context.HasSelection Then
            LoadReceiptPreviewList(context.ReportId)
        Else
            ShowReceiptMessage("Select a report to load receipts.")
        End If
    End Sub

    Private Sub InitializeReceiptPreviewPanel()
        If _viewerSplitContainer.Parent Is Nothing Then
            Controls.Remove(CrystalReportViewer1)

            _viewerSplitContainer.Dock = DockStyle.Fill
            _viewerSplitContainer.FixedPanel = FixedPanel.Panel2
            _viewerSplitContainer.Panel1MinSize = 0
            _viewerSplitContainer.Panel2MinSize = 0
            _viewerSplitContainer.SplitterWidth = 6
            _viewerSplitContainer.Panel1.Controls.Add(CrystalReportViewer1)
            _viewerSplitContainer.Panel2.Controls.Add(_receiptPreviewPanel)
            Controls.Add(_viewerSplitContainer)
            Controls.SetChildIndex(_viewerSplitContainer, 0)

            If Not _receiptPreviewLayoutEventsAttached Then
                AddHandler _viewerSplitContainer.SizeChanged, AddressOf viewerSplitContainer_SizeChanged
                _receiptPreviewLayoutEventsAttached = True
            End If
        End If

        If _receiptPreviewPanel.Controls.Count = 0 Then
            _receiptPreviewPanel.Dock = DockStyle.Fill
            _receiptPreviewPanel.Padding = New Padding(8)

            Dim header As New Label() With {
                .Dock = DockStyle.Top,
                .Height = 28,
                .Font = New Font("Segoe UI Semibold", 10.0!, FontStyle.Bold, GraphicsUnit.Point, CType(0, Byte)),
                .Text = "Scanned receipts"
            }

            _btnOpenReceipt.Dock = DockStyle.Bottom
            _btnOpenReceipt.Enabled = False
            _btnOpenReceipt.Height = 32
            _btnOpenReceipt.Text = "Open Receipt"
            AddHandler _btnOpenReceipt.Click, AddressOf btnOpenReceipt_Click

            _receiptList.Dock = DockStyle.Top
            _receiptList.DisplayMember = "OriginalFileName"
            _receiptList.Height = 120
            AddHandler _receiptList.SelectedIndexChanged, AddressOf receiptList_SelectedIndexChanged
            AddHandler _receiptList.DoubleClick, AddressOf receiptList_DoubleClick

            _receiptPreviewBox.Dock = DockStyle.Fill
            _receiptPreviewBox.BackColor = Color.White
            _receiptPreviewBox.BorderStyle = BorderStyle.FixedSingle
            _receiptPreviewBox.SizeMode = PictureBoxSizeMode.Zoom

            _receiptPreviewMessage.Dock = DockStyle.Fill
            _receiptPreviewMessage.BackColor = Color.White
            _receiptPreviewMessage.BorderStyle = BorderStyle.FixedSingle
            _receiptPreviewMessage.TextAlign = ContentAlignment.MiddleCenter

            _receiptPreviewPanel.Controls.Add(_receiptPreviewBox)
            _receiptPreviewPanel.Controls.Add(_receiptPreviewMessage)
            _receiptPreviewPanel.Controls.Add(_receiptList)
            _receiptPreviewPanel.Controls.Add(_btnOpenReceipt)
            _receiptPreviewPanel.Controls.Add(header)
        End If

        BeginInvoke(New MethodInvoker(AddressOf ApplyReceiptPreviewSplitterSize))
    End Sub

    Private Sub viewerSplitContainer_SizeChanged(sender As Object, e As EventArgs)
        ApplyReceiptPreviewSplitterSize()
    End Sub

    Private Sub ApplyReceiptPreviewSplitterSize()
        If _viewerSplitContainer.Parent Is Nothing Then
            Return
        End If

        Dim availableWidth As Integer = _viewerSplitContainer.ClientSize.Width

        If availableWidth <= 0 Then
            Return
        End If

        Dim targetPanel2Width As Integer = Math.Min(340, Math.Max(220, availableWidth \ 3))
        Dim splitterDistance As Integer = Math.Max(0, availableWidth - targetPanel2Width)

        _viewerSplitContainer.Panel1MinSize = 0
        _viewerSplitContainer.Panel2MinSize = 0
        _viewerSplitContainer.SplitterDistance = Math.Min(Math.Max(0, splitterDistance), availableWidth)

        If availableWidth >= 620 Then
            _viewerSplitContainer.Panel1MinSize = 300
            _viewerSplitContainer.Panel2MinSize = 220
        End If
    End Sub

    Private Sub LoadReceiptPreviewList(reportId As String)
        ClearReceiptImage()
        _receiptList.DataSource = Nothing
        _btnOpenReceipt.Enabled = False

        If String.IsNullOrWhiteSpace(reportId) Then
            ShowReceiptMessage("Select a report to load receipts.")
            Return
        End If

        Try
            Dim receipts As List(Of ScannedReceiptAttachmentMetadataDto) = _scannedReceiptAttachmentService.GetMetadataByReportId(reportId)
            _receiptList.DataSource = receipts

            If receipts.Count = 0 Then
                ShowReceiptMessage("No scanned receipts were saved for this report.")
                Return
            End If

            _receiptList.SelectedIndex = 0
        Catch ex As Exception
            ShowReceiptMessage("Unable to load scanned receipts. " & ex.Message)
        End Try
    End Sub

    Private Sub receiptList_SelectedIndexChanged(sender As Object, e As EventArgs)
        Dim metadata As ScannedReceiptAttachmentMetadataDto = TryCast(_receiptList.SelectedItem, ScannedReceiptAttachmentMetadataDto)
        _btnOpenReceipt.Enabled = metadata IsNot Nothing

        If metadata Is Nothing Then
            ShowReceiptMessage("Select a receipt to preview.")
            Return
        End If

        PreviewReceipt(metadata)
    End Sub

    Private Sub receiptList_DoubleClick(sender As Object, e As EventArgs)
        OpenSelectedReceipt()
    End Sub

    Private Sub btnOpenReceipt_Click(sender As Object, e As EventArgs)
        OpenSelectedReceipt()
    End Sub

    Private Sub PreviewReceipt(metadata As ScannedReceiptAttachmentMetadataDto)
        If Not IsImageReceipt(metadata) Then
            ShowReceiptMessage("PDF receipt selected. Click Open Receipt to view it.")
            Return
        End If

        Try
            Dim receipt As ScannedReceiptAttachmentDto = _scannedReceiptAttachmentService.GetById(metadata.ID)

            If receipt Is Nothing OrElse receipt.ReceiptContent Is Nothing OrElse receipt.ReceiptContent.Length = 0 Then
                ShowReceiptMessage("Receipt content was not found.")
                Return
            End If

            Using stream As New MemoryStream(receipt.ReceiptContent)
                Using loadedImage As Image = Image.FromStream(stream)
                    ClearReceiptImage()
                    _selectedReceiptImage = New Bitmap(loadedImage)
                    _receiptPreviewBox.Image = _selectedReceiptImage
                    _receiptPreviewBox.Visible = True
                    _receiptPreviewMessage.Visible = False
                End Using
            End Using
        Catch ex As Exception
            ShowReceiptMessage("Unable to preview receipt. " & ex.Message)
        End Try
    End Sub

    Private Sub OpenSelectedReceipt()
        Dim metadata As ScannedReceiptAttachmentMetadataDto = TryCast(_receiptList.SelectedItem, ScannedReceiptAttachmentMetadataDto)

        If metadata Is Nothing Then
            Return
        End If

        Try
            Dim receipt As ScannedReceiptAttachmentDto = _scannedReceiptAttachmentService.GetById(metadata.ID)

            If receipt Is Nothing OrElse receipt.ReceiptContent Is Nothing OrElse receipt.ReceiptContent.Length = 0 Then
                MsgBox("Receipt content was not found.")
                Return
            End If

            Dim tempPath As String = Path.Combine(Path.GetTempPath(), BuildReceiptTempFileName(receipt))
            File.WriteAllBytes(tempPath, receipt.ReceiptContent)
            Process.Start(New ProcessStartInfo(tempPath) With {.UseShellExecute = True})
        Catch ex As Exception
            MsgBox("Unable to open receipt. " & ex.Message)
        End Try
    End Sub

    Private Shared Function IsImageReceipt(metadata As ScannedReceiptAttachmentMetadataDto) As Boolean
        If metadata Is Nothing OrElse String.IsNullOrWhiteSpace(metadata.ContentType) Then
            Return False
        End If

        Return metadata.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Function BuildReceiptTempFileName(receipt As ScannedReceiptAttachmentDto) As String
        Dim extension As String = If(receipt.FileExtension, String.Empty).Trim()

        If extension.Length = 0 OrElse Not extension.StartsWith(".", StringComparison.Ordinal) Then
            extension = ".bin"
        End If

        Dim safeName As String = Path.GetFileNameWithoutExtension(If(receipt.OriginalFileName, "receipt"))
        For Each invalidCharacter As Char In Path.GetInvalidFileNameChars()
            safeName = safeName.Replace(invalidCharacter, "_"c)
        Next

        If safeName.Length = 0 Then
            safeName = "receipt"
        End If

        Return String.Format("ERReceipt_{0}_{1}{2}", receipt.ID, safeName, extension)
    End Function

    Private Sub ShowReceiptMessage(message As String)
        ClearReceiptImage()
        _receiptPreviewMessage.Text = message
        _receiptPreviewMessage.Visible = True
        _receiptPreviewBox.Visible = False
    End Sub

    Private Sub ClearReceiptImage()
        _receiptPreviewBox.Image = Nothing

        If _selectedReceiptImage IsNot Nothing Then
            _selectedReceiptImage.Dispose()
            _selectedReceiptImage = Nothing
        End If
    End Sub
End Class
