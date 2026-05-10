Imports ERSystem.Core.Domain.Entities

Namespace Application.Interfaces
    Public Interface IReportService
        Sub AddReport(report As Report)
        Sub UpdateReport(report As Report)
        Sub RefileReport(reportID As String, status As String)
    End Interface
End Namespace