Option Strict On

Imports ER_System.Presentation.ViewModels

Namespace Presentation.Presenters
    Public Class ExpenseUpdatePresenter
        Public Function Validate(ByVal model As ExpenseUpdateViewModel) As String
            If model Is Nothing Then
                Return "Invalid expense data"
            End If

            If String.IsNullOrWhiteSpace(model.Particulars) OrElse
                String.IsNullOrWhiteSpace(model.Amount) OrElse
                String.IsNullOrWhiteSpace(model.Category) Then
                Return "Please fill in the Particulars/Expense Amount/Category"
            End If

            If Val(model.Amount) = 0 Then
                Return "Expense Amount cannot be 0"
            End If

            Return String.Empty
        End Function

        Public Sub UpdateExpense(ByVal model As ExpenseUpdateViewModel)
            ER_System.modMaintenance.UpdateExpense(
                model.TransactionId,
                model.ExpenseDateText,
                If(model.IsPerdiem, "1", "0"),
                model.Particulars,
                model.Invoice,
                model.Multiplier,
                model.ExpenseType,
                model.Category,
                model.Amount,
                model.Remarks,
                model.Status,
                model.Total,
                model.Location,
                model.UserId,
                model.ServiceNumber,
                model.Instrument,
                model.SerialNumber,
                model.WorkWith,
                model.MealValue,
                model.TransportationValue,
                model.MDays,
                model.Computation,
                model.TotalDays)

            ER_System.modMaintenance.AddExpenseHisto(
                model.ExpenseDateText,
                If(model.IsPerdiem, "1", "0"),
                model.Particulars,
                model.Invoice,
                model.Multiplier,
                model.ExpenseType,
                model.Category,
                model.Amount,
                model.Remarks,
                model.Status,
                model.Total,
                model.Location,
                model.UserId,
                model.ReportId,
                model.TransactionId,
                model.ServiceNumber,
                model.Instrument,
                model.SerialNumber,
                model.EditedByUserId,
                model.MDays,
                model.Computation,
                model.TotalDays)
        End Sub
    End Class
End Namespace
