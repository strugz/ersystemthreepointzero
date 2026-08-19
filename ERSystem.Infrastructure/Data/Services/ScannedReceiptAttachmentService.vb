Option Strict On

Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ScannedReceiptAttachmentService
        Private ReadOnly _repository As IScannedReceiptAttachmentRepository

        Public Sub New()
            Me.New(New ScannedReceiptAttachmentRepository())
        End Sub

        Public Sub New(repository As IScannedReceiptAttachmentRepository)
            If repository Is Nothing Then
                Throw New ArgumentNullException("repository")
            End If

            _repository = repository
        End Sub

        Public Sub ReplaceForReport(request As SaveScannedReceiptAttachmentRequest)
            If request Is Nothing Then
                Throw New ArgumentNullException("request")
            End If

            If String.IsNullOrWhiteSpace(request.ReportID) Then
                Throw New ArgumentException("Report ID is required.", "request")
            End If

            Using dbContext As New AppDbContext()
                _repository.ReplaceForReport(request.ReportID, request.LocalPaths, request.CreatedByUserID, dbContext)
            End Using
        End Sub

        Public Sub DeleteForReport(reportId As String)
            Using dbContext As New AppDbContext()
                _repository.DeleteForReport(reportId, dbContext)
                dbContext.SaveChanges()
            End Using
        End Sub

        Public Function GetMetadataByReportId(reportId As String) As List(Of ScannedReceiptAttachmentMetadataDto)
            Return _repository.GetMetadataByReportId(reportId)
        End Function

        Public Function GetById(receiptAttachmentId As Integer) As ScannedReceiptAttachmentDto
            Return _repository.GetById(receiptAttachmentId)
        End Function
    End Class
End Namespace
