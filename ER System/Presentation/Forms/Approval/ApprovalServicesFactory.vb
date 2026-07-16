Option Strict On

Imports System.IO
Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data

Friend NotInheritable Class ApprovalServicesFactory
    Private Shared ReadOnly ApproveRepository As IApproveRepository = New ApproveRepository()
    Private Shared ReadOnly ApproveActionRepository As IApproveActionRepository = New ApproveActionRepository()
    Private Shared ReadOnly RejectActionRepository As IRejectActionRepository = New RejectActionRepository()
    Private Shared ReadOnly SettingsRegistryProvider As New SettingsRegistryProvider()
    Private Shared ReadOnly UserAccountRegistryProvider As New UserAccountRegistryProvider()
    Private Shared ReadOnly SelectedReportContextStore As ERSystem.AppServices.ISelectedReportContextStore = New LegacySelectedReportContextStore()

    Private Sub New()
    End Sub

    Public Shared Function CreateApproveService() As ERSystem.AppServices.ApproveService
        Return New ERSystem.AppServices.ApproveService(
            ApproveRepository,
            SettingsRegistryProvider,
            UserAccountRegistryProvider)
    End Function

    Public Shared Function CreateApproveActionService() As ERSystem.AppServices.ApproveActionService
        Dim approveService As ERSystem.AppServices.ApproveService = CreateApproveService()
        Dim financeReviewService As IFinanceReviewService = New FinanceReviewService()
        Dim cleanupService As New ERSystem.AppServices.ScannedReceiptCleanupService(
            New ReportDetailService(),
            financeReviewService,
            Path.Combine(Application.StartupPath, "ScannedReceipts"))

        Return New ERSystem.AppServices.ApproveActionService(
            ApproveActionRepository,
            approveService,
            UserAccountRegistryProvider,
            financeReviewService,
            cleanupService)
    End Function

    Public Shared Function CreateSelectionContextService() As ERSystem.AppServices.ApproveSelectionContextService
        Return New ERSystem.AppServices.ApproveSelectionContextService(
            SettingsRegistryProvider,
            SelectedReportContextStore)
    End Function

    Public Shared Function CreateRejectActionService() As ERSystem.AppServices.RejectActionService
        Return New ERSystem.AppServices.RejectActionService(
            RejectActionRepository,
            CreateApproveService(),
            UserAccountRegistryProvider)
    End Function

    Public Shared Function CreateSelectedReportContextStore() As ERSystem.AppServices.ISelectedReportContextStore
        Return SelectedReportContextStore
    End Function
End Class
