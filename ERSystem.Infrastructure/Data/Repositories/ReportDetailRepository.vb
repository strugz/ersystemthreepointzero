Imports System.Data.Entity
Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ReportDetailRepository
        Implements IReportDetailRepository

        Public Function GetAll() As List(Of ReportDetailDto) Implements IReportDetailRepository.GetAll
            Using dbContext As New AppDbContext()
                Return dbContext.ReportsDetails.
                    AsNoTracking().
                    Select(Function(report) ToDto(report)).
                    ToList()
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

        Private Shared Function ToDto(report As ReportDetailModel) As ReportDetailDto
            Return New ReportDetailDto With {
                .ID = report.ID,
                .ReportDateFrom = report.ReportDateFrom,
                .ReportDateTo = report.ReportDateTo,
                .ReportDescription = report.ReportDescription,
                .UserID = report.UserID,
                .ReportStatus = report.ReportStatus,
                .ReportEndorseSignature = report.ReportEndorseSignature,
                .ReportEndorseStatus = report.ReportEndorseStatus,
                .ReportDateFiled = report.ReportDateFiled,
                .ReportFileStatus = report.ReportFileStatus,
                .ExpenseID = report.ExpenseID,
                .ReportPrintStatus = report.ReportPrintStatus,
                .ReportReturnedForModi = report.ReportReturnedForModi,
                .ReportNumberStatus = report.ReportNumberStatus,
                .ReportReserveSignature = report.ReportReserveSignature,
                .ReportReserveStatus1 = report.ReportReserveStatus1,
                .ReportReserveStatus2 = report.ReportReserveStatus2,
                .ReportCancelNote = report.ReportCancelNote,
                .ReportAttachment = report.ReportAttachment,
                .ReportSentStatus = report.ReportSentStatus
            }
        End Function
    End Class
End Namespace
