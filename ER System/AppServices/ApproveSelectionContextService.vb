Option Strict On

Namespace AppServices
    Friend Class ApproveSelectionContextService
        Private Const SettingsFileName As String = "settings.txt"

        Private ReadOnly _settingsRegistryProvider As Infrastructure.Configuration.SettingsRegistryProvider
        Private ReadOnly _loader As ClsLoadData

        Public Sub New()
            _settingsRegistryProvider = New Infrastructure.Configuration.SettingsRegistryProvider()
            _loader = New ClsLoadData()
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

            _loader.DeleteEReportDetails(System.Windows.Forms.Application.StartupPath + "\" + SettingsFileName)
            _loader.SetEReportDetails(reportId)

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
End Namespace
