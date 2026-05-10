Imports ERSystem.Core.Domain.Entities
Imports ERSystem.Core.Domain.Interfaces
Imports ERSystem.Core.Application.Interfaces

Namespace Application.Services
    Public Class ReportService
        Implements IReportService

        Private ReadOnly _repository As IReportRepository

        Public Sub New(repository As IReportRepository)
            _repository = repository
        End Sub

        Public Sub AddReport(report As Report) Implements IReportService.AddReport
            _repository.AddReport(report)
        End Sub

        Public Sub UpdateReport(report As Report) Implements IReportService.UpdateReport
            _repository.UpdateReport(report)
        End Sub

        Public Sub RefileReport(reportID As String, status As String) Implements IReportService.RefileReport
            _repository.RefileReport(reportID, status)
        End Sub
    End Class
End Namespace
