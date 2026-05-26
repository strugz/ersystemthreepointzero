Option Strict On

Imports System.Data.Entity
Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ExpenseDetailRepository
        Implements IExpenseDetailRepository

        Public Function GetAll() As List(Of ExpenseDetailDto) Implements IExpenseDetailRepository.GetAll
            Using dbContext As New AppDbContext()
                Dim expenses = dbContext.ExpenseDetails.
                    AsNoTracking().
                    ToList()

                Return expenses.Select(Function(expense) ToDto(expense)).ToList()
            End Using
        End Function

        Public Function GetById(expenseId As Integer) As ExpenseDetailDto Implements IExpenseDetailRepository.GetById
            If expenseId <= 0 Then
                Return Nothing
            End If

            Using dbContext As New AppDbContext()
                Dim expense = dbContext.ExpenseDetails.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.ID = expenseId)

                If expense Is Nothing Then
                    Return Nothing
                End If

                Return ToDto(expense)
            End Using
        End Function

        Public Function Create(expense As CreateExpenseDetailDto) As ExpenseDetailDto Implements IExpenseDetailRepository.Create
            Using dbContext As New AppDbContext()
                Return Create(expense, dbContext)
            End Using
        End Function

        Public Function Create(expense As CreateExpenseDetailDto, dbContext As AppDbContext) As ExpenseDetailDto Implements IExpenseDetailRepository.Create
            If expense Is Nothing Then
                Throw New ArgumentNullException("expense")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim model As ExpenseDetailModel = ToModel(expense)
            dbContext.ExpenseDetails.Add(model)
            dbContext.SaveChanges()
            Return ToDto(model)
        End Function

        Public Sub Update(expense As UpdateExpenseDetailDto) Implements IExpenseDetailRepository.Update
            If expense Is Nothing Then
                Throw New ArgumentNullException("expense")
            End If

            If expense.ID <= 0 Then
                Throw New ArgumentException("Expense ID is required.", "expense")
            End If

            Using dbContext As New AppDbContext()
                Dim existing = dbContext.ExpenseDetails.FirstOrDefault(Function(item) item.ID = expense.ID)

                If existing Is Nothing Then
                    Throw New InvalidOperationException("Expense details were not found.")
                End If

                existing.ExpenseTransDate = expense.ExpenseTransDate
                existing.ExpensePerdiem = expense.ExpensePerdiem
                existing.ExpenseParticulars = expense.ExpenseParticulars
                existing.ExpenseInvoice = expense.ExpenseInvoice
                existing.ExpenseMultiplier = expense.ExpenseMultiplier
                existing.ExpenseType = expense.ExpenseType
                existing.ExpenseCategory = expense.ExpenseCategory
                existing.ExpenseAmount = expense.ExpenseAmount
                existing.ExpenseRemarks = expense.ExpenseRemarks
                existing.ExpenseStatus = expense.ExpenseStatus
                existing.UserID = expense.UserID
                existing.ExpenseTotalAmount = expense.ExpenseTotalAmount
                existing.ExpenseLocation = expense.ExpenseLocation
                existing.ReportID = expense.ReportID
                existing.WorkWith = expense.WorkWith
                existing.ServiceNumber = expense.ServiceNumber
                existing.Instrument = expense.Instrument
                existing.SerialNumber = expense.SerialNumber
                existing.Sort = expense.Sort
                existing.MDays = expense.MDays
                existing.Computation = expense.Computation
                existing.TotDays = expense.TotDays
                existing.NumberEdited = expense.NumberEdited
                dbContext.SaveChanges()
            End Using
        End Sub

        Private Shared Function ToDto(expense As ExpenseDetailModel) As ExpenseDetailDto
            Return New ExpenseDetailDto With {
                .ID = expense.ID,
                .ExpenseTransDate = expense.ExpenseTransDate,
                .ExpensePerdiem = expense.ExpensePerdiem,
                .ExpenseParticulars = expense.ExpenseParticulars,
                .ExpenseInvoice = expense.ExpenseInvoice,
                .ExpenseMultiplier = expense.ExpenseMultiplier,
                .ExpenseType = expense.ExpenseType,
                .ExpenseCategory = expense.ExpenseCategory,
                .ExpenseAmount = expense.ExpenseAmount,
                .ExpenseRemarks = expense.ExpenseRemarks,
                .ExpenseStatus = expense.ExpenseStatus,
                .UserID = expense.UserID,
                .ExpenseTotalAmount = expense.ExpenseTotalAmount,
                .ExpenseLocation = expense.ExpenseLocation,
                .ReportID = expense.ReportID,
                .WorkWith = expense.WorkWith,
                .ServiceNumber = expense.ServiceNumber,
                .Instrument = expense.Instrument,
                .SerialNumber = expense.SerialNumber,
                .Sort = expense.Sort,
                .MDays = expense.MDays,
                .Computation = expense.Computation,
                .TotDays = expense.TotDays,
                .NumberEdited = expense.NumberEdited
            }
        End Function

        Private Shared Function ToModel(expense As CreateExpenseDetailDto) As ExpenseDetailModel
            Return New ExpenseDetailModel With {
                .ExpenseTransDate = expense.ExpenseTransDate,
                .ExpensePerdiem = expense.ExpensePerdiem,
                .ExpenseParticulars = expense.ExpenseParticulars,
                .ExpenseInvoice = expense.ExpenseInvoice,
                .ExpenseMultiplier = expense.ExpenseMultiplier,
                .ExpenseType = expense.ExpenseType,
                .ExpenseCategory = expense.ExpenseCategory,
                .ExpenseAmount = expense.ExpenseAmount,
                .ExpenseRemarks = expense.ExpenseRemarks,
                .ExpenseStatus = expense.ExpenseStatus,
                .UserID = expense.UserID,
                .ExpenseTotalAmount = expense.ExpenseTotalAmount,
                .ExpenseLocation = expense.ExpenseLocation,
                .ReportID = expense.ReportID,
                .WorkWith = expense.WorkWith,
                .ServiceNumber = expense.ServiceNumber,
                .Instrument = expense.Instrument,
                .SerialNumber = expense.SerialNumber,
                .MDays = expense.MDays,
                .Computation = expense.Computation,
                .TotDays = expense.TotDays
            }
        End Function
    End Class
End Namespace
