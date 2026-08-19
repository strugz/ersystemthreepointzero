Imports System.Data.Entity
Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ReportDetailRepository
        Implements IReportDetailRepository

        Public Function GetAll() As List(Of ReportDetailDto) Implements IReportDetailRepository.GetAll
            Using dbContext As New AppDbContext()
                Dim reports = dbContext.ReportsDetails.
                    AsNoTracking().
                    ToList()

                Return reports.Select(Function(report) ToDto(report)).ToList()
            End Using
        End Function

        Public Function GetById(reportId As String) As ReportDetailDto Implements IReportDetailRepository.GetById
            If String.IsNullOrWhiteSpace(reportId) Then
                Return Nothing
            End If

            Using dbContext As New AppDbContext()
                Dim report = dbContext.ReportsDetails.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.ID = reportId)

                If report Is Nothing Then
                    Return Nothing
                End If

                Return ToDto(report)
            End Using
        End Function

        Public Function Create(report As CreateReportDetailDto) As ReportDetailDto Implements IReportDetailRepository.Create
            Using dbContext As New AppDbContext()
                Return Create(report, dbContext)
            End Using
        End Function

        Public Function Create(report As CreateReportDetailDto, dbContext As AppDbContext) As ReportDetailDto Implements IReportDetailRepository.Create
            If report Is Nothing Then
                Throw New ArgumentNullException("report")
            End If

            If String.IsNullOrWhiteSpace(report.ID) Then
                Throw New ArgumentException("Report ID is required.", "report")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim model As ReportDetailModel = ToModel(report)
            dbContext.ReportsDetails.Add(model)
            dbContext.SaveChanges()
            Return ToDto(model)
        End Function

        Public Sub Update(report As UpdateReportDetailDto) Implements IReportDetailRepository.Update
            Using dbContext As New AppDbContext()
                Update(report, dbContext)
            End Using
        End Sub

        Public Sub Update(report As UpdateReportDetailDto, dbContext As AppDbContext) Implements IReportDetailRepository.Update
            If report Is Nothing Then
                Throw New ArgumentNullException("report")
            End If

            If String.IsNullOrWhiteSpace(report.ID) Then
                Throw New ArgumentException("Report ID is required.", "report")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim existing = dbContext.ReportsDetails.FirstOrDefault(Function(item) item.ID = report.ID)

            If existing Is Nothing Then
                Throw New InvalidOperationException("Report details were not found.")
            End If

            existing.ReportDateFrom = report.ReportDateFrom
            existing.ReportDateTo = report.ReportDateTo
            existing.ReportDescription = report.ReportDescription
            existing.ReportAttachment = report.ReportAttachment
            existing.ReportType = report.ReportType
            existing.ERFReferenceNo = report.ERFReferenceNo
            dbContext.SaveChanges()
        End Sub

        Private Shared Function ToDto(report As ReportDetailModel) As ReportDetailDto
            Return New ReportDetailDto With {
                .ID = report.ID,
                .ReportDateFrom = report.ReportDateFrom,
                .ReportDateTo = report.ReportDateTo,
                .ReportDescription = report.ReportDescription,
                .UserID = report.UserID,
                .ReportStatus = report.ReportStatus,
                .ReportEndorseStatus = report.ReportEndorseStatus,
                .ReportDateFiled = report.ReportDateFiled,
                .ReportFileStatus = report.ReportFileStatus,
                .ReportPrintStatus = report.ReportPrintStatus,
                .ReportNumberStatus = report.ReportNumberStatus,
                .ReportAttachment = report.ReportAttachment,
                .ReportType = report.ReportType,
                .ERFReferenceNo = report.ERFReferenceNo
            }
        End Function

        Private Shared Function ToModel(report As CreateReportDetailDto) As ReportDetailModel
            Return New ReportDetailModel With {
                .ID = report.ID,
                .ReportDateFrom = report.ReportDateFrom,
                .ReportDateTo = report.ReportDateTo,
                .ReportDescription = report.ReportDescription,
                .UserID = report.UserID,
                .ReportStatus = report.ReportStatus,
                .ReportEndorseStatus = report.ReportEndorseStatus,
                .ReportDateFiled = report.ReportDateFiled,
                .ReportFileStatus = report.ReportFileStatus,
                .ReportPrintStatus = report.ReportPrintStatus,
                .ReportNumberStatus = report.ReportNumberStatus,
                .ReportAttachment = report.ReportAttachment,
                .ReportType = report.ReportType,
                .ERFReferenceNo = report.ERFReferenceNo
            }
        End Function
End Class
End Namespace
