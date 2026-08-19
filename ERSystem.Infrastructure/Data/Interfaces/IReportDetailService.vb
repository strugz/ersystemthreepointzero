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
        Sub UpdateReport(report As UpdateReportDetailDto,
                         cashAdvance As UpdateCashAdvanceDto,
                         scannedReceiptPaths As IEnumerable(Of String),
                         attachmentUpdateMode As ScannedReceiptAttachmentUpdateMode,
                         createdByUserId As Nullable(Of Integer))
    End Interface
End Namespace
