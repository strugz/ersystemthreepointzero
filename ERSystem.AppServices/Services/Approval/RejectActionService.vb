Option Strict On

Imports ERSystem.Infrastructure.Data

Public Class RejectActionService
    Private ReadOnly _rejectActionRepository As IRejectActionRepository
    Private ReadOnly _approveService As ApproveService

    Public Sub New(rejectActionRepository As IRejectActionRepository,
                   approveService As ApproveService)
        If rejectActionRepository Is Nothing Then
            Throw New ArgumentNullException("rejectActionRepository")
        End If

        If approveService Is Nothing Then
            Throw New ArgumentNullException("approveService")
        End If

        _rejectActionRepository = rejectActionRepository
        _approveService = approveService
    End Sub

    Public Function RejectReport(reportId As String, reportUserId As String, rejectNote As String) As RejectActionResult
        If String.IsNullOrWhiteSpace(reportId) Then
            Return New RejectActionResult With {
                .IsSuccess = False,
                .Message = "No Report Selected"
            }
        End If

        _rejectActionRepository.RejectFiledReport(reportId, rejectNote)

        Return New RejectActionResult With {
            .IsSuccess = True,
            .Message = String.Empty,
            .ReloadResult = _approveService.ReloadAfterReject(reportUserId)
        }
    End Function
End Class
