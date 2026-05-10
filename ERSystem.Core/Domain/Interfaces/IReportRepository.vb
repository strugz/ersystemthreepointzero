Imports ERSystem.Core.Domain.Entities

Namespace Domain.Interfaces
    Public Interface IReportRepository
        Sub AddReport(report As Report)
        Sub UpdateReport(report As Report)
        Sub RefileReport(reportID As String, status As String)
    End Interface
End Namespace