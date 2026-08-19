Imports System.Globalization

Public NotInheritable Class ExpenseEntryRequestFactory
    Public Function CreateAddExpenseRequest(
        snapshot As EReportEntrySnapshot,
        reportData As String(),
        mealData As String,
        transportationData As String) As Global.ERSystem.Domain.AddExpenseRequestDto

        Dim mealParts As String() = SplitLegacyDelimitedValue(mealData)
        Dim transportationParts As String() = SplitLegacyDelimitedValue(transportationData)

        Return New Global.ERSystem.Domain.AddExpenseRequestDto With {
            .Transdate = snapshot.Transdate.Date,
            .Perdiem = If(snapshot.PerdiemChecked, "1", "0"),
            .Particulars = snapshot.Particulars,
            .Invoice = snapshot.Invoice,
            .Multiplier = ParseRequiredInteger(snapshot.Multiplier, "Multiplier"),
            .Type = If(snapshot.LocalChecked, "Local", "Foreign"),
            .Category = snapshot.Category,
            .Amount = ParseRequiredDouble(snapshot.Amount, "Expense Amount"),
            .VatAmount = ParseVatAmount(snapshot.Invoice, snapshot.VatAmount),
            .Remarks = snapshot.Remarks,
            .Status = snapshot.Status,
            .TotalAmount = ParseRequiredDouble(snapshot.TotalAmount, "Total Amount"),
            .Location = If(Trim(snapshot.Location) = "", "Allowance", Trim(snapshot.Location)),
            .UserID = ParseRequiredInteger(GetArrayValue(reportData, 14), "User ID"),
            .ReportID = GetArrayValue(reportData, 13),
            .WorkWith = snapshot.WorkWith,
            .ServiceNumber = If(snapshot.ServiceNumber = "", "N/A", snapshot.ServiceNumber),
            .Instrument = If(snapshot.Instrument = "", "N/A", snapshot.Instrument),
            .SerialNumber = If(snapshot.SerialNumber = "", "N/A", snapshot.SerialNumber),
            .MDays = snapshot.MDays,
            .Computation = snapshot.Computation,
            .TotDays = snapshot.TotalDays,
            .Meal = GetArrayValue(mealParts, 0),
            .PaidFor = GetArrayValue(mealParts, 1),
            .PaidEmp = GetArrayValue(mealParts, 2),
            .FareID = ParseNullableLong(GetArrayValue(transportationParts, 0)),
            .FareFrom = GetArrayValue(transportationParts, 1),
            .FareTo = GetArrayValue(transportationParts, 2)
        }
    End Function

    Public Function CreateUpdateExpenseRequest(
        snapshot As EReportEntrySnapshot,
        expenseData As String(),
        reportData As String(),
        mealData As String,
        transportationData As String) As Global.ERSystem.Domain.UpdateExpenseRequestDto

        Dim mealParts As String() = SplitLegacyDelimitedValue(mealData)
        Dim transportationParts As String() = SplitLegacyDelimitedValue(transportationData)

        Return New Global.ERSystem.Domain.UpdateExpenseRequestDto With {
            .ExpenseID = ParseRequiredLong(GetArrayValue(expenseData, 16), "Expense ID"),
            .Transdate = snapshot.Transdate.Date,
            .Perdiem = If(snapshot.PerdiemChecked, "1", "0"),
            .Particulars = snapshot.Particulars,
            .Invoice = snapshot.Invoice,
            .Multiplier = ParseRequiredInteger(snapshot.Multiplier, "Multiplier"),
            .Type = If(snapshot.LocalChecked, "Local", "Foreign"),
            .Category = snapshot.Category,
            .Amount = ParseRequiredDouble(snapshot.Amount, "Expense Amount"),
            .VatAmount = ParseVatAmount(snapshot.Invoice, snapshot.VatAmount),
            .Remarks = snapshot.Remarks,
            .Status = snapshot.Status,
            .TotalAmount = ParseRequiredDouble(snapshot.TotalAmount, "Total Amount"),
            .Location = If(Trim(snapshot.Location) = "", "Allowance", Trim(snapshot.Location)),
            .UserID = ParseRequiredInteger(GetArrayValue(reportData, 14), "User ID"),
            .ReportID = GetArrayValue(reportData, 13),
            .WorkWith = snapshot.WorkWith,
            .ServiceNumber = snapshot.ServiceNumber,
            .Instrument = snapshot.Instrument,
            .SerialNumber = snapshot.SerialNumber,
            .MDays = snapshot.MDays,
            .Computation = snapshot.Computation,
            .TotDays = snapshot.TotalDays,
            .Meal = GetArrayValue(mealParts, 0),
            .PaidFor = GetArrayValue(mealParts, 1),
            .PaidEmp = GetArrayValue(mealParts, 2),
            .FareID = ParseNullableLong(GetArrayValue(transportationParts, 0)),
            .FareFrom = GetArrayValue(transportationParts, 1),
            .FareTo = GetArrayValue(transportationParts, 2)
        }
    End Function

    Private Shared Function SplitLegacyDelimitedValue(value As String) As String()
        If String.IsNullOrWhiteSpace(value) Then
            Return New String() {}
        End If

        Return value.Split("/"c)
    End Function

    Private Shared Function GetArrayValue(values As String(), index As Integer) As String
        If values Is Nothing OrElse values.Length <= index Then
            Return String.Empty
        End If

        Return values(index)
    End Function

    Private Shared Function ParseRequiredInteger(value As String, fieldName As String) As Integer
        Dim parsedValue As Integer
        If Integer.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, parsedValue) Then
            Return parsedValue
        End If

        Throw New FormatException(fieldName & " must be a valid whole number.")
    End Function

    Private Shared Function ParseRequiredLong(value As String, fieldName As String) As Long
        Dim parsedValue As Long
        If Long.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, parsedValue) Then
            Return parsedValue
        End If

        Throw New FormatException(fieldName & " must be a valid whole number.")
    End Function

    Private Shared Function ParseNullableLong(value As String) As Nullable(Of Long)
        If String.IsNullOrWhiteSpace(value) Then
            Return Nothing
        End If

        Dim parsedValue As Long
        If Long.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, parsedValue) Then
            Return parsedValue
        End If

        Throw New FormatException("Fare ID must be a valid whole number.")
    End Function

    Private Shared Function ParseRequiredDouble(value As String, fieldName As String) As Double
        Dim parsedValue As Double
        If Double.TryParse(value, NumberStyles.Float Or NumberStyles.AllowThousands, CultureInfo.CurrentCulture, parsedValue) Then
            Return parsedValue
        End If

        Throw New FormatException(fieldName & " must be a valid number.")
    End Function

    Private Shared Function ParseVatAmount(invoice As String, vatAmount As String) As Nullable(Of Double)
        If String.IsNullOrWhiteSpace(invoice) Then
            Return Nothing
        End If

        Return ParseRequiredDouble(vatAmount, "VAT Amount")
    End Function
End Class
