Option Strict On

Namespace Infrastructure.Configuration
    Friend Class SelectedReportContextProvider
        Private Const SettingsFileName As String = "settings.txt"
        Private ReadOnly _loader As ClsLoadData

        Public Sub New()
            _loader = New ClsLoadData()
        End Sub

        Public Function Load() As Domain.Entities.SelectedReportContext
            Dim path As String = System.Windows.Forms.Application.StartupPath + "\" + SettingsFileName

            If Not String.Equals(_loader.TempFileValidation(path).ToString(), Boolean.TrueString, StringComparison.OrdinalIgnoreCase) Then
                Return New Domain.Entities.SelectedReportContext()
            End If

            Return New Domain.Entities.SelectedReportContext With {
                .Values = _loader.GetEReportDetails(path)
            }
        End Function

        Public Sub Save(reportId As String)
            If String.IsNullOrWhiteSpace(reportId) Then
                Throw New ArgumentException("Report ID is required.", NameOf(reportId))
            End If

            _loader.DeleteEReportDetails(System.Windows.Forms.Application.StartupPath + "\" + SettingsFileName)
            _loader.SetEReportDetails(reportId)
        End Sub

        Public Sub Clear()
            _loader.DeleteEReportDetails(System.Windows.Forms.Application.StartupPath + "\" + SettingsFileName)
        End Sub
    End Class
End Namespace
