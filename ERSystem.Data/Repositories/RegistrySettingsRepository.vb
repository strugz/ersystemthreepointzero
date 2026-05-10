Imports System.Collections.Generic
Imports Microsoft.Win32
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class RegistrySettingsRepository
        Implements ISettingsRepository

        Private ReadOnly _basePath As String

        Public Sub New(basePath As String)
            _basePath = basePath ' e.g., "HKEY_CURRENT_USER\Software\ER System"
        End Sub

        Public Function GetValues(subKey As String, valueNames As String()) As List(Of String) Implements ISettingsRepository.GetValues
            Dim results As New List(Of String)()
            Dim fullPath = IO.Path.Combine(_basePath, subKey)

            For Each valueName In valueNames
                Dim value As Object = Registry.GetValue(fullPath, valueName, String.Empty)
                If value IsNot Nothing Then
                    results.Add(value.ToString())
                Else
                    results.Add(String.Empty)
                End If
            Next
            Return results
        End Function

        Public Sub SetValues(subKey As String, valueNames As String(), values As String()) Implements ISettingsRepository.SetValues
            Dim fullPath = IO.Path.Combine(_basePath, subKey)
            For i As Integer = 0 To Math.Min(valueNames.Length, values.Length) - 1
                Registry.SetValue(fullPath, valueNames(i), values(i))
            Next
        End Sub

        Public Sub SetValue(subKey As String, valueName As String, value As String) Implements ISettingsRepository.SetValue
            Dim fullPath = IO.Path.Combine(_basePath, subKey)
            Registry.SetValue(fullPath, valueName, value)
        End Sub
    End Class
End Namespace