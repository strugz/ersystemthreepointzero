Option Strict On

Friend Class LegacySelectedReportContextStore
    Implements ERSystem.AppServices.ISelectedReportContextStore

    Private Const SettingsFileName As String = "settings.txt"
    Private ReadOnly _loader As ClsLoadData

    Public Sub New()
        _loader = New ClsLoadData()
    End Sub

    Public Sub Save(reportId As String) Implements ERSystem.AppServices.ISelectedReportContextStore.Save
        _loader.DeleteEReportDetails(System.Windows.Forms.Application.StartupPath + "\" + SettingsFileName)
        _loader.SetEReportDetails(reportId)
    End Sub

    Public Function LoadValues() As String() Implements ERSystem.AppServices.ISelectedReportContextStore.LoadValues
        Return _loader.GetEReportDetails(System.Windows.Forms.Application.StartupPath + "\" + SettingsFileName)
    End Function
End Class
