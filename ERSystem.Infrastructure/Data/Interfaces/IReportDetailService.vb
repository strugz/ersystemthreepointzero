Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IReportDetailService
        Function GetAll() As List(Of ReportDetailDto)
        Function GetById(reportId As String) As ReportDetailDto
        Function Create(report As CreateReportDetailDto) As ReportDetailDto
        Function CreateReport(report As CreateReportDetailDto, cashAdvance As CreateCashAdvanceDto) As ReportDetailDto
        Function CreateReport(report As CreateReportDetailDto,
                              cashAdvance As CreateCashAdvanceDto,
                              scannedReceiptPaths As IEnumerable(Of String),
                              createdByUserId As Nullable(Of Integer)) As ReportDetailDto
        Sub Update(report As UpdateReportDetailDto)
    End Interface
End Namespace
