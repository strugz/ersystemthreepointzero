Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ReportDetailService
        Implements IReportDetailService

        Private ReadOnly _repository As IReportDetailRepository

        Public Sub New()
            Me.New(New ReportDetailRepository())
        End Sub

        Public Sub New(repository As IReportDetailRepository)
            _repository = repository
        End Sub

        Public Function GetAll() As List(Of ReportDetailDto) Implements IReportDetailService.GetAll
            Return _repository.GetAll()
        End Function

        Public Function GetById(reportId As String) As ReportDetailDto Implements IReportDetailService.GetById
            Return _repository.GetById(reportId)
        End Function
    End Class
End Namespace
