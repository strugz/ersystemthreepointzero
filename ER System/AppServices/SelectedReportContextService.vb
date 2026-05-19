Option Strict On

Namespace AppServices
    Friend Class SelectedReportContextService
        Private ReadOnly _selectedReportContextProvider As Infrastructure.Configuration.SelectedReportContextProvider

        Public Sub New()
            _selectedReportContextProvider = New Infrastructure.Configuration.SelectedReportContextProvider()
        End Sub

        Public Function Load() As Domain.Entities.SelectedReportContext
            Return _selectedReportContextProvider.Load()
        End Function

        Public Sub Save(reportId As String)
            _selectedReportContextProvider.Save(reportId)
        End Sub

        Public Sub Clear()
            _selectedReportContextProvider.Clear()
        End Sub
    End Class
End Namespace
