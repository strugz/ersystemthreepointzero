Imports System.Data

Public NotInheritable Class ExpenseLegacyStateGateway
    Private ReadOnly _loadData As ClsLoadData

    Public Sub New()
        Me.New(New ClsLoadData())
    End Sub

    Public Sub New(loadData As ClsLoadData)
        If loadData Is Nothing Then
            Throw New ArgumentNullException("loadData")
        End If

        _loadData = loadData
    End Sub

    Public Function LoadReportSettings() As String()
        Return _loadData.GetEReportDetails(Application.StartupPath + "\settings.txt")
    End Function

    Public Function LoadExpenseSettings() As String()
        Return _loadData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
    End Function

    Public Function GetMealPayload() As String
        Return _loadData.GetMeal()
    End Function

    Public Function GetTransportationPayload() As String
        Return _loadData.GetTranspo()
    End Function

    Public Function GetCurrentUserId() As String
        Return Convert.ToString(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
    End Function

    Public Function LoadExpenseReport(reportId As String, userId As String) As DataTable
        Return LoadingExpenseReport(reportId, userId)
    End Function

    Public Sub ClearExpenseEntry(clearWorkContext As Boolean)
        If clearWorkContext Then
            ModDataStore.ClearAllExpenseData()
        Else
            ModDataStore.clearExpenseData()
        End If
    End Sub

    Public Sub DeleteExpenseSettings()
        _loadData.DeleteEReportDetails(Application.StartupPath + "\expenseSettings.txt")
    End Sub

    Public Sub SaveWorkContext(workWith As String, location As String, instrument As String, serialNumber As String, serviceNumber As String)
        modReuse.SetTextFile(workWith, location, instrument, serialNumber, serviceNumber)
    End Sub
End Class
