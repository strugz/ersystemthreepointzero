Imports System.Data

Public NotInheritable Class EReportPresenter
    Private ReadOnly _gateway As ExpenseLegacyStateGateway
    Private ReadOnly _requestFactory As ExpenseEntryRequestFactory
    Private ReadOnly _expenseEntryService As Global.ERSystem.AppServices.Services.ExpenseReport.ExpenseEntryService

    Public Sub New()
        Me.New(
            New ExpenseLegacyStateGateway(),
            New ExpenseEntryRequestFactory(),
            New Global.ERSystem.AppServices.Services.ExpenseReport.ExpenseEntryService())
    End Sub

    Public Sub New(
        gateway As ExpenseLegacyStateGateway,
        requestFactory As ExpenseEntryRequestFactory,
        expenseEntryService As Global.ERSystem.AppServices.Services.ExpenseReport.ExpenseEntryService)

        If gateway Is Nothing Then
            Throw New ArgumentNullException("gateway")
        End If

        If requestFactory Is Nothing Then
            Throw New ArgumentNullException("requestFactory")
        End If

        If expenseEntryService Is Nothing Then
            Throw New ArgumentNullException("expenseEntryService")
        End If

        _gateway = gateway
        _requestFactory = requestFactory
        _expenseEntryService = expenseEntryService
    End Sub

    Public Function AddExpense(snapshot As EReportEntrySnapshot) As EReportEntryResult
        Dim validationMessage As String = ValidateForAdd(snapshot)
        If validationMessage <> String.Empty Then
            Return EReportEntryResult.Failed(validationMessage)
        End If

        Try
            Dim reportData As String() = _gateway.LoadReportSettings()
            Dim request As Global.ERSystem.Domain.AddExpenseRequestDto =
                _requestFactory.CreateAddExpenseRequest(
                    snapshot,
                    reportData,
                    _gateway.GetMealPayload(),
                    _gateway.GetTransportationPayload())

            Dim serviceResult As Global.ERSystem.AppServices.Services.ExpenseReport.AddExpenseResult =
                _expenseEntryService.AddExpense(request)

            If Not serviceResult.Success Then
                Return EReportEntryResult.Failed(serviceResult.Message)
            End If

            Return New EReportEntryResult With {
                .Success = True,
                .ShouldAskClearDetails = True,
                .ShouldRefreshExpenseGrid = True,
                .RefreshReportId = GetArrayValue(reportData, 13),
                .RefreshUserId = _gateway.GetCurrentUserId(),
                .FocusParticulars = True,
                .EnableExpenseGrid = True,
                .ResetTransactionId = True,
                .ResetStatusIndex = 0,
                .EnableCategory = True,
                .ClearComputation = True
            }
        Catch ex As Exception
            Return EReportEntryResult.Failed(ex.ToString)
        End Try
    End Function

    Public Function UpdateExpense(snapshot As EReportEntrySnapshot) As EReportEntryResult
        Dim validationMessage As String = ValidateForUpdate(snapshot)
        If validationMessage <> String.Empty Then
            Return EReportEntryResult.Failed(validationMessage)
        End If

        Try
            Dim expenseData As String() = _gateway.LoadExpenseSettings()
            Dim reportData As String() = _gateway.LoadReportSettings()
            Dim request As Global.ERSystem.Domain.UpdateExpenseRequestDto =
                _requestFactory.CreateUpdateExpenseRequest(
                    snapshot,
                    expenseData,
                    reportData,
                    _gateway.GetMealPayload(),
                    _gateway.GetTransportationPayload())

            Dim serviceResult As Global.ERSystem.AppServices.Services.ExpenseReport.UpdateExpenseResult =
                _expenseEntryService.UpdateExpense(request)

            If Not serviceResult.Success Then
                Return EReportEntryResult.Failed(serviceResult.Message)
            End If

            Return New EReportEntryResult With {
                .Success = True,
                .ShouldAskClearDetails = True,
                .ShouldRefreshExpenseGrid = True,
                .RefreshReportId = GetArrayValue(reportData, 13),
                .RefreshUserId = GetArrayValue(reportData, 14),
                .FocusParticulars = True,
                .EnableExpenseGrid = True,
                .ResetTransactionId = True,
                .ResetStatusIndex = 0,
                .EnableCategory = True,
                .DeleteExpenseSettingsAfterClear = True,
                .PersistWorkContext = True
            }
        Catch ex As Exception
            Return EReportEntryResult.Failed(ex.Message)
        End Try
    End Function

    Public Sub ApplyClearChoice(clearWorkContext As Boolean, deleteExpenseSettings As Boolean)
        _gateway.ClearExpenseEntry(clearWorkContext)

        If deleteExpenseSettings Then
            _gateway.DeleteExpenseSettings()
        End If
    End Sub

    Public Sub PersistWorkContext(snapshot As EReportEntrySnapshot)
        _gateway.SaveWorkContext(snapshot.WorkWith, snapshot.Location, snapshot.Instrument, snapshot.SerialNumber, snapshot.ServiceNumber)
    End Sub

    Public Function LoadExpenseReport(reportId As String, userId As String) As DataTable
        Return _gateway.LoadExpenseReport(reportId, userId)
    End Function

    Private Shared Function ValidateForAdd(snapshot As EReportEntrySnapshot) As String
        Dim commonValidation As String = ValidateCommon(snapshot)
        If commonValidation <> String.Empty Then
            Return commonValidation
        End If

        If Not snapshot.LocalChecked AndAlso Not snapshot.ForeignChecked Then
            Return "Please Select Type"
        End If

        If Not snapshot.CategorySelected Then
            Return "Please Select Category"
        End If

        If String.IsNullOrEmpty(snapshot.WorkWith) Then
            Return "Please fill the WorkWith"
        End If

        Return String.Empty
    End Function

    Private Shared Function ValidateForUpdate(snapshot As EReportEntrySnapshot) As String
        Return ValidateCommon(snapshot)
    End Function

    Private Shared Function ValidateCommon(snapshot As EReportEntrySnapshot) As String
        If snapshot Is Nothing Then
            Return "Expense entry is required."
        End If

        If String.IsNullOrEmpty(snapshot.Particulars) OrElse
            String.IsNullOrEmpty(snapshot.Amount) OrElse
            String.IsNullOrEmpty(snapshot.Category) Then
            Return "Please fill in the Particulars/Expense Amount/Category"
        End If

        If Not String.IsNullOrWhiteSpace(snapshot.Invoice) AndAlso String.IsNullOrWhiteSpace(snapshot.VatAmount) Then
            Return "Please fill the VAT Amount"
        End If

        Return String.Empty
    End Function

    Private Shared Function GetArrayValue(values As String(), index As Integer) As String
        If values Is Nothing OrElse values.Length <= index Then
            Return String.Empty
        End If

        Return values(index)
    End Function
End Class
