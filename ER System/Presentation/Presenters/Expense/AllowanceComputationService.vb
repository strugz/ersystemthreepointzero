Public NotInheritable Class AllowanceComputationService
    Public Function Build(totalDays As String, minusDays As String, amount As String) As AllowanceComputationResult
        Dim normalizedMinusDays As String = If(String.IsNullOrWhiteSpace(minusDays), "0", minusDays)
        Dim multiplier As Double = Val(totalDays) - Val(normalizedMinusDays)
        Dim computationText As String

        If normalizedMinusDays <> "0" Then
            computationText = " (" & totalDays & "Days - " & normalizedMinusDays & "Days) * " & amount
        Else
            computationText = " (" & totalDays & "Days) * " & amount
        End If

        Return New AllowanceComputationResult With {
            .TotalDays = totalDays,
            .MinusDays = normalizedMinusDays,
            .Multiplier = multiplier.ToString(),
            .ComputationText = computationText
        }
    End Function
End Class
