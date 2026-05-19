Option Strict On

Namespace AppServices
    Friend Class ReportViewerContextService
        Private Const EncryptionKey As String = "crimsonmonastery2003"

        Private ReadOnly _selectedReportContextProvider As Infrastructure.Configuration.SelectedReportContextProvider
        Private ReadOnly _connectionSettingsProvider As Infrastructure.Configuration.RegistryConnectionSettingsProvider
        Private ReadOnly _userAccountRegistryProvider As Infrastructure.Configuration.UserAccountRegistryProvider
        Private ReadOnly _encryption As clsEncryption
        Private ReadOnly _loader As ClsLoadData

        Public Sub New()
            _selectedReportContextProvider = New Infrastructure.Configuration.SelectedReportContextProvider()
            _connectionSettingsProvider = New Infrastructure.Configuration.RegistryConnectionSettingsProvider()
            _userAccountRegistryProvider = New Infrastructure.Configuration.UserAccountRegistryProvider()
            _encryption = New clsEncryption(EncryptionKey)
            _loader = New ClsLoadData()
        End Sub

        Public Function Load() As ReportViewerContextResult
            Dim selectedReport As Domain.Entities.SelectedReportContext = _selectedReportContextProvider.Load()

            If Not selectedReport.HasSelection Then
                Return New ReportViewerContextResult()
            End If

            Dim currentUserId As String = _userAccountRegistryProvider.GetValue("UserID")
            Dim userLevel As String = _userAccountRegistryProvider.GetValue("UserLevel")
            Dim connectionSettings As Infrastructure.Configuration.ConnectionSettings = _connectionSettingsProvider.Load()
            Dim isAdminViewingOwnReport As Boolean = String.Equals(userLevel, "Admin", StringComparison.OrdinalIgnoreCase) AndAlso
                                                   String.Equals(selectedReport.UserId, currentUserId, StringComparison.OrdinalIgnoreCase)
            Dim reportDocument As ReportDocument = _loader.MyReportDocument(
                System.Windows.Forms.Application.StartupPath & "\ER Report.rpt",
                connectionSettings.UserName,
                connectionSettings.Password,
                {"@UserID", "@reportID"},
                {selectedReport.UserId, selectedReport.ReportId})

            Return New ReportViewerContextResult With {
                .HasSelection = True,
                .ReportId = selectedReport.ReportId,
                .ReportUserId = selectedReport.UserId,
                .Status = selectedReport.Status,
                .PrintStatus = selectedReport.PrintStatus,
                .Description = selectedReport.Description,
                .IsAdminViewingOwnReport = isAdminViewingOwnReport,
                .CanSendPrint = isAdminViewingOwnReport,
                .ViewerReport = reportDocument
            }
        End Function
    End Class
End Namespace
