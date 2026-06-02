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
            dialog.Filter = "Receipt Files|*.pdf;*.jpg;*.jpeg;*.png|PDF Files|*.pdf|Image Files|*.jpg;*.jpeg;*.png|All Files|*.*"
            dialog.Multiselect = True
            dialog.Title = "Select scanned receipt attachment"

            If dialog.ShowDialog(Me) = DialogResult.OK Then
                TxtAttachment.Text = AppendAttachmentPaths(TxtAttachment.Text, dialog.FileNames)
            End If
        End Using
    End Sub

    Private Sub BtnClearAttachment_Click(sender As Object, e As EventArgs) Handles BtnClearAttachment.Click
        TxtAttachment.Clear()
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
        TxtAttachment.Text = If(report.ReportAttachment, String.Empty)
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
        TxtAttachment.Clear()
        TxtERFReferenceNo.Text = GenerateReferenceNumber()
        CboReportType.SelectedIndex = 0
        BtnSave.Text = "Save"
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
        Dim attachmentPath As String = NormalizeAttachmentPaths(newReportId)

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

        _reportDetailService.CreateReport(report, BuildCreateCashAdvanceDto(newReportId, userId), SplitAttachmentPaths(attachmentPath), userId)
        _reportId = newReportId
    End Sub

    Private Sub UpdateReport()
        Dim userId As Integer = GetCurrentUserId()
        Dim attachmentPath As String = NormalizeAttachmentPaths(_reportId)

        _reportDetailService.Update(New UpdateReportDetailDto With {
            .ID = _reportId,
            .ReportDateFrom = DtpReportFrom.Value.Date,
            .ReportDateTo = DtpReportTo.Value.Date,
            .ReportDescription = TxtPurpose.Text.Trim(),
            .ReportAttachment = attachmentPath,
            .ReportType = CboReportType.SelectedItem.ToString(),
            .ERFReferenceNo = TxtERFReferenceNo.Text.Trim()
        })

        _cashAdvanceService.UpdateByReportId(_reportId, BuildUpdateCashAdvanceDto(userId))
        _scannedReceiptAttachmentService.ReplaceForReport(New SaveScannedReceiptAttachmentRequest With {
            .ReportID = _reportId,
            .LocalPaths = SplitAttachmentPaths(attachmentPath),
            .CreatedByUserID = userId
        })
    End Sub

    Private Shared Function SplitAttachmentPaths(attachmentPath As String) As IEnumerable(Of String)
        If String.IsNullOrWhiteSpace(attachmentPath) Then
            Return Enumerable.Empty(Of String)()
        End If

        Return attachmentPath.Split(";"c).
            Select(Function(path) path.Trim()).
            Where(Function(path) path.Length > 0).
            ToList()
    End Function

    Private Shared Function AppendAttachmentPaths(existingAttachmentPath As String, selectedPaths As IEnumerable(Of String)) As String
        Dim attachmentPaths As New List(Of String)()
        Dim pathKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        AddUniqueAttachmentPaths(attachmentPaths, pathKeys, SplitAttachmentPaths(existingAttachmentPath))
        AddUniqueAttachmentPaths(attachmentPaths, pathKeys, selectedPaths)

        Return String.Join(";", attachmentPaths)
    End Function

    Private Shared Sub AddUniqueAttachmentPaths(attachmentPaths As IList(Of String),
                                                pathKeys As ISet(Of String),
                                                sourcePaths As IEnumerable(Of String))
        If sourcePaths Is Nothing Then
            Return
        End If

        For Each sourcePath As String In sourcePaths
            Dim normalizedPath As String = If(sourcePath, String.Empty).Trim()

            If normalizedPath.Length = 0 Then
                Continue For
            End If

            Dim pathKey As String = BuildAttachmentPathKey(normalizedPath)

            If pathKeys.Add(pathKey) Then
                attachmentPaths.Add(normalizedPath)
            End If
        Next
    End Sub

    Private Shared Function BuildAttachmentPathKey(attachmentPath As String) As String
        Try
            Return Path.GetFullPath(attachmentPath)
        Catch ex As Exception
            Return attachmentPath.Trim()
        End Try
    End Function

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

    Private Function NormalizeAttachmentPaths(reportId As String) As String
        If String.IsNullOrWhiteSpace(TxtAttachment.Text) Then
            Return String.Empty
        End If

        Dim copiedPaths As New List(Of String)()
        Dim targetDirectory As String = Path.Combine(Application.StartupPath, ScannedReceiptsFolderName)

        Try
            Directory.CreateDirectory(targetDirectory)

            For Each attachmentPath In TxtAttachment.Text.Split(";"c)
                Dim sourcePath As String = attachmentPath.Trim()
                If String.IsNullOrWhiteSpace(sourcePath) Then
                    Continue For
                End If

                If Not File.Exists(sourcePath) Then
                    Throw New InvalidOperationException("Attachment file was not found: " & sourcePath)
                End If

                If IsPathInDirectory(sourcePath, targetDirectory) Then
                    copiedPaths.Add(Path.GetFullPath(sourcePath))
                    Continue For
                End If

                Dim destinationPath As String = BuildAttachmentDestinationPath(sourcePath, targetDirectory, reportId)
                File.Copy(sourcePath, destinationPath, False)
                copiedPaths.Add(destinationPath)
            Next
        Catch ex As InvalidOperationException
            Throw
        Catch ex As Exception
            Throw New InvalidOperationException("Unable to copy scanned receipt attachment. " & ex.Message, ex)
        End Try

        Dim normalizedPaths As String = String.Join(";", copiedPaths)
        TxtAttachment.Text = normalizedPaths
        Return normalizedPaths
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
