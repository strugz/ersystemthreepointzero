Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IReportDetailRepository
        Function GetAll() As List(Of ReportDetailDto)
        Function GetById(reportId As String) As ReportDetailDto
        Function Create(report As CreateReportDetailDto) As ReportDetailDto
        Function Create(report As CreateReportDetailDto, dbContext As AppDbContext) As ReportDetailDto
        Sub Update(report As UpdateReportDetailDto)
    End Interface
End Namespace
