Option Strict On

Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IScannedReceiptAttachmentRepository
        Sub ReplaceForReport(reportId As String,
                             localPaths As IEnumerable(Of String),
                             createdByUserId As Nullable(Of Integer),
                             dbContext As AppDbContext)

        Sub DeleteForReport(reportId As String, dbContext As AppDbContext)

        Function GetMetadataByReportId(reportId As String) As List(Of ScannedReceiptAttachmentMetadataDto)

        Function GetById(receiptAttachmentId As Integer) As ScannedReceiptAttachmentDto
    End Interface
End Namespace
