Imports ERSystem.Domain
Imports ERSystem.Infrastructure.Data
Imports System.IO

Public Class FrmEReportDetailsV2
    Private Const ReportTypeReplenishment As String = "Replenishment of Revolving fund"
    Private Const ReportTypeLiquidation As String = "Liquidation for Cash Advance"
    Private Const ReportTypeReimbursement As String = "Reimbursement"
    Private Const ScannedReceiptsFolderName As String = "ScannedReceipts"

    Private ReadOnly _reportDetailService As IReportDetailService
    Private ReadOnly _cashAdvanceService As ICashAdvanceService
    Private ReadOnly _scannedReceiptAttachmentService As ScannedReceiptAttachmentService
    Private ReadOnly _selectedReportContextService As New AppServices.SelectedReportContextService()
    Private _reportId As String = String.Empty
    Private _lastAutoPurpose As String = String.Empty
    Private _receiptSelection As ERSystem.AppServices.ScannedReceiptSelectionState

    Public Sub New()
        Me.New(New ReportDetailService(), New CashAdvanceService(), New ScannedReceiptAttachmentService())
    End Sub

    Public Sub New(reportDetailService As IReportDetailService, cashAdvanceService As ICashAdvanceService)
        Me.New(reportDetailService, cashAdvanceService, New ScannedReceiptAttachmentService())
    End Sub

    Public Sub New(reportDetailService As IReportDetailService,
                   cashAdvanceService As ICashAdvanceService,
                   scannedReceiptAttachmentService As ScannedReceiptAttachmentService)
        InitializeComponent()
        _reportDetailService = reportDetailService
        _cashAdvanceService = cashAdvanceService
        _scannedReceiptAttachmentService = scannedReceiptAttachmentService
    End Sub

    Private Sub FrmEReportDetailsV2_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InitializeReportTypes()
        LoadSelectedReport()
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs) Handles BtnSave.Click
        If Not ValidateForm() Then
            Return
        End If

        Try
            If String.IsNullOrWhiteSpace(_reportId) Then
                CreateReport()
                MessageBox.Show("Add Successfully")
            Else
                UpdateReport()
                MessageBox.Show("Update Successfully")
            End If
        Catch ex As InvalidOperationException
            MessageBox.Show(ex.Message)
            Return
        End Try

        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs) Handles BtnCancel.Click
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

    Private Sub BtnBrowseAttachment_Click(sender As Object, e As EventArgs) Handles BtnBrowseAttachment.Click
        Using dialog As New OpenFileDialog()
            dialog.Filter = "Receipt Files|*.pdf;*.jpg;*.jpeg;*.png|PDF Files|*.pdf|Image Files|*.jpg;*.jpeg;*.png"
            dialog.Multiselect = True
            dialog.Title = "Select scanned receipt attachment"

            If dialog.ShowDialog(Me) = DialogResult.OK Then
                Try
                    Dim selectedPaths As List(Of String) = ValidateSelectedAttachmentFiles(dialog.FileNames)
                    _receiptSelection.AddLocalPaths(selectedPaths)
                    RefreshAttachmentDisplay()
                Catch ex As InvalidOperationException
                    MessageBox.Show(ex.Message)
                End Try
            End If
        End Using
    End Sub

    Private Sub BtnClearAttachment_Click(sender As Object, e As EventArgs) Handles BtnClearAttachment.Click
        If _receiptSelection Is Nothing OrElse Not _receiptSelection.HasReceipts Then
            Return
        End If

        If MessageBox.Show("Remove all scanned receipts from this report?",
                           "Confirm Receipt Removal",
                           MessageBoxButtons.YesNo,
                           MessageBoxIcon.Warning) <> DialogResult.Yes Then
            Return
        End If

        Try
            _receiptSelection.Clear()
            RefreshAttachmentDisplay()
        Catch ex As InvalidOperationException
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub CboReportType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CboReportType.SelectedIndexChanged
        ApplyReportTypePurpose()
        ApplyReportTypeState()
    End Sub

    Private Sub InitializeReportTypes()
        If CboReportType.Items.Count = 0 Then
            CboReportType.Items.AddRange(New Object() {ReportTypeReplenishment, ReportTypeLiquidation, ReportTypeReimbursement})
        End If
    End Sub

    Private Sub LoadSelectedReport()
        Dim selectedReport = _selectedReportContextService.Load()

        If Not selectedReport.HasSelection Then
            PrepareNewReport()
            Return
        End If

        _reportId = selectedReport.ReportId
        Dim report = _reportDetailService.GetById(_reportId)
        Dim cashAdvance = _cashAdvanceService.GetByReportId(_reportId).FirstOrDefault()

        If report Is Nothing Then
            PrepareNewReport()
            Return
        End If

        DtpReportFrom.Value = If(report.ReportDateFrom.HasValue, report.ReportDateFrom.Value, DateTime.Now)
        DtpReportTo.Value = If(report.ReportDateTo.HasValue, report.ReportDateTo.Value, DateTime.Now)
        Dim storedReceipts As List(Of ScannedReceiptAttachmentMetadataDto)
        Try
            storedReceipts = _scannedReceiptAttachmentService.GetMetadataByReportId(_reportId)
        Catch ex As Exception
            storedReceipts = New List(Of ScannedReceiptAttachmentMetadataDto)()
            MessageBox.Show("Unable to load scanned receipt information. Existing receipts will be preserved. " & ex.Message)
        End Try

        _receiptSelection = New ERSystem.AppServices.ScannedReceiptSelectionState(
            report.ReportAttachment,
            storedReceipts,
            IsFinalApprovedReport(report))
        RefreshAttachmentDisplay()
        SelectReportType(ResolveReportType(report, cashAdvance))
        TxtPurpose.Text = report.ReportDescription
        TxtERFReferenceNo.Text = ResolveErfReferenceNo(report, cashAdvance)
        _lastAutoPurpose = If(CboReportType.SelectedItem Is Nothing, String.Empty, CboReportType.SelectedItem.ToString())

        If cashAdvance IsNot Nothing Then
            TxtReferenceNo.Text = ResolveCashReferenceNo(report, cashAdvance)
            DtpCashDate.Value = ParseDateOrToday(cashAdvance.CashDate)
            TxtRefDoc.Text = cashAdvance.CashRefDoc
            TxtAmount.Text = If(cashAdvance.CashAmount.HasValue, cashAdvance.CashAmount.Value.ToString(), String.Empty)
            TxtRevolvingFund.Text = cashAdvance.RevolvingFund
        End If

        BtnSave.Text = "Update"
        ApplyAttachmentState()
        ApplyReportTypeState()
    End Sub

    Private Sub PrepareNewReport()
        _reportId = String.Empty
        TxtPurpose.Clear()
        DtpReportFrom.Value = DateTime.Now
        DtpReportTo.Value = DateTime.Now
        DtpCashDate.Value = DateTime.Now
        TxtRefDoc.Clear()
        TxtReferenceNo.Clear()
        TxtAmount.Clear()
        TxtRevolvingFund.Clear()
        _receiptSelection = New ERSystem.AppServices.ScannedReceiptSelectionState(
            String.Empty,
            Enumerable.Empty(Of ScannedReceiptAttachmentMetadataDto)(),
            False)
        RefreshAttachmentDisplay()
        TxtERFReferenceNo.Text = GenerateReferenceNumber()
        CboReportType.SelectedIndex = 0
        BtnSave.Text = "Save"
        ApplyAttachmentState()
        ApplyReportTypeState()
    End Sub

    Private Function ValidateForm() As Boolean
        If String.IsNullOrWhiteSpace(TxtPurpose.Text) Then
            MessageBox.Show("Please enter the purpose of expense.")
            TxtPurpose.Focus()
            Return False
        End If

        If CboReportType.SelectedItem Is Nothing Then
            MessageBox.Show("Please select a report type.")
            CboReportType.Focus()
            Return False
        End If

        If String.IsNullOrWhiteSpace(TxtERFReferenceNo.Text) Then
            TxtERFReferenceNo.Text = GenerateReferenceNumber()
        End If

        If IsReplenishment() AndAlso String.IsNullOrWhiteSpace(TxtRevolvingFund.Text) Then
            MessageBox.Show("Please enter the revolving fund amount.")
            TxtRevolvingFund.Focus()
            Return False
        End If

        If IsLiquidation() AndAlso String.IsNullOrWhiteSpace(TxtAmount.Text) Then
            MessageBox.Show("Please enter the cash advance amount.")
            TxtAmount.Focus()
            Return False
        End If

        Return True
    End Function

    Private Sub CreateReport()
        Dim newReportId As String = Guid.NewGuid().ToString()
        Dim userId As Integer = GetCurrentUserId()
        Dim copiedPaths As New List(Of String)()
        Dim createdFiles As New List(Of String)()

        Try
            copiedPaths = CopyAttachmentFiles(_receiptSelection.GetPendingLocalPaths(), newReportId, createdFiles)
        Catch
            DeleteCreatedFiles(createdFiles)
            Throw
        End Try

        Dim attachmentPath As String = String.Join(";", copiedPaths)

        Dim report As New CreateReportDetailDto With {
            .ID = newReportId,
            .ReportDateFrom = DtpReportFrom.Value.Date,
            .ReportDateTo = DtpReportTo.Value.Date,
            .ReportDescription = TxtPurpose.Text.Trim(),
            .UserID = userId,
            .ReportStatus = String.Empty,
            .ReportEndorseStatus = GetInitialApprovalStatus(),
            .ReportDateFiled = DateTime.Now.ToString("yyyy-MM-dd"),
            .ReportFileStatus = GetInitialFileStatus(),
            .ReportPrintStatus = GetInitialPrintStatus(),
            .ReportNumberStatus = 0,
            .ReportAttachment = attachmentPath,
            .ReportType = CboReportType.SelectedItem.ToString(),
            .ERFReferenceNo = TxtERFReferenceNo.Text.Trim()
        }

        Try
            _reportDetailService.CreateReport(report, BuildCreateCashAdvanceDto(newReportId, userId), copiedPaths, userId)
        Catch
            DeleteCreatedFiles(createdFiles)
            Throw
        End Try

        _reportId = newReportId
    End Sub

    Private Sub UpdateReport()
        Dim userId As Integer = GetCurrentUserId()
        Dim updateMode As ScannedReceiptAttachmentUpdateMode = _receiptSelection.UpdateMode
        Dim copiedPaths As New List(Of String)()
        Dim createdFiles As New List(Of String)()

        If updateMode <> ScannedReceiptAttachmentUpdateMode.Unchanged Then
            Try
                copiedPaths = CopyAttachmentFiles(_receiptSelection.GetPendingLocalPaths(), _reportId, createdFiles)
            Catch
                DeleteCreatedFiles(createdFiles)
                Throw
            End Try
        End If

        Dim attachmentPath As String = _receiptSelection.BuildLegacyAttachmentValue(copiedPaths)

        Dim report As New UpdateReportDetailDto With {
            .ID = _reportId,
            .ReportDateFrom = DtpReportFrom.Value.Date,
            .ReportDateTo = DtpReportTo.Value.Date,
            .ReportDescription = TxtPurpose.Text.Trim(),
            .ReportAttachment = attachmentPath,
            .ReportType = CboReportType.SelectedItem.ToString(),
            .ERFReferenceNo = TxtERFReferenceNo.Text.Trim()
        }

        Try
            _reportDetailService.UpdateReport(
                report,
                BuildUpdateCashAdvanceDto(userId),
                copiedPaths,
                updateMode,
                userId)
        Catch
            DeleteCreatedFiles(createdFiles)
            Throw
        End Try
    End Sub

    Private Function BuildCreateCashAdvanceDto(reportId As String, userId As Integer) As CreateCashAdvanceDto
        Return New CreateCashAdvanceDto With {
            .ReportID = reportId,
            .emp_userID = userId,
            .CashAmount = If(IsLiquidation(), ParseDoubleOrZero(TxtAmount.Text), 0),
            .CashDate = If(IsCashAdvanceReport(), DtpCashDate.Value.ToString("MM-dd-yyyy"), String.Empty),
            .CashRefDoc = If(IsCashAdvanceReport(), TxtRefDoc.Text.Trim(), String.Empty),
            .CashRefNo = TxtReferenceNo.Text.Trim(),
            .BalanceTo = "EMPLOYEE",
            .RevolvingFund = If(IsReplenishment(), TxtRevolvingFund.Text.Trim(), String.Empty),
            .CashCheck = If(IsCashAdvanceReport(), "1", "0")
        }
    End Function

    Private Function BuildUpdateCashAdvanceDto(userId As Integer) As UpdateCashAdvanceDto
        Return New UpdateCashAdvanceDto With {
            .ReportID = _reportId,
            .emp_userID = userId,
            .CashAmount = If(IsLiquidation(), ParseDoubleOrZero(TxtAmount.Text), 0),
            .CashDate = If(IsCashAdvanceReport(), DtpCashDate.Value.ToString("MM-dd-yyyy"), String.Empty),
            .CashRefDoc = If(IsCashAdvanceReport(), TxtRefDoc.Text.Trim(), String.Empty),
            .CashRefNo = TxtReferenceNo.Text.Trim(),
            .BalanceTo = "EMPLOYEE",
            .RevolvingFund = If(IsReplenishment(), TxtRevolvingFund.Text.Trim(), String.Empty),
            .CashCheck = If(IsCashAdvanceReport(), "1", "0")
        }
    End Function

    Private Sub ApplyReportTypeState()
        Dim cashAdvanceReport As Boolean = IsCashAdvanceReport()
        Dim liquidation As Boolean = IsLiquidation()
        Dim replenishment As Boolean = IsReplenishment()
        Dim reimbursement As Boolean = IsReimbursement()

        GroupBoxCash.Enabled = Not reimbursement
        DtpCashDate.Enabled = cashAdvanceReport
        TxtRefDoc.Enabled = cashAdvanceReport
        TxtReferenceNo.Enabled = cashAdvanceReport
        TxtAmount.Enabled = liquidation
        TxtRevolvingFund.Enabled = replenishment

        If liquidation Then
            TxtRevolvingFund.Clear()
        ElseIf reimbursement Then
            TxtAmount.Clear()
            TxtRefDoc.Clear()
            TxtReferenceNo.Clear()
            TxtRevolvingFund.Clear()
        ElseIf replenishment Then
            TxtAmount.Clear()
            TxtRefDoc.Clear()
            TxtReferenceNo.Clear()
        End If
    End Sub

    Private Sub ApplyReportTypePurpose()
        If CboReportType.SelectedItem Is Nothing Then
            Return
        End If

        Dim selectedPurpose As String = CboReportType.SelectedItem.ToString()
        Dim currentPurpose As String = TxtPurpose.Text.Trim()

        If String.IsNullOrWhiteSpace(currentPurpose) OrElse
            String.Equals(currentPurpose, _lastAutoPurpose, StringComparison.OrdinalIgnoreCase) OrElse
            IsKnownReportType(currentPurpose) Then
            TxtPurpose.Text = selectedPurpose
            _lastAutoPurpose = selectedPurpose
            Return
        End If

        _lastAutoPurpose = selectedPurpose
    End Sub

    Private Sub SelectReportType(reportType As String)
        Dim normalizedReportType As String = NormalizeReportType(reportType)

        For Each item In CboReportType.Items
            If String.Equals(Convert.ToString(item), normalizedReportType, StringComparison.OrdinalIgnoreCase) Then
                CboReportType.SelectedItem = item
                Return
            End If
        Next

        CboReportType.SelectedItem = ReportTypeLiquidation
    End Sub

    Private Function ResolveReportType(report As ReportDetailDto, cashAdvance As CashAdvanceDto) As String
        Dim reportType As String = NormalizeReportType(report.ReportType)
        If Not String.IsNullOrWhiteSpace(reportType) Then
            Return reportType
        End If

        reportType = NormalizeReportType(report.ReportDescription)
        If Not String.IsNullOrWhiteSpace(reportType) Then
            Return reportType
        End If

        If cashAdvance IsNot Nothing Then
            If String.Equals(cashAdvance.CashCheck, "1", StringComparison.OrdinalIgnoreCase) Then
                Return ReportTypeLiquidation
            End If

            If Not String.IsNullOrWhiteSpace(cashAdvance.RevolvingFund) Then
                Return ReportTypeReplenishment
            End If
        End If

        Return ReportTypeLiquidation
    End Function

    Private Function ResolveErfReferenceNo(report As ReportDetailDto, cashAdvance As CashAdvanceDto) As String
        If report IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(report.ERFReferenceNo) Then
            Return report.ERFReferenceNo.Trim()
        End If

        If cashAdvance IsNot Nothing AndAlso IsGeneratedErfReference(cashAdvance.CashRefNo) Then
            Return cashAdvance.CashRefNo.Trim()
        End If

        Return GenerateReferenceNumber()
    End Function

    Private Function ResolveCashReferenceNo(report As ReportDetailDto, cashAdvance As CashAdvanceDto) As String
        If cashAdvance Is Nothing OrElse String.IsNullOrWhiteSpace(cashAdvance.CashRefNo) Then
            Return String.Empty
        End If

        If String.IsNullOrWhiteSpace(report.ERFReferenceNo) AndAlso IsGeneratedErfReference(cashAdvance.CashRefNo) Then
            Return String.Empty
        End If

        Return cashAdvance.CashRefNo.Trim()
    End Function

    Private Function IsGeneratedErfReference(value As String) As Boolean
        Return Not String.IsNullOrWhiteSpace(value) AndAlso value.Trim().StartsWith("ER-", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function NormalizeReportType(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then
            Return String.Empty
        End If

        Dim normalizedValue As String = value.Trim()

        If normalizedValue.IndexOf("reimburs", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
            normalizedValue.IndexOf("reimburse", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return ReportTypeReimbursement
        End If

        If normalizedValue.IndexOf("replen", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
            normalizedValue.IndexOf("replesh", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
            normalizedValue.IndexOf("revolving", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return ReportTypeReplenishment
        End If

        If normalizedValue.IndexOf("liquid", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
            normalizedValue.IndexOf("cash advance", StringComparison.OrdinalIgnoreCase) >= 0 Then
            Return ReportTypeLiquidation
        End If

        Return String.Empty
    End Function

    Private Function CopyAttachmentFiles(sourcePaths As IEnumerable(Of String),
                                         reportId As String,
                                         createdFiles As IList(Of String)) As List(Of String)
        Dim validatedPaths As List(Of String) = ValidateSelectedAttachmentFiles(sourcePaths)
        Dim copiedPaths As New List(Of String)()
        Dim targetDirectory As String = Path.Combine(Application.StartupPath, ScannedReceiptsFolderName)

        Try
            Directory.CreateDirectory(targetDirectory)

            For Each sourcePath As String In validatedPaths
                If IsPathInDirectory(sourcePath, targetDirectory) Then
                    copiedPaths.Add(Path.GetFullPath(sourcePath))
                    Continue For
                End If

                Dim destinationPath As String = BuildAttachmentDestinationPath(sourcePath, targetDirectory, reportId)
                File.Copy(sourcePath, destinationPath, False)
                copiedPaths.Add(destinationPath)
                createdFiles.Add(destinationPath)
            Next
        Catch ex As InvalidOperationException
            Throw
        Catch ex As Exception
            Throw New InvalidOperationException("Unable to copy scanned receipt attachment. " & ex.Message, ex)
        End Try

        Return copiedPaths
    End Function

    Private Function ValidateSelectedAttachmentFiles(sourcePaths As IEnumerable(Of String)) As List(Of String)
        Dim validatedPaths As New List(Of String)()
        Dim pathKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim aggregateFileSize As Long

        If sourcePaths Is Nothing Then
            Return validatedPaths
        End If

        For Each path As String In sourcePaths
            Dim sourcePath As String = If(path, String.Empty).Trim()
            If sourcePath.Length = 0 Then
                Continue For
            End If

            Dim fullPath As String
            Try
                fullPath = IO.Path.GetFullPath(sourcePath)
            Catch ex As Exception
                Throw New InvalidOperationException("Receipt file path is invalid: " & sourcePath, ex)
            End Try

            If Not pathKeys.Add(fullPath) Then
                Continue For
            End If

            If Not File.Exists(fullPath) Then
                Throw New InvalidOperationException("Receipt file was not found: " & fullPath)
            End If

            Dim extension As String = IO.Path.GetExtension(fullPath)
            If Not IsSupportedReceiptExtension(extension) Then
                Throw New InvalidOperationException("Unsupported receipt file type: " & extension & ". Select a PDF, JPG, JPEG, or PNG file.")
            End If

            Try
                Using stream As New FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    If stream.Length = 0 Then
                        Throw New InvalidOperationException("Receipt file is empty: " & fullPath)
                    End If

                    If Long.MaxValue - aggregateFileSize < stream.Length Then
                        Throw New InvalidOperationException("The combined receipt file size is too large to process.")
                    End If

                    aggregateFileSize += stream.Length
                End Using
            Catch ex As InvalidOperationException
                Throw
            Catch ex As Exception
                Throw New InvalidOperationException("Receipt file could not be read: " & fullPath, ex)
            End Try

            validatedPaths.Add(fullPath)
        Next

        Return validatedPaths
    End Function

    Private Shared Function IsSupportedReceiptExtension(extension As String) As Boolean
        Return String.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Sub DeleteCreatedFiles(createdFiles As IEnumerable(Of String))
        If createdFiles Is Nothing Then
            Return
        End If

        For Each filePath As String In createdFiles
            Try
                If File.Exists(filePath) Then
                    File.Delete(filePath)
                End If
            Catch ex As IOException
                Debug.WriteLine("Unable to roll back copied receipt file: " & ex.Message)
            Catch ex As UnauthorizedAccessException
                Debug.WriteLine("Unable to roll back copied receipt file: " & ex.Message)
            End Try
        Next
    End Sub

    Private Sub RefreshAttachmentDisplay()
        If _receiptSelection Is Nothing Then
            TxtAttachment.Clear()
            Return
        End If

        TxtAttachment.Text = _receiptSelection.BuildDisplayText()
    End Sub

    Private Sub ApplyAttachmentState()
        TxtAttachment.ReadOnly = True
        Dim isReadOnly As Boolean = _receiptSelection IsNot Nothing AndAlso _receiptSelection.IsReadOnly
        BtnBrowseAttachment.Enabled = Not isReadOnly
        BtnClearAttachment.Enabled = Not isReadOnly
        GroupBoxAttachment.Text = If(isReadOnly, "Scanned Receipts (Read-only after approval)", "Scanned Receipts")
    End Sub

    Private Shared Function IsFinalApprovedReport(report As ReportDetailDto) As Boolean
        Return report IsNot Nothing AndAlso
            String.Equals(report.ReportEndorseStatus, "APPROVED", StringComparison.OrdinalIgnoreCase) AndAlso
            String.Equals(report.ReportFileStatus, "0", StringComparison.Ordinal) AndAlso
            String.Equals(report.ReportPrintStatus, "0", StringComparison.Ordinal)
    End Function

    Private Function BuildAttachmentDestinationPath(sourcePath As String, targetDirectory As String, reportId As String) As String
        Dim fileNameWithoutExtension As String = Path.GetFileNameWithoutExtension(sourcePath)
        Dim extension As String = Path.GetExtension(sourcePath)
        Dim destinationPath As String = Path.Combine(targetDirectory, Path.GetFileName(sourcePath))

        If Not File.Exists(destinationPath) Then
            Return destinationPath
        End If

        Dim safeReportId As String = reportId.Replace("-"c, "_"c)
        Dim counter As Integer = 1

        Do
            Dim uniqueFileName As String = String.Format("{0}_{1}_{2}{3}", fileNameWithoutExtension, safeReportId, counter, extension)
            destinationPath = Path.Combine(targetDirectory, uniqueFileName)
            counter += 1
        Loop While File.Exists(destinationPath)

        Return destinationPath
    End Function

    Private Function IsPathInDirectory(filePath As String, directoryPath As String) As Boolean
        Dim fullFilePath As String = Path.GetFullPath(filePath)
        Dim fullDirectoryPath As String = Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) & Path.DirectorySeparatorChar

        Return fullFilePath.StartsWith(fullDirectoryPath, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsKnownReportType(value As String) As Boolean
        Return String.Equals(value, ReportTypeReplenishment, StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(value, ReportTypeLiquidation, StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(value, ReportTypeReimbursement, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsLiquidation() As Boolean
        Return String.Equals(Convert.ToString(CboReportType.SelectedItem), ReportTypeLiquidation, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsReplenishment() As Boolean
        Return String.Equals(Convert.ToString(CboReportType.SelectedItem), ReportTypeReplenishment, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsReimbursement() As Boolean
        Return String.Equals(Convert.ToString(CboReportType.SelectedItem), ReportTypeReimbursement, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsCashAdvanceReport() As Boolean
        Return IsLiquidation()
    End Function

    Private Function GenerateReferenceNumber() As String
        Return "ER-" & GetCurrentUserId().ToString() & DateTime.Now.ToString("yyyyMMdd-HHmmss")
    End Function

    Private Function GetCurrentUserId() As Integer
        Dim value As String = GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0)
        Dim userId As Integer
        If Integer.TryParse(value, userId) Then
            Return userId
        End If

        Return 0
    End Function

    Private Function GetInitialApprovalStatus() As String
        If String.Equals(GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0), "Admin", StringComparison.OrdinalIgnoreCase) Then
            Return "APPROVED"
        End If

        Return "NOT APPROVED"
    End Function

    Private Function GetInitialFileStatus() As String
        If String.Equals(GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0), "Admin", StringComparison.OrdinalIgnoreCase) Then
            Return "0"
        End If

        Return String.Empty
    End Function

    Private Function GetInitialPrintStatus() As String
        Return If(String.Equals(GetInitialApprovalStatus(), "APPROVED", StringComparison.OrdinalIgnoreCase), "0", "1")
    End Function

    Private Function ParseNullableDouble(value As String) As Nullable(Of Double)
        If String.IsNullOrWhiteSpace(value) Then
            Return Nothing
        End If

        Dim parsed As Double
        If Double.TryParse(value, parsed) Then
            Return parsed
        End If

        Return Nothing
    End Function

    Private Function ParseDoubleOrZero(value As String) As Double
        Dim parsed As Double
        If Double.TryParse(value, parsed) Then
            Return parsed
        End If

        Return 0
    End Function

    Private Function ParseDateOrToday(value As String) As DateTime
        Dim parsed As DateTime
        If DateTime.TryParse(value, parsed) Then
            Return parsed
        End If

        Return DateTime.Now
    End Function

    Private Sub FrmEReportDetailsV2_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        _selectedReportContextService.Clear()
    End Sub
End Class
