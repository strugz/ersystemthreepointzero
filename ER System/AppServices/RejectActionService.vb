Option Strict On

Namespace AppServices
    Friend Class RejectActionService
        Private ReadOnly _rejectActionRepository As Infrastructure.Data.Repositories.RejectActionRepository
        Private ReadOnly _approveService As ApproveService

        Public Sub New()
            _rejectActionRepository = New Infrastructure.Data.Repositories.RejectActionRepository()
            _approveService = New ApproveService()
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
End Namespace
