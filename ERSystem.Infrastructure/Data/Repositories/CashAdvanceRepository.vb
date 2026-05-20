Imports System.Data.Entity
Imports System.Linq
Imports ERSystem.Domain
Imports ERSystem.Domain.Dtos.CashAdvance

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class CashAdvanceRepository
        Implements ICashAdvanceRepository

        Public Function GetAll() As List(Of CashAdvanceDto) Implements ICashAdvanceRepository.GetAll
            Using dbContext As New AppDbContext()
                Return dbContext.CashAdvances.
                    AsNoTracking().
                    Select(Function(cashAdvance) ToDto(cashAdvance)).
                    ToList()
            End Using
        End Function

        Public Function GetByReportId(reportId As String) As List(Of CashAdvanceDto) Implements ICashAdvanceRepository.GetByReportId
            If String.IsNullOrWhiteSpace(reportId) Then
                Return New List(Of CashAdvanceDto)()
            End If

            Using dbContext As New AppDbContext()
                Return dbContext.CashAdvances.
                    AsNoTracking().
                    Where(Function(item) item.ReportID = reportId).
                    Select(Function(cashAdvance) ToDto(cashAdvance)).
                    ToList()
            End Using
        End Function

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
    End Class
End Namespace
