Imports System.Collections.Generic

Namespace Domain.Interfaces
    Public Interface ISettingsRepository
        Function GetValues(subKey As String, valueNames As String()) As List(Of String)
        Sub SetValues(subKey As String, valueNames As String(), values As String())
        Sub SetValue(subKey As String, valueName As String, value As String)
    End Interface
End Namespace