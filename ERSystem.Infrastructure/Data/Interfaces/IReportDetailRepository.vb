Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IReportDetailRepository
        Function GetAll() As List(Of ReportDetailDto)
        Function GetById(reportId As String) As ReportDetailDto
    End Interface
End Namespace
