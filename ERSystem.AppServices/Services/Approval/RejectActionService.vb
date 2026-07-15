Option Strict On

Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data

Public Class RejectActionService
    Private ReadOnly _rejectActionRepository As IRejectActionRepository
    Private ReadOnly _approveService As ApproveService
    Private ReadOnly _userAccountRegistryProvider As UserAccountRegistryProvider

    Public Sub New(rejectActionRepository As IRejectActionRepository,
                   approveService As ApproveService,
                   userAccountRegistryProvider As UserAccountRegistryProvider)
        If rejectActionRepository Is Nothing Then
            Throw New ArgumentNullException("rejectActionRepository")
        End If

        If approveService Is Nothing Then
            Throw New ArgumentNullException("approveService")
        End If

        If userAccountRegistryProvider Is Nothing Then
            Throw New ArgumentNullException("userAccountRegistryProvider")
        End If

        _rejectActionRepository = rejectActionRepository
        _approveService = approveService
        _userAccountRegistryProvider = userAccountRegistryProvider
    End Sub

    Public Function RejectReport(reportId As String, reportUserId As String, rejectNote As String) As RejectActionResult
        If String.IsNullOrWhiteSpace(reportId) Then
            Return New RejectActionResult With {
                .IsSuccess = False,
                .Message = "No Report Selected"
            }
        End If

        _rejectActionRepository.RejectFiledReport(reportId, rejectNote, _userAccountRegistryProvider.GetValue("UserID"))

        Return New RejectActionResult With {
            .IsSuccess = True,
            .Message = String.Empty,
            .ReloadResult = _approveService.ReloadAfterReject(reportUserId)
        }
    End Function
End Class
