Option Strict On

Imports ERSystem.Domain.Approval

Public Interface IApprovalValidationService
    Function Validate(userId As String, signId As String, reportId As String) As ApprovalValidationStatus
End Interface
