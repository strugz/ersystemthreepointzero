Option Strict On

Imports ERSystem.Domain.Approval

Friend Class LegacyApprovalValidationService
    Implements ERSystem.AppServices.IApprovalValidationService

    Private ReadOnly _loader As ClsLoadData

    Public Sub New()
        _loader = New ClsLoadData()
    End Sub

    Public Function Validate(userId As String, signId As String, reportId As String) As ApprovalValidationStatus Implements ERSystem.AppServices.IApprovalValidationService.Validate
        Dim legacyResult As String = _loader.ApproverValidation(userId, signId, reportId)

        If String.Equals(legacyResult, "True", StringComparison.Ordinal) Then
            Return ApprovalValidationStatus.CanApprove
        End If

        If String.Equals(legacyResult, "Done", StringComparison.Ordinal) Then
            Return ApprovalValidationStatus.AlreadyConfirmed
        End If

        Return ApprovalValidationStatus.WaitingForPreviousApprover
    End Function
End Class
