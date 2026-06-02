Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ReportDetailService
        Implements IReportDetailService

        Private ReadOnly _repository As IReportDetailRepository
        Private ReadOnly _cashAdvanceRepository As ICashAdvanceRepository
        Private ReadOnly _scannedReceiptAttachmentRepository As IScannedReceiptAttachmentRepository

        Public Sub New()
            Me.New(New ReportDetailRepository(), New CashAdvanceRepository(), New ScannedReceiptAttachmentRepository())
        End Sub

        Public Sub New(repository As IReportDetailRepository)
            Me.New(repository, New CashAdvanceRepository(), New ScannedReceiptAttachmentRepository())
        End Sub

        Public Sub New(repository As IReportDetailRepository, cashAdvanceRepository As ICashAdvanceRepository)
            Me.New(repository, cashAdvanceRepository, New ScannedReceiptAttachmentRepository())
        End Sub

        Public Sub New(repository As IReportDetailRepository,
                       cashAdvanceRepository As ICashAdvanceRepository,
                       scannedReceiptAttachmentRepository As IScannedReceiptAttachmentRepository)
            _repository = repository
            _cashAdvanceRepository = cashAdvanceRepository
            _scannedReceiptAttachmentRepository = scannedReceiptAttachmentRepository
        End Sub

        Public Function GetAll() As List(Of ReportDetailDto) Implements IReportDetailService.GetAll
            Return _repository.GetAll()
        End Function

        Public Function GetById(reportId As String) As ReportDetailDto Implements IReportDetailService.GetById
            Return _repository.GetById(reportId)
        End Function

        Public Function Create(report As CreateReportDetailDto) As ReportDetailDto Implements IReportDetailService.Create
            Return _repository.Create(report)
        End Function

        Public Function CreateReport(report As CreateReportDetailDto, cashAdvance As CreateCashAdvanceDto) As ReportDetailDto Implements IReportDetailService.CreateReport
            Return CreateReport(report, cashAdvance, Enumerable.Empty(Of String)(), Nothing)
        End Function

        Public Function CreateReport(report As CreateReportDetailDto,
                                     cashAdvance As CreateCashAdvanceDto,
                                     scannedReceiptPaths As IEnumerable(Of String),
                                     createdByUserId As Nullable(Of Integer)) As ReportDetailDto Implements IReportDetailService.CreateReport
            If report Is Nothing Then
                Throw New ArgumentNullException("report")
            End If

            If cashAdvance Is Nothing Then
                Throw New ArgumentNullException("cashAdvance")
            End If

            Dim receiptPaths As List(Of String) = If(scannedReceiptPaths, Enumerable.Empty(Of String)()).ToList()

            Using dbContext As New AppDbContext()
                Using transaction = dbContext.Database.BeginTransaction()
                    Dim createdReport = _repository.Create(report, dbContext)
                    _cashAdvanceRepository.Create(cashAdvance, dbContext)

                    If receiptPaths.Any() Then
                        _scannedReceiptAttachmentRepository.ReplaceForReport(report.ID, receiptPaths, createdByUserId, dbContext)
                    End If

                    transaction.Commit()
                    Return createdReport
                End Using
            End Using
        End Function

        Public Sub Update(report As UpdateReportDetailDto) Implements IReportDetailService.Update
            _repository.Update(report)
        End Sub
    End Class
End Namespace
