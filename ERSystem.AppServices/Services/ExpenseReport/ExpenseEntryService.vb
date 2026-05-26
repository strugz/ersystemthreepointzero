Option Strict On

Imports System.Data
Imports ERSystem.Domain
Imports ERSystem.Infrastructure
Imports ERSystem.Infrastructure.Data

Namespace Global.ERSystem.AppServices.Services.ExpenseReport
    Public Class ExpenseEntryService

        Private Const ActiveExpenseStatus As String = "True"
        Private Const DefaultWorkWith As String = "NONE"

        Private ReadOnly _expenseDetailRepository As IExpenseDetailRepository
        Private ReadOnly _expenseMealItemRepository As IExpenseMealItemRepository
        Private ReadOnly _expenseTransportationItemRepository As IExpenseTransportationItemRepository
        Private ReadOnly _expenseNotificationRepository As IExpenseNotificationRepository
        Private ReadOnly _expenseIdCounterRepository As IExpenseIdCounterRepository

        Public Sub New()
            Me.New(
                New ExpenseDetailRepository(),
                New ExpenseMealItemRepository(),
                New ExpenseTransportationItemRepository(),
                New ExpenseNotificationRepository(),
                New ExpenseIdCounterRepository())
        End Sub

        Public Sub New(
            expenseDetailRepository As IExpenseDetailRepository,
            expenseMealItemRepository As IExpenseMealItemRepository,
            expenseTransportationItemRepository As IExpenseTransportationItemRepository,
            expenseNotificationRepository As IExpenseNotificationRepository,
            expenseIdCounterRepository As IExpenseIdCounterRepository)

            If expenseDetailRepository Is Nothing Then
                Throw New ArgumentNullException("expenseDetailRepository")
            End If

            If expenseMealItemRepository Is Nothing Then
                Throw New ArgumentNullException("expenseMealItemRepository")
            End If

            If expenseTransportationItemRepository Is Nothing Then
                Throw New ArgumentNullException("expenseTransportationItemRepository")
            End If

            If expenseNotificationRepository Is Nothing Then
                Throw New ArgumentNullException("expenseNotificationRepository")
            End If

            If expenseIdCounterRepository Is Nothing Then
                Throw New ArgumentNullException("expenseIdCounterRepository")
            End If

            _expenseDetailRepository = expenseDetailRepository
            _expenseMealItemRepository = expenseMealItemRepository
            _expenseTransportationItemRepository = expenseTransportationItemRepository
            _expenseNotificationRepository = expenseNotificationRepository
            _expenseIdCounterRepository = expenseIdCounterRepository
        End Sub

        Public Function AddExpense(request As AddExpenseRequestDto) As AddExpenseResult
            If request Is Nothing Then
                Return AddExpenseResult.Failed("Expense request is required.")
            End If

            Using dbContext As New AppDbContext()
                Using transaction = dbContext.Database.BeginTransaction(IsolationLevel.Serializable)
                    Try
                        Dim expenseId As Long = _expenseIdCounterRepository.GetNextExpenseId(dbContext)
                        Dim sort As Integer = _expenseDetailRepository.CountActiveByReportId(request.ReportID, dbContext)

                        _expenseDetailRepository.Create(CreateExpenseDetail(request, expenseId, sort), dbContext)

                        If Not String.IsNullOrWhiteSpace(request.Meal) Then
                            _expenseMealItemRepository.Create(CreateMealItem(request, expenseId), dbContext)
                        End If

                        If request.FareID.HasValue Then
                            _expenseTransportationItemRepository.Create(CreateTransportationItem(request, expenseId), dbContext)
                        End If

                        If ShouldCreateNotification(request.Meal) Then
                            _expenseNotificationRepository.Create(CreateNotification(request, expenseId), dbContext)
                        End If

                        transaction.Commit()
                        Return AddExpenseResult.Succeeded(expenseId)
                    Catch ex As Exception
                        transaction.Rollback()
                        Return AddExpenseResult.Failed(ex.Message)
                    End Try
                End Using
            End Using
        End Function

        Private Shared Function CreateExpenseDetail(request As AddExpenseRequestDto, expenseId As Long, sort As Integer) As CreateExpenseDetailDto
            Return New CreateExpenseDetailDto With {
                .ID = expenseId,
                .ExpenseTransDate = request.Transdate,
                .ExpensePerdiem = request.Perdiem,
                .ExpenseParticulars = request.Particulars,
                .ExpenseInvoice = request.Invoice,
                .ExpenseMultiplier = request.Multiplier,
                .ExpenseType = request.Type,
                .ExpenseCategory = request.Category,
                .ExpenseAmount = request.Amount,
                .ExpenseRemarks = request.Remarks,
                .ExpenseStatus = ActiveExpenseStatus,
                .UserID = request.UserID,
                .ExpenseTotalAmount = request.TotalAmount,
                .ExpenseLocation = request.Location,
                .ReportID = request.ReportID,
                .WorkWith = NormalizeWorkWith(request.WorkWith),
                .ServiceNumber = request.ServiceNumber,
                .Instrument = request.Instrument,
                .SerialNumber = request.SerialNumber,
                .Sort = sort,
                .MDays = request.MDays,
                .Computation = request.Computation,
                .TotDays = request.TotDays,
                .NumberEdited = 0
            }
        End Function

        Private Shared Function CreateMealItem(request As AddExpenseRequestDto, expenseId As Long) As ExpenseMealItemDto
            Return New ExpenseMealItemDto With {
                .ExpenseID = expenseId,
                .Meal = request.Meal,
                .PaidFor = request.PaidFor,
                .PaidEmp = request.PaidEmp
            }
        End Function

        Private Shared Function CreateTransportationItem(request As AddExpenseRequestDto, expenseId As Long) As ExpenseTransportationItemDto
            Return New ExpenseTransportationItemDto With {
                .expense_id = expenseId,
                .FareID = request.FareID.Value.ToString(),
                .FareFrom = request.FareFrom,
                .FareTo = request.FareTo
            }
        End Function

        Private Shared Function CreateNotification(request As AddExpenseRequestDto, expenseId As Long) As ExpenseNotificationDto
            Return New ExpenseNotificationDto With {
                .ID = Guid.NewGuid().ToString(),
                .ToNotify = request.PaidEmp,
                .DateIncluded = request.Transdate,
                .ExpenseID = expenseId.ToString(),
                .Category = request.Meal,
                .UsernameFiled = If(request.UserID.HasValue, request.UserID.Value.ToString(), Nothing),
                .Status = 0
            }
        End Function

        Private Shared Function NormalizeWorkWith(workWith As String) As String
            If String.IsNullOrWhiteSpace(workWith) Then
                Return DefaultWorkWith
            End If

            Return workWith
        End Function

        Private Shared Function ShouldCreateNotification(meal As String) As Boolean
            If String.IsNullOrWhiteSpace(meal) Then
                Return True
            End If

            Return meal.Split("^"c).Length <= 2
        End Function
    End Class
End Namespace
