Option Strict On

Imports ERSystem.Infrastructure.Configuration

Public Class ApproveSelectionContextService
    Private ReadOnly _settingsRegistryProvider As SettingsRegistryProvider
    Private ReadOnly _selectedReportContextStore As ISelectedReportContextStore

    Public Sub New(settingsRegistryProvider As SettingsRegistryProvider,
                   selectedReportContextStore As ISelectedReportContextStore)
        If settingsRegistryProvider Is Nothing Then
            Throw New ArgumentNullException("settingsRegistryProvider")
        End If

        If selectedReportContextStore Is Nothing Then
            Throw New ArgumentNullException("selectedReportContextStore")
        End If

        _settingsRegistryProvider = settingsRegistryProvider
        _selectedReportContextStore = selectedReportContextStore
    End Sub

    Public Function PrepareSelectedReportContext(reportId As String) As ApproveSelectionContextResult
        If String.IsNullOrWhiteSpace(reportId) Then
            Return New ApproveSelectionContextResult With {
                .HasSelection = False,
                .ShouldEnableActionButtons = False,
                .ShouldShowContextMenu = False,
                .DelayMilliseconds = 0
            }
        End If

        _selectedReportContextStore.Save(reportId)

        Return New ApproveSelectionContextResult With {
            .HasSelection = True,
            .ShouldEnableActionButtons = True,
            .ShouldShowContextMenu = False,
            .DelayMilliseconds = 500
        }
    End Function

    Public Function PrepareContextMenuSelection(reportId As String) As ApproveSelectionContextResult
        Dim result As ApproveSelectionContextResult = PrepareSelectedReportContext(reportId)
        result.ShouldShowContextMenu = result.HasSelection
        Return result
    End Function

    Public Sub SetApproverEditMode()
        _settingsRegistryProvider.Save({"Approver"}, {"1"})
    End Sub
End Class
