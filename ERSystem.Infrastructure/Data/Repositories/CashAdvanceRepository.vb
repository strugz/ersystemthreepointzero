Imports System.Data.Entity
Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class CashAdvanceRepository
        Implements ICashAdvanceRepository

        Public Function GetAll() As List(Of CashAdvanceDto) Implements ICashAdvanceRepository.GetAll
            Using dbContext As New AppDbContext()
                Dim cashAdvances = dbContext.CashAdvances.
                    AsNoTracking().
                    ToList()

                Return cashAdvances.Select(Function(cashAdvance) ToDto(cashAdvance)).ToList()
            End Using
        End Function

        Public Function GetByReportId(reportId As String) As List(Of CashAdvanceDto) Implements ICashAdvanceRepository.GetByReportId
            If String.IsNullOrWhiteSpace(reportId) Then
                Return New List(Of CashAdvanceDto)()
            End If

            Using dbContext As New AppDbContext()
                Dim cashAdvances = dbContext.CashAdvances.
                    AsNoTracking().
                    Where(Function(item) item.ReportID = reportId).
                    ToList()

                Return cashAdvances.Select(Function(cashAdvance) ToDto(cashAdvance)).ToList()
            End Using
        End Function

        Public Function Create(cashAdvance As CreateCashAdvanceDto) As CashAdvanceDto Implements ICashAdvanceRepository.Create
            Using dbContext As New AppDbContext()
                Return Create(cashAdvance, dbContext)
            End Using
        End Function

        Public Function Create(cashAdvance As CreateCashAdvanceDto, dbContext As AppDbContext) As CashAdvanceDto Implements ICashAdvanceRepository.Create
            If cashAdvance Is Nothing Then
                Throw New ArgumentNullException("cashAdvance")
            End If

            If String.IsNullOrWhiteSpace(cashAdvance.ReportID) Then
                Throw New ArgumentException("Report ID is required.", "cashAdvance")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim model As CashAdvanceModel = ToModel(cashAdvance)
            dbContext.CashAdvances.Add(model)
            dbContext.SaveChanges()
            Return ToDto(model)
        End Function

        Public Sub UpdateByReportId(reportId As String, cashAdvance As UpdateCashAdvanceDto) Implements ICashAdvanceRepository.UpdateByReportId
            Using dbContext As New AppDbContext()
                UpdateByReportId(reportId, cashAdvance, dbContext)
            End Using
        End Sub

        Public Sub UpdateByReportId(reportId As String,
                                    cashAdvance As UpdateCashAdvanceDto,
                                    dbContext As AppDbContext) Implements ICashAdvanceRepository.UpdateByReportId
            If String.IsNullOrWhiteSpace(reportId) Then
                Throw New ArgumentException("Report ID is required.", "reportId")
            End If

            If cashAdvance Is Nothing Then
                Throw New ArgumentNullException("cashAdvance")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim existing = dbContext.CashAdvances.FirstOrDefault(Function(item) item.ReportID = reportId)

            If existing Is Nothing Then
                existing = New CashAdvanceModel With {.ReportID = reportId}
                dbContext.CashAdvances.Add(existing)
            End If

            existing.emp_userID = cashAdvance.emp_userID
            existing.CashAmount = cashAdvance.CashAmount
            existing.CashDate = cashAdvance.CashDate
            existing.CashRefDoc = cashAdvance.CashRefDoc
            existing.CashRefNo = cashAdvance.CashRefNo
            existing.BalanceTo = cashAdvance.BalanceTo
            existing.RevolvingFund = cashAdvance.RevolvingFund
            existing.CashCheck = cashAdvance.CashCheck
            dbContext.SaveChanges()
        End Sub

        Private Shared Function ToDto(cashAdvance As CashAdvanceModel) As CashAdvanceDto
            Return New CashAdvanceDto With {
                .ID = cashAdvance.ID,
                .ReportID = cashAdvance.ReportID,
                .emp_userID = cashAdvance.emp_userID,
                .CashAmount = cashAdvance.CashAmount,
                .CashDate = cashAdvance.CashDate,
                .CashRefDoc = cashAdvance.CashRefDoc,
                .CashRefNo = cashAdvance.CashRefNo,
                .BalanceTo = cashAdvance.BalanceTo,
                .RevolvingFund = cashAdvance.RevolvingFund,
                .CashCheck = cashAdvance.CashCheck
            }
        End Function

        Private Shared Function ToModel(cashAdvance As CreateCashAdvanceDto) As CashAdvanceModel
            Return New CashAdvanceModel With {
                .ReportID = cashAdvance.ReportID,
                .emp_userID = cashAdvance.emp_userID,
                .CashAmount = cashAdvance.CashAmount,
                .CashDate = cashAdvance.CashDate,
                .CashRefDoc = cashAdvance.CashRefDoc,
                .CashRefNo = cashAdvance.CashRefNo,
                .BalanceTo = cashAdvance.BalanceTo,
                .RevolvingFund = cashAdvance.RevolvingFund,
                .CashCheck = cashAdvance.CashCheck
            }
        End Function
    End Class
End Namespace
