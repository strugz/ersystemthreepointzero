Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Public Class AllowanceComputationResult
    Public Property TotalDays As String
    Public Property MinusDays As String
    Public Property Multiplier As String
    Public Property ComputationText As String
End Class

Public Class TransportationResult
    Public Property FareId As String
    Public Property FareName As String
    Public Property FareFrom As String
    Public Property FareTo As String
    Public Property Payload As String
    Public Property Particulars As String
    Public Property AmountEnabled As Boolean
End Class

Public Class MealSelectionResult
    Public Property BreakfastSelected As Boolean
    Public Property LunchSelected As Boolean
    Public Property DinnerSelected As Boolean
    Public Property OtMealSelected As Boolean
    Public Property PaidFor As Boolean
    Public Property PaidEmployeeIndexes As List(Of Integer)
End Class

Public NotInheritable Class AllowanceComputationPopup
    Inherits Form

    Private ReadOnly _txtTotalDays As New TextBox()
    Private ReadOnly _txtMinusDays As New TextBox()
    Private ReadOnly _result As AllowanceComputationResult

    Public ReadOnly Property Result As AllowanceComputationResult
        Get
            Return _result
        End Get
    End Property

    Public Sub New(totalDays As String, minusDays As String)
        _result = New AllowanceComputationResult()

        Text = "Allowance Computation"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        ShowInTaskbar = False
        MaximizeBox = False
        MinimizeBox = False
        ClientSize = New Size(225, 120)
        KeyPreview = True

        Controls.Add(New Label() With {.Text = "# of Days(22)", .Location = New Point(12, 12), .AutoSize = True})
        _txtTotalDays.Location = New Point(12, 30)
        _txtTotalDays.Size = New Size(92, 22)
        _txtTotalDays.Text = totalDays
        Controls.Add(_txtTotalDays)

        Controls.Add(New Label() With {.Text = "( - ) Days", .Location = New Point(118, 12), .AutoSize = True})
        _txtMinusDays.Location = New Point(118, 30)
        _txtMinusDays.Size = New Size(92, 22)
        _txtMinusDays.Text = If(String.IsNullOrWhiteSpace(minusDays), "0", minusDays)
        Controls.Add(_txtMinusDays)

        Dim btnClose As New Button() With {.Text = "Close", .Location = New Point(22, 78), .Size = New Size(87, 24), .DialogResult = DialogResult.Cancel}
        Dim btnDone As New Button() With {.Text = "Done", .Location = New Point(123, 78), .Size = New Size(87, 24)}
        AddHandler btnDone.Click, AddressOf DoneClicked

        Controls.Add(btnClose)
        Controls.Add(btnDone)
        CancelButton = btnClose
        AcceptButton = btnDone
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If e.KeyCode = Keys.Escape Then
            DialogResult = DialogResult.Cancel
            Close()
        End If
    End Sub

    Private Sub DoneClicked(sender As Object, e As EventArgs)
        Dim totalDays As Double
        Dim minusDays As Double

        If Not Double.TryParse(_txtTotalDays.Text, totalDays) Then
            MessageBox.Show("Please enter a valid # of Days value.")
            Return
        End If

        If String.IsNullOrWhiteSpace(_txtMinusDays.Text) Then
            _txtMinusDays.Text = "0"
        End If

        If Not Double.TryParse(_txtMinusDays.Text, minusDays) Then
            MessageBox.Show("Please enter a valid - Days value.")
            Return
        End If

        If totalDays - minusDays <= 0 Then
            MessageBox.Show("0 or Less than 0 Multiplier is Not Allowed!")
            Return
        End If

        Result.TotalDays = _txtTotalDays.Text
        Result.MinusDays = _txtMinusDays.Text
        DialogResult = DialogResult.OK
        Close()
    End Sub
End Class

Public NotInheritable Class TransportationPopup
    Inherits Form

    Private ReadOnly _cbbFare As New ComboBox()
    Private ReadOnly _txtFrom As New TextBox()
    Private ReadOnly _txtTo As New TextBox()
    Private ReadOnly _btnAddFare As New Button()
    Private ReadOnly _result As TransportationResult
    Private _isLoading As Boolean

    Public ReadOnly Property Result As TransportationResult
        Get
            Return _result
        End Get
    End Property

    Public Sub New(fares As DataTable, selectedFareId As Object, fareFrom As String, fareTo As String)
        _result = New TransportationResult()

        Text = "Transportation"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        ShowInTaskbar = False
        MaximizeBox = False
        MinimizeBox = False
        ClientSize = New Size(224, 198)
        KeyPreview = True

        Controls.Add(New Label() With {.Text = "Fare", .Location = New Point(12, 12), .AutoSize = True})
        _cbbFare.DropDownStyle = ComboBoxStyle.DropDownList
        _cbbFare.Location = New Point(12, 30)
        _cbbFare.Size = New Size(148, 21)
        AddHandler _cbbFare.SelectedValueChanged, AddressOf FareSelectedValueChanged
        AddHandler _cbbFare.Click, Sub() FareComboValidation = "1"
        Controls.Add(_cbbFare)

        _btnAddFare.Text = "Save"
        _btnAddFare.Location = New Point(166, 29)
        _btnAddFare.Size = New Size(46, 23)
        _btnAddFare.Enabled = False
        AddHandler _btnAddFare.Click, AddressOf AddFareClicked
        Controls.Add(_btnAddFare)

        Controls.Add(New Label() With {.Text = "From", .Location = New Point(12, 58), .AutoSize = True})
        _txtFrom.CharacterCasing = CharacterCasing.Upper
        _txtFrom.Location = New Point(12, 76)
        _txtFrom.Size = New Size(200, 22)
        Controls.Add(_txtFrom)

        Controls.Add(New Label() With {.Text = "To", .Location = New Point(12, 104), .AutoSize = True})
        _txtTo.CharacterCasing = CharacterCasing.Upper
        _txtTo.Location = New Point(12, 122)
        _txtTo.Size = New Size(200, 22)
        Controls.Add(_txtTo)

        Dim btnClose As New Button() With {.Text = "Close", .Location = New Point(24, 162), .Size = New Size(87, 24), .DialogResult = DialogResult.Cancel}
        Dim btnDone As New Button() With {.Text = "Done", .Location = New Point(125, 162), .Size = New Size(87, 24)}
        AddHandler btnDone.Click, AddressOf DoneClicked
        Controls.Add(btnClose)
        Controls.Add(btnDone)
        CancelButton = btnClose
        AcceptButton = btnDone

        LoadFares(fares)

        _isLoading = True
        If selectedFareId IsNot Nothing Then
            _cbbFare.SelectedValue = selectedFareId
        ElseIf _cbbFare.Items.Count > 0 Then
            _cbbFare.SelectedIndex = 0
        End If

        _txtFrom.Text = fareFrom
        _txtTo.Text = fareTo
        _isLoading = False
        ApplyFareSelection()
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If e.KeyCode = Keys.Escape Then
            DialogResult = DialogResult.Cancel
            Close()
        End If
    End Sub

    Private Sub LoadFares(fares As DataTable)
        _cbbFare.DataSource = fares
        _cbbFare.ValueMember = "id"
        _cbbFare.DisplayMember = "FareName"
    End Sub

    Private Sub AddFareClicked(sender As Object, e As EventArgs)
        InsertFare(_cbbFare.Text)
        _cbbFare.DropDownStyle = ComboBoxStyle.DropDownList
        FareComboValidation = "0"
        _btnAddFare.Text = "Save"
        _btnAddFare.Enabled = False
        LoadFares(LoadFare())
    End Sub

    Private Sub FareSelectedValueChanged(sender As Object, e As EventArgs)
        If _isLoading Then
            Return
        End If

        ApplyFareSelection()
    End Sub

    Private Sub ApplyFareSelection()
        Dim selectedFareId As String = Convert.ToString(_cbbFare.SelectedValue)

        If selectedFareId = "4" Then
            _txtFrom.Text = "Allowance"
            _txtTo.Text = "Allowance"
            _txtFrom.Enabled = False
            _txtTo.Enabled = False
            _btnAddFare.Enabled = False
        ElseIf FareComboValidation <> "1" Then
            _txtFrom.Enabled = True
            _txtTo.Enabled = True
            _btnAddFare.Enabled = False
        ElseIf selectedFareId = "2" Then
            _cbbFare.DropDownStyle = ComboBoxStyle.DropDown
            FareComboValidation = "1"
            _btnAddFare.Text = "Save"
            _cbbFare.DataSource = Nothing
            _cbbFare.Text = ""
            _btnAddFare.Enabled = True
            _cbbFare.Select()
        Else
            If Not _isLoading Then
                _txtFrom.Text = ""
                _txtTo.Text = ""
            End If

            _txtFrom.Enabled = True
            _txtTo.Enabled = True
            _btnAddFare.Enabled = False
        End If
    End Sub

    Private Sub DoneClicked(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(_txtFrom.Text) OrElse String.IsNullOrWhiteSpace(_txtTo.Text) Then
            MessageBox.Show("Please fill all Fields")
            Return
        End If

        Result.FareId = Convert.ToString(_cbbFare.SelectedValue)
        Result.FareName = _cbbFare.Text
        Result.FareFrom = _txtFrom.Text
        Result.FareTo = _txtTo.Text
        Result.Payload = Result.FareId & "/" & Result.FareFrom & "/" & Result.FareTo

        If Result.FareId = "4" Then
            Result.Particulars = "Transportation"
            Result.AmountEnabled = False
        Else
            Result.Particulars = Result.FareFrom & " To " & Result.FareTo & " (" & Result.FareName & ")"
            Result.AmountEnabled = True
        End If

        DialogResult = DialogResult.OK
        Close()
    End Sub
End Class

Public NotInheritable Class MealSelectionPopup
    Inherits Form

    Private ReadOnly _clbMeals As New CheckedListBox()
    Private ReadOnly _cbDinnerOtMeal As New CheckBox()
    Private ReadOnly _rbDinner As New RadioButton()
    Private ReadOnly _rbOtMeal As New RadioButton()
    Private ReadOnly _cbPaidFor As New CheckBox()
    Private ReadOnly _clbPaidBill As New CheckedListBox()
    Private ReadOnly _expenseDate As String
    Private ReadOnly _username As String
    Private ReadOnly _result As MealSelectionResult
    Private _counter As String = ""

    Public ReadOnly Property Result As MealSelectionResult
        Get
            Return _result
        End Get
    End Property

    Public Sub New(
        expenseDate As String,
        username As String,
        workWith As String,
        breakfastChecked As Boolean,
        lunchChecked As Boolean,
        dinnerChecked As Boolean,
        otMealChecked As Boolean,
        paidForChecked As Boolean,
        paidForEnabled As Boolean,
        paidForVisible As Boolean,
        paidBillVisible As Boolean,
        paidBillItems As IEnumerable(Of String),
        paidBillCheckedIndexes As IEnumerable(Of Integer))

        _expenseDate = expenseDate
        _username = username
        _result = New MealSelectionResult With {.PaidEmployeeIndexes = New List(Of Integer)()}

        Text = "Meals"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        ShowInTaskbar = False
        MaximizeBox = False
        MinimizeBox = False
        ClientSize = New Size(224, 198)
        KeyPreview = True

        _clbMeals.BorderStyle = BorderStyle.None
        _clbMeals.CheckOnClick = True
        _clbMeals.Items.AddRange(New Object() {"Breakfast", "Lunch"})
        _clbMeals.Location = New Point(12, 14)
        _clbMeals.Size = New Size(108, 34)
        _clbMeals.SetItemChecked(0, breakfastChecked)
        _clbMeals.SetItemChecked(1, lunchChecked)
        AddHandler _clbMeals.SelectedValueChanged, AddressOf MealsSelectedValueChanged

        _cbDinnerOtMeal.Text = "Check for Dinner" & Environment.NewLine & "or OT Meal"
        _cbDinnerOtMeal.Location = New Point(12, 66)
        _cbDinnerOtMeal.Size = New Size(113, 34)
        _cbDinnerOtMeal.Checked = dinnerChecked OrElse otMealChecked
        AddHandler _cbDinnerOtMeal.CheckedChanged, AddressOf DinnerOtMealCheckedChanged

        _rbDinner.Text = "Dinner"
        _rbDinner.Location = New Point(12, 104)
        _rbDinner.Enabled = _cbDinnerOtMeal.Checked
        _rbDinner.Checked = dinnerChecked

        _rbOtMeal.Text = "OT Meal"
        _rbOtMeal.Location = New Point(12, 128)
        _rbOtMeal.Enabled = _cbDinnerOtMeal.Checked
        _rbOtMeal.Checked = otMealChecked

        _cbPaidFor.Text = "Paid For"
        _cbPaidFor.Location = New Point(134, 14)
        _cbPaidFor.Size = New Size(78, 20)
        _cbPaidFor.Visible = paidForVisible
        _cbPaidFor.Enabled = paidForEnabled
        _cbPaidFor.Checked = paidForChecked
        AddHandler _cbPaidFor.CheckedChanged, AddressOf PaidForCheckedChanged

        _clbPaidBill.CheckOnClick = True
        _clbPaidBill.Location = New Point(134, 38)
        _clbPaidBill.Size = New Size(78, 108)
        _clbPaidBill.Visible = paidBillVisible
        _clbPaidBill.Enabled = paidForChecked
        For Each item As String In paidBillItems
            _clbPaidBill.Items.Add(item)
        Next
        For Each index As Integer In paidBillCheckedIndexes
            If index >= 0 AndAlso index < _clbPaidBill.Items.Count Then
                _clbPaidBill.SetItemChecked(index, True)
            End If
        Next
        AddHandler _clbPaidBill.SelectedIndexChanged, AddressOf PaidBillSelectedIndexChanged

        If workWith = "NONE" OrElse String.IsNullOrWhiteSpace(workWith) Then
            _cbPaidFor.Enabled = False
            _cbPaidFor.Visible = False
            _clbPaidBill.Visible = False
            _clbMeals.Enabled = True
        End If

        Dim btnClose As New Button() With {.Text = "Close", .Location = New Point(24, 162), .Size = New Size(87, 24), .DialogResult = DialogResult.Cancel}
        Dim btnDone As New Button() With {.Text = "Done", .Location = New Point(125, 162), .Size = New Size(87, 24)}
        AddHandler btnDone.Click, AddressOf DoneClicked

        Controls.AddRange(New Control() {_clbMeals, _cbDinnerOtMeal, _rbDinner, _rbOtMeal, _cbPaidFor, _clbPaidBill, btnClose, btnDone})
        CancelButton = btnClose
        AcceptButton = btnDone
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If e.KeyCode = Keys.Escape Then
            DialogResult = DialogResult.Cancel
            Close()
        End If
    End Sub

    Private Sub PaidForCheckedChanged(sender As Object, e As EventArgs)
        If _cbPaidFor.Checked Then
            _clbPaidBill.Enabled = True
            _clbMeals.Enabled = False
            _cbDinnerOtMeal.Enabled = False
            _rbDinner.Enabled = False
            _rbOtMeal.Enabled = False
        Else
            For index As Integer = 0 To _clbPaidBill.Items.Count - 1
                _clbPaidBill.SetItemChecked(index, False)
            Next

            _clbPaidBill.Enabled = False
            _clbMeals.Enabled = True
            _cbDinnerOtMeal.Enabled = True
            _rbDinner.Enabled = _cbDinnerOtMeal.Checked
            _rbOtMeal.Enabled = _cbDinnerOtMeal.Checked
        End If
    End Sub

    Private Sub PaidBillSelectedIndexChanged(sender As Object, e As EventArgs)
        _cbPaidFor.Enabled = _clbPaidBill.CheckedIndices.Count = 0
    End Sub

    Private Sub DinnerOtMealCheckedChanged(sender As Object, e As EventArgs)
        Dim filedMeals As String = modLoadingData.LoadNotification(_expenseDate, _username, "")

        If filedMeals <> "" Then
            Dim filedParts As String() = filedMeals.Split("/"c)
            For Each filedPart As String In filedParts
                Dim values As String() = filedPart.Split("^"c)
                If values.Length > 2 AndAlso (values(2) = "Dinner" OrElse values(2) = "OT Meal") Then
                    If _counter = "" Then
                        _counter = "1"
                        MessageBox.Show(values(2) & " Meal is Already Filed by " & values(1))
                        _cbDinnerOtMeal.Checked = False
                    Else
                        _counter = ""
                        _cbDinnerOtMeal.Checked = False
                    End If

                    Return
                End If
            Next
        End If

        If _cbDinnerOtMeal.Checked Then
            _rbDinner.Enabled = True
            _rbOtMeal.Enabled = True
            _cbPaidFor.Enabled = _clbMeals.CheckedIndices.Count = 0
            If _clbMeals.CheckedIndices.Count <> 0 Then
                _cbPaidFor.Checked = False
            End If
        Else
            _rbDinner.Enabled = False
            _rbOtMeal.Enabled = False
            _rbDinner.Checked = False
            _rbOtMeal.Checked = False
            _cbPaidFor.Enabled = _clbMeals.CheckedIndices.Count = 1
            If _clbMeals.CheckedIndices.Count = 0 Then
                _cbPaidFor.Checked = False
            End If
        End If
    End Sub

    Private Sub MealsSelectedValueChanged(sender As Object, e As EventArgs)
        Dim filedMeals As String = modLoadingData.LoadNotification(_expenseDate, _username, "")

        If filedMeals <> "" Then
            Dim filedParts As String() = filedMeals.Split("/"c)
            For Each filedPart As String In filedParts
                Dim values As String() = filedPart.Split("^"c)
                If values.Length > 2 AndAlso _clbMeals.SelectedIndex = 0 AndAlso values(2) = "Breakfast" Then
                    MessageBox.Show("Breakfast Meal is Already Filed by " & values(1))
                    _clbMeals.SetItemChecked(0, False)
                ElseIf values.Length > 2 AndAlso _clbMeals.SelectedIndex = 1 AndAlso values(2) = "Lunch" Then
                    MessageBox.Show("Lunch Meal is Already Filed by " & values(1))
                    _clbMeals.SetItemChecked(1, False)
                End If
            Next
        Else
            If _clbMeals.CheckedIndices.Count = 1 Then
                _cbPaidFor.Enabled = Not _cbDinnerOtMeal.Checked
            ElseIf _clbMeals.CheckedIndices.Count = 0 Then
                _cbPaidFor.Enabled = _cbDinnerOtMeal.Checked
            Else
                _cbPaidFor.Enabled = False
            End If
        End If
    End Sub

    Private Sub DoneClicked(sender As Object, e As EventArgs)
        If _cbPaidFor.Checked AndAlso _clbPaidBill.CheckedIndices.Count = 0 Then
            MessageBox.Show("No selected Employee to be Paid for. Please Uncheck 'Paid For'")
            Return
        End If

        If _clbMeals.CheckedIndices.Count = 0 AndAlso Not _cbDinnerOtMeal.Checked Then
            MessageBox.Show("Unable to Proceed. Please Select Meal/s")
            Return
        End If

        If _cbDinnerOtMeal.Checked AndAlso Not _rbDinner.Checked AndAlso Not _rbOtMeal.Checked Then
            MessageBox.Show("You've checked the 'Check for Dinner or OT Meal'. Please Choose either Dinner or OT Meal")
            Return
        End If

        Result.BreakfastSelected = _clbMeals.GetItemChecked(0)
        Result.LunchSelected = _clbMeals.GetItemChecked(1)
        Result.DinnerSelected = _cbDinnerOtMeal.Checked AndAlso _rbDinner.Checked
        Result.OtMealSelected = _cbDinnerOtMeal.Checked AndAlso _rbOtMeal.Checked
        Result.PaidFor = _cbPaidFor.Checked

        For Each checkedIndex As Integer In _clbPaidBill.CheckedIndices
            Result.PaidEmployeeIndexes.Add(checkedIndex)
        Next

        DialogResult = DialogResult.OK
        Close()
    End Sub
End Class
