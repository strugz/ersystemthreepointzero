Option Strict On

Imports System.Data.Entity
Imports System.IO
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ScannedReceiptAttachmentRepository
        Implements IScannedReceiptAttachmentRepository

        Private Shared ReadOnly ContentTypes As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
            {".pdf", "application/pdf"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".png", "image/png"}
        }

        Public Sub ReplaceForReport(reportId As String,
                                    localPaths As IEnumerable(Of String),
                                    createdByUserId As Nullable(Of Integer),
                                    dbContext As AppDbContext) Implements IScannedReceiptAttachmentRepository.ReplaceForReport
            If String.IsNullOrWhiteSpace(reportId) Then
                Throw New ArgumentException("Report ID is required.", "reportId")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            DeleteForReport(reportId, dbContext)

            If localPaths Is Nothing Then
                dbContext.SaveChanges()
                Return
            End If

            For Each localPath As String In localPaths
                Dim sourcePath As String = If(localPath, String.Empty).Trim()

                If sourcePath.Length = 0 Then
                    Continue For
                End If

                If Not File.Exists(sourcePath) Then
                    Throw New InvalidOperationException("Scanned receipt file was not found: " & sourcePath)
                End If

                Dim extension As String = Path.GetExtension(sourcePath)
                Dim contentType As String = Nothing

                If Not ContentTypes.TryGetValue(extension, contentType) Then
                    Throw New InvalidOperationException("Unsupported scanned receipt file type: " & extension)
                End If

                Dim receiptContent As Byte() = File.ReadAllBytes(sourcePath)

                dbContext.ScannedReceiptAttachments.Add(New ScannedReceiptAttachmentModel With {
                    .ReportID = reportId,
                    .OriginalFileName = Path.GetFileName(sourcePath),
                    .StoredFilePath = Path.GetFullPath(sourcePath),
                    .ContentType = contentType,
                    .FileExtension = extension.ToLowerInvariant(),
                    .FileSizeBytes = receiptContent.LongLength,
                    .ReceiptContent = receiptContent,
                    .CreatedByUserID = createdByUserId,
                    .CreatedDate = DateTime.Now
                })
            Next

            dbContext.SaveChanges()
        End Sub

        Public Sub DeleteForReport(reportId As String, dbContext As AppDbContext) Implements IScannedReceiptAttachmentRepository.DeleteForReport
            If String.IsNullOrWhiteSpace(reportId) Then
                Return
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim existingReceipts = dbContext.ScannedReceiptAttachments.
                Where(Function(receipt) receipt.ReportID = reportId)

            dbContext.ScannedReceiptAttachments.RemoveRange(existingReceipts)
        End Sub

        Public Function GetMetadataByReportId(reportId As String) As List(Of ScannedReceiptAttachmentMetadataDto) Implements IScannedReceiptAttachmentRepository.GetMetadataByReportId
            If String.IsNullOrWhiteSpace(reportId) Then
                Return New List(Of ScannedReceiptAttachmentMetadataDto)()
            End If

            Using dbContext As New AppDbContext()
                Return dbContext.ScannedReceiptAttachments.
                    AsNoTracking().
                    Where(Function(receipt) receipt.ReportID = reportId).
                    OrderBy(Function(receipt) receipt.ID).
                    Select(Function(receipt) New ScannedReceiptAttachmentMetadataDto With {
                        .ID = receipt.ID,
                        .ReportID = receipt.ReportID,
                        .OriginalFileName = receipt.OriginalFileName,
                        .StoredFilePath = receipt.StoredFilePath,
                        .ContentType = receipt.ContentType,
                        .FileExtension = receipt.FileExtension,
                        .FileSizeBytes = receipt.FileSizeBytes,
                        .CreatedByUserID = receipt.CreatedByUserID,
                        .CreatedDate = receipt.CreatedDate
                    }).
                    ToList()
            End Using
        End Function

        Public Function GetById(receiptAttachmentId As Integer) As ScannedReceiptAttachmentDto Implements IScannedReceiptAttachmentRepository.GetById
            If receiptAttachmentId <= 0 Then
                Return Nothing
            End If

            Using dbContext As New AppDbContext()
                Dim receipt = dbContext.ScannedReceiptAttachments.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.ID = receiptAttachmentId)

                If receipt Is Nothing Then
                    Return Nothing
                End If

                Return New ScannedReceiptAttachmentDto With {
                    .ID = receipt.ID,
                    .ReportID = receipt.ReportID,
                    .OriginalFileName = receipt.OriginalFileName,
                    .StoredFilePath = receipt.StoredFilePath,
                    .ContentType = receipt.ContentType,
                    .FileExtension = receipt.FileExtension,
                    .FileSizeBytes = receipt.FileSizeBytes,
                    .ReceiptContent = receipt.ReceiptContent,
                    .CreatedByUserID = receipt.CreatedByUserID,
                    .CreatedDate = receipt.CreatedDate
                }
            End Using
        End Function
    End Class
End Namespace
