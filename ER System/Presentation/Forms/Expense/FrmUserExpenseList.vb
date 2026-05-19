Public Class FrmUserExpenseList
    Dim comboClick As Integer = 0
    Public perdiem As String
    Dim checkboxlistSelected As String
    Dim btnFareValidation As String = "0"
    Dim FareComboValidation As String = "0"
    Dim SortNumber As Integer
    Private ReadOnly ClsData As New ClsLoadData

#Region "Sorting"
    Enum mode
        top = 0
        up = 1
        down = 2
        bottom = 3
    End Enum
    Private Sub swapRows(ByVal range As mode)
        Dim iSelectedRow As Integer = -1
        For iTmp = 0 To dgvExpense.Rows.Count - 1
            If dgvExpense.Rows(iTmp).Selected Then
                iSelectedRow = iTmp
                Exit For
            End If
        Next
        If iSelectedRow <> -1 Then
            Dim sTmp(18) As String
            For iTmp = 0 To dgvExpense.Columns.Count - 1
                sTmp(iTmp) = dgvExpense.Rows(iSelectedRow).Cells(iTmp).Value.ToString
            Next
            Dim iNewRow As Integer
            If range = mode.down Then
                iNewRow = iSelectedRow + 1
            ElseIf range = mode.up Then
                iNewRow = iSelectedRow - 1
            End If
            If range = mode.up Or range = mode.down Then
                For iTmp = 0 To dgvExpense.Columns.Count - 1
                    dgvExpense.Rows(iSelectedRow).Cells(iTmp).Value = dgvExpense.Rows(iNewRow).Cells(iTmp).Value
                    dgvExpense.Rows(iNewRow).Cells(iTmp).Value = sTmp(iTmp)
                Next
                toSelect(iNewRow)
            ElseIf range = mode.top Or range = mode.bottom Then
                reshuffleRows(sTmp, iSelectedRow, range)
            End If
        End If
    End Sub
    Private Sub toSelect(ByVal iNewRow As Integer)
        dgvExpense.Rows(iNewRow).Selected = True
    End Sub
    Private Sub reshuffleRows(ByVal sTmp() As String, ByVal iSelectedRow As Integer, ByVal Range As mode)
        If Range = mode.top Then
            Dim iFirstRow As Integer = 0
            If iSelectedRow > iFirstRow Then
                For iTmp = iSelectedRow To 1 Step -1
                    For iCol = 0 To dgvExpense.Columns.Count - 1
                        dgvExpense.Rows(iTmp).Cells(iCol).Value = dgvExpense.Rows(iTmp - 1).Cells(iCol).Value
                    Next
                Next
                For iCol = 0 To dgvExpense.Columns.Count - 1
                    dgvExpense.Rows(iFirstRow).Cells(iCol).Value = sTmp(iCol).ToString
                Next
                toSelect(iFirstRow)
            End If
        Else
            Dim iLastRow = dgvExpense.Rows.Count - 1
            If iSelectedRow < iLastRow Then
                For iTmp = iSelectedRow To iLastRow - 1
                    For iCol = 0 To dgvExpense.Columns.Count - 1
                        dgvExpense.Rows(iTmp).Cells(iCol).Value = dgvExpense.Rows(iTmp + 1).Cells(iCol).Value
                    Next
                Next
                For iCol = 0 To dgvExpense.Columns.Count - 1
                    dgvExpense.Rows(iLastRow).Cells(iCol).Value = sTmp(iCol).ToString
                Next
                toSelect(iLastRow)
            End If
        End If
    End Sub
#End Region
    Private Sub txtCategory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles txtCategory.SelectedIndexChanged
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
        Call OpeningLead(myERData(14), myERData(16))
    End Sub
    Private Sub TransportationDataValidation(ByVal ExpenseID As String)
        Call MyFare()
        If ExpenseID <> Nothing Then
            Dim myERData As String()
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
            If myERData.Length <> 0 Then
                CBBFare.SelectedValue = myERData(1)
                txtFrom.Text = myERData(2)
                txtTo.Text = myERData(3)
                BTNEditCategory.Enabled = False
            Else
                CBBFare.SelectedIndex = 0
                txtFrom.Clear()
                txtTo.Clear()
                BTNEditCategory.Enabled = False
            End If
        Else
            Dim myERData As String()
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
            If myERData.Length <> 0 Then
                CBBFare.SelectedValue = myERData(1)
                txtFrom.Text = myERData(2)
                txtTo.Text = myERData(3)
            Else
                CBBFare.SelectedIndex = 0
                txtFrom.Clear()
                txtTo.Clear()
                BTNEditCategory.Enabled = False
            End If
            txtExpenseAmount.Clear()
            txtTotal.Clear()
        End If
    End Sub
    Private Sub MyFare()
        Dim dtLoadFare As New DataTable
        dtLoadFare = LoadFare()
        With CBBFare
            .DataSource = dtLoadFare
            .ValueMember = "id"
            .DisplayMember = "FareName"
        End With
    End Sub
    Private Sub MealDataValidation()
        If modLoadingData.WorkWith <> "NONE" And modLoadingData.WorkWith <> "" Then
            GetWorkWith(txtWorkWith.Text)
            If ModDataStore.transactionID <> Nothing Then

                'If ModDataStore.transactionID <> "" And LoadUserExpenseMeal(ModDataStore.transactionID) <> 0 Then
                '    SetUserMealExpenseItem()
                'Else
                '    If modLoadingData.GetUserMeal <> "" Then
                '        GetUserExpenseMeal()
                '        CBBPaidFor.Enabled = True
                '    Else
                '        SetUserMealExpenseItem()
                '    End If
                'End If
            Else
                If modLoadingData.GetUserMeal <> "" Then
                    SetUserMealExpenseItem()
                Else
                    GetUserExpenseMeal()
                End If
                If CLBMeals.CheckedIndices.Count = 1 Then
                    CBBPaidFor.Enabled = True
                Else
                    CBBPaidFor.Enabled = False
                End If
            End If
            CBBPaidFor.Visible = True
            CLBPaidBill.Visible = True
        ElseIf modLoadingData.WorkWith = "NONE" Then
            GetWorkWith(txtWorkWith.Text)
            If ModDataStore.transactionID <> Nothing Then
                'If ModDataStore.transactionID <> "" And LoadUserExpenseMeal(ModDataStore.transactionID) <> 0 Then
                '    SetUserMealExpenseItem()
                '    Call UserMealSettingsWithOutWorkWith()
                'Else
                '    GetUserExpenseMeal()
                '    Call UserMealSettingsWithOutWorkWith()
                'End If
            Else
                GetUserExpenseMeal()
                Call UserMealSettingsWithOutWorkWith()
            End If
        End If
    End Sub
    Private Sub UserMealSettingsWithOutWorkWith()
        CBBPaidFor.Enabled = False
        CBBPaidFor.Visible = False
        CLBPaidBill.Visible = False
        CLBMeals.Enabled = True
    End Sub
    Private Sub GetUserExpenseMeal()
        If modLoadingData.UserExpenseMeal = "" Then
            CLBMeals.SetItemChecked(0, False)
            CLBMeals.SetItemChecked(1, True)
        End If
    End Sub
    Private Sub GetWorkWith(ByVal WorkWith As String)
        If WorkWith = "NONE" Then
            CLBPaidBill.Items.Clear()
        Else
            CLBPaidBill.Items.Clear()
            CLBPaidBill.BeginUpdate()
            If WorkWith.Split("/").Length = 0 Then
                CLBPaidBill.Items.Add(WorkWith)
            Else
                For Each item In WorkWith.Split("/")
                    CLBPaidBill.Items.Add(item)
                Next
            End If
            CLBPaidBill.EndUpdate()
        End If
    End Sub
    Private Sub SetUserMealExpenseItem()
        If modLoadingData.GetUserMeal.Contains("&") = True Then
            Call ResetMeal()
            For x = 0 To modLoadingData.GetUserMeal.Split("&").Count - 1
                If modLoadingData.GetUserMeal.Split("&")(x).Split("^")(1) = "Dinner" Then
                    CBDinnerOTMeal.Checked = True
                    RBDinner.Checked = True
                ElseIf modLoadingData.GetUserMeal.Split("&")(x).Split("^")(1) = "OT Meal" Then
                    CBDinnerOTMeal.Checked = True
                    RBOTMeal.Checked = True
                Else
                    CLBMeals.SetItemChecked(modLoadingData.GetUserMeal.Split("&")(x).Split("^")(0), True)
                End If
                'CLBMeals.SetItemChecked(modLoadingData.GetUserMeal.Split("&")(x).Split("^")(0), True)
            Next
        ElseIf modLoadingData.GetUserPaidFor = 0 Then
            Call ResetMeal()
            If modLoadingData.GetUserMeal.Split("^")(1) = "Dinner" Then
                CBDinnerOTMeal.Checked = True
                RBDinner.Checked = True
                CBBPaidFor.Enabled = True
            ElseIf modLoadingData.GetUserMeal.Split("^")(1) = "OT Meal" Then
                CBDinnerOTMeal.Checked = True
                RBOTMeal.Checked = True
                CBBPaidFor.Enabled = True
            Else
                CLBMeals.SetItemChecked(modLoadingData.GetUserMeal.Split("^")(0), True)
                CBBPaidFor.Enabled = True
            End If
            'CLBMeals.SetItemChecked(modLoadingData.GetUserMeal.Split("^")(0), True)
            'CBBPaidFor.Enabled = True
        ElseIf modLoadingData.GetUserPaidFor = 1 Then
            'Call ResetMeal()
            'CLBMeals.SetItemChecked(modLoadingData.GetUserMeal.Split("^")(0), True)
            'CBBPaidFor.Checked = True
            'For x = 0 To modLoadingData.GetUserPaidEmp.Split("&").Count - 1
            '    CLBPaidBill.SetItemChecked(modLoadingData.GetUserPaidEmp.Split("&")(x).Split("^")(0), True)
            'Next
            Call ResetMeal()
            If modLoadingData.GetUserMeal.Split("^")(1) = "Dinner" Then
                CBDinnerOTMeal.Checked = True
                RBDinner.Checked = True
                CBBPaidFor.Checked = True
                CBBPaidFor.Enabled = False
            ElseIf modLoadingData.GetUserMeal.Split("^")(1) = "OT Meal" Then
                CBDinnerOTMeal.Checked = True
                RBOTMeal.Checked = True
                CBBPaidFor.Checked = True
                CBBPaidFor.Enabled = False
            Else
                CLBMeals.SetItemChecked(modLoadingData.GetUserMeal.Split("^")(0), True)
                CBBPaidFor.Checked = True
                CBBPaidFor.Enabled = False
            End If

            For x = 0 To modLoadingData.GetUserPaidEmp.Split("&").Count - 1
                CLBPaidBill.SetItemChecked(modLoadingData.GetUserPaidEmp.Split("&")(x).Split("^")(0), True)
            Next
        Else
            Call ModFrmUserExpenseListDetails.GetUserExpenseMeal()
        End If
    End Sub
    Private Sub ResetMeal()
        CLBMeals.SetItemChecked(0, False)
        CLBMeals.SetItemChecked(1, False)
    End Sub
    Private Sub FrmUserExpenseList_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If modLoadingData.LoginDeptID = 7 Then
            CBPerdiem.Enabled = False
        Else
            CBPerdiem.Enabled = True
        End If
        If ModDataStore.ExpenseReportEdit = "1" Then
            LoadingExpenseReport(modLoadingData.ReportIDExport, ModDataStore.ReportUserID)
        Else
            TPExpenseReport.Enabled = True
            LoadingExpenseReport(modLoadingData.ReportIDExport, ModDataStore.ReportUserID)
        End If

    End Sub

    Private Sub txtCategory_Click(sender As Object, e As EventArgs) Handles txtCategory.Click
        comboClick = 1
    End Sub

    Private Sub dgvExpense_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvExpense.CellClick
        Try
            SortNumber = dgvExpense.CurrentCell.RowIndex
        Catch ex As Exception
        End Try
    End Sub

    Private Sub btnWorkWith_Click(sender As Object, e As EventArgs) Handles btnWorkWith.Click
        LoadUserWorkWith()
        frmAdditionalInput.dgvUser.Columns(1).ReadOnly = True
        frmAdditionalInput.dgvUser.Columns(2).ReadOnly = True
        frmAdditionalInput.dgvUser.Columns(0).Width = 30
        frmAdditionalInput.Show()
        frmAdditionalInput.BringToFront()
    End Sub

    Private Sub FrmUserExpenseList_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        ModDataStore.ExpenseReportEdit = "0"
        Call ReleasMemory()
    End Sub
    Public Sub AutoSizegrid()
        dgvExpense.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvExpense.Columns(1).DefaultCellStyle.WrapMode = DataGridViewTriState.True
    End Sub

    Private Sub BTNMeals_Click(sender As Object, e As EventArgs) Handles BTNMeals.Click
        If CBBPaidFor.Checked = True And CLBPaidBill.CheckedIndices.Count = 0 Then
            MessageBox.Show("No selected Employee to be Paid for. Please Uncheck 'Paid For'")
        ElseIf CLBMeals.CheckedIndices.Count <> 0 Or CBDinnerOTMeal.Checked = True And
            (RBDinner.Checked = True Or RBOTMeal.Checked = True) Then

            If CBDinnerOTMeal.Checked = True And (RBDinner.Checked = False And RBOTMeal.Checked = False) Then
                MsgBox("You've checked the 'Check for Dinner or OT Meal'. Please Choose either Dinner or OT Meal")
            Else
                Call BTNMealClickExpenseListDetails()
            End If
        Else
            MessageBox.Show("Unable to Proceed. Please Select Meal/s")
        End If
    End Sub
    Private Sub SetUserExpenseMeal(ByVal CLBMeals As CheckedListBox, ByVal PaidFor As String, ByVal PaidEmp As CheckedListBox)
        Dim UserMealExpense As String = ""
        Dim UserPaidExpense As String = ""
        Dim UserPaidMeal As String = ""
        For index = 0 To CLBMeals.CheckedIndices.Count - 1
            UserMealExpense = IIf(UserMealExpense = "", "", UserMealExpense & "&") & CLBMeals.CheckedIndices.Item(index) & "^" & CLBMeals.CheckedItems.Item(index)
            GetUserMealExpense(CLBMeals.CheckedItems.Item(index))
        Next
        For index = 0 To PaidEmp.CheckedIndices.Count - 1
            UserPaidExpense = IIf(UserPaidExpense = "", "", UserPaidExpense & "&") & PaidEmp.CheckedIndices.Item(index) & "^" & PaidEmp.CheckedItems.Item(index)
            LoadUserMealRate(PaidEmp.CheckedItems.Item(index), UserMealExpense.Split("^")(1))
            UserPaidMeal = IIf(UserPaidMeal = "", "", UserPaidMeal & ",") & PaidEmp.CheckedItems.Item(index) & "-" & ModDataStore.UserWorkWithExpense
        Next
        ModDataStore.UserPaidMeal = UserPaidMeal
        modLoadingData.GetUserMeal = UserMealExpense
        modLoadingData.GetUserPaidFor = PaidFor
        modLoadingData.GetUserPaidEmp = UserPaidExpense
        modLoadingData.UserExpenseMeal = UserMealExpense + "/" + PaidFor + "/" & IIf(UserPaidExpense = "", "", UserPaidExpense)
    End Sub
    Private Sub GetUserMealExpense(ByVal MealName As String)
        If MealName = "Breakfast" Then
            UserTotalExpenseAmount = IIf(UserTotalExpenseAmount = 0.0, 0.0, UserTotalExpenseAmount) + LoginMealBF
        ElseIf MealName = "Lunch" Then
            UserTotalExpenseAmount = IIf(UserTotalExpenseAmount = 0.0, 0.0, UserTotalExpenseAmount) + LoginMealLunch
        ElseIf MealName = "Dinner" Then
            UserTotalExpenseAmount = IIf(UserTotalExpenseAmount = 0.0, 0.0, UserTotalExpenseAmount) + LoginMealDinner
        ElseIf MealName = "OT Meal" Then
            UserTotalExpenseAmount = IIf(UserTotalExpenseAmount = 0.0, 0.0, UserTotalExpenseAmount) + LoginMealOTMeal
        End If

    End Sub

    Private Sub CLBMeals_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CLBMeals.SelectedIndexChanged
        Dim count As Integer = 0
        If modLoadingData.WorkWith <> "NONE" Then
            If CLBMeals.CheckedIndices.Count = 1 Then
                If CBDinnerOTMeal.Checked = True Then
                    CBBPaidFor.Enabled = False
                Else
                    CBBPaidFor.Enabled = True
                End If
            ElseIf CLBMeals.CheckedIndices.Count = 0 Then
                If CBDinnerOTMeal.Checked = True Then
                    CBBPaidFor.Enabled = True
                Else
                    CBBPaidFor.Enabled = False
                End If
            ElseIf CLBMeals.CheckedIndices.Count >= 0 Then
                CBBPaidFor.Enabled = False
            End If
        End If
    End Sub

    Private Sub CBBPaidFor_CheckedChanged(sender As Object, e As EventArgs) Handles CBBPaidFor.CheckedChanged
        If CBBPaidFor.Checked = True Then
            CLBPaidBill.Enabled = True
            CLBMeals.Enabled = False
            CBDinnerOTMeal.Enabled = False
            RBDinner.Enabled = False
            RBOTMeal.Enabled = False
        Else
            UncheckPaidBill()
            CLBPaidBill.Enabled = False
            CLBMeals.Enabled = True
            CBDinnerOTMeal.Enabled = True
            RBDinner.Enabled = True
            RBOTMeal.Enabled = True
        End If
    End Sub
    Private Sub UncheckPaidBill()
        For x = 0 To CLBPaidBill.CheckedIndices.Count - 1
            CLBPaidBill.SetItemChecked(x, False)
        Next
    End Sub
    Private Sub CLBPaidBill_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CLBPaidBill.SelectedIndexChanged
        If CLBPaidBill.CheckedIndices.Count = 0 Then
            CBBPaidFor.Enabled = True
        Else
            CBBPaidFor.Enabled = False
        End If
    End Sub

    Private Sub BTNTransportation_Click(sender As Object, e As EventArgs) Handles BTNTransportation.Click
        If txtFrom.Text = "" Or txtTo.Text = "" Then
            MsgBox("Please fill all Fields")
        Else
            GBTransportation.Visible = False
            txtParticulars.Text = txtFrom.Text + " To " + txtTo.Text + " (" + CBBFare.Text + ")"
            TPExpenseReport.Enabled = True
            ModDataStore.GetUserFareID = CBBFare.SelectedValue
            ModDataStore.GetUserFareFrom = txtFrom.Text
            ModDataStore.GetUserFareTo = txtTo.Text
            ModDataStore.UserExpenseTransportation = CBBFare.SelectedValue.ToString() + "/" + txtFrom.Text + "/" + txtTo.Text
            txtExpenseAmount.Enabled = True
            txtExpenseAmount.Select()
        End If
        Me.txtTotal.Select()
        Call ModFrmUserExpenseListDetails.OnOffControl(True)
        If ModDataStore.transactionID <> "" Then
            btnExpenseSave.Visible = False
            btnExpenseUpdate.Visible = True
        Else
            btnExpenseSave.Visible = True
            btnExpenseUpdate.Visible = False
        End If
    End Sub

    Private Sub btnExpenseSave_Click(sender As Object, e As EventArgs) Handles btnExpenseSave.Click
        If txtParticulars.Text = Nothing Or txtExpenseAmount.Text = Nothing Or txtCategory.SelectedItem = "" Then
            MsgBox("Please fill in the Particulars/Expense Amount/Category")
        ElseIf RbLocal.Checked = False And RBForeign.Checked = False Then
            MsgBox("Please Select Type")
        ElseIf txtCategory.SelectedItem = Nothing Then
            MsgBox("Please Select Category")
        ElseIf txtWorkWith.Text = "" Then
            MsgBox("Please fill the WorkWith")
        Else
            AddExpenseReport()
            If MessageBox.Show(
                    "Data Saved. You Want to Erase the Details Above?",
                    "Delete Details Above?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) = DialogResult.Yes Then
                ModFrmUserExpenseListDetails.ClearAllExpenseData()
            Else
                ModFrmUserExpenseListDetails.clearExpenseData()
            End If
            LoadingExpenseReport(modLoadingData.ReportIDExport, ModDataStore.ReportUserID)
            txtParticulars.Focus()
            dgvExpense.Enabled = True
            comboClick = 0
        End If
        ModDataStore.transactionID = ""
        txtStatus.SelectedIndex = 0
        txtCategory.Enabled = True
    End Sub
    Private Sub AddExpenseReport()
        AddExpense(dtpExpenseDate.Text, IIf(CBPerdiem.Checked, "1", "0"), txtParticulars.Text,
                   txtInvoice.Text, txtMultiplier.Text, IIf(RbLocal.Checked = True, "Local", "Foreign"), txtCategory.Text,
                   txtExpenseAmount.Text, txtRemarks.Text, txtStatus.Text, txtTotal.Text,
                   IIf(Trim(Trim(txtLocation.Text)) = "", "Allowance", Trim(Trim(Trim(txtLocation.Text)))),
                   ModDataStore.ReportUserID, modLoadingData.ReportIDExport, IIf(txtServiceNumber.Text = "", "N/A", txtServiceNumber.Text),
                   IIf(txtInstrument.Text = "", "N/A", txtInstrument.Text), IIf(txtSerialNumber.Text = "", "N/A", txtSerialNumber.Text), txtWorkWith.Text,
                   modLoadingData.UserExpenseMeal, ModDataStore.UserExpenseTransportation)
    End Sub
    Private Sub clearExpenseData()
        txtParticulars.Clear()
        txtType.SelectedItem = Nothing
        txtCategory.SelectedItem = Nothing
        txtExpenseAmount.Clear()
        txtMultiplier.Text = "1"
        txtTotal.Text = ""
        txtInvoice.Clear()
        txtRemarks.Clear()
        CBPerdiem.Checked = False
        btnExpenseUpdate.Visible = False
        btnExpenseSave.Visible = True
        modLoadingData.WorkWith = ""
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        ModFrmUserExpenseListDetails.ClearExpenseDataDetails(transactionID, comboClick)
        btnExpenseUpdate.Visible = False
        btnExpenseSave.Visible = True
    End Sub
    Private Sub clearExpenseData1()
        txtParticulars.Clear()
        txtType.SelectedItem = Nothing
        txtCategory.SelectedItem = Nothing
        txtExpenseAmount.Clear()
        txtMultiplier.Text = "1"
        txtTotal.Text = ""
        txtInvoice.Clear()
        txtRemarks.Clear()
        CBPerdiem.Checked = False
        btnExpenseUpdate.Visible = False
        btnExpenseSave.Visible = True
        txtServiceNumber.Clear()
        txtInstrument.Clear()
        txtSerialNumber.Clear()
        txtWorkWith.Clear()
        modLoadingData.WorkWith = ""
        txtLocation.Clear()
    End Sub
    Private Sub btnExpenseUpdate_Click(sender As Object, e As EventArgs) Handles btnExpenseUpdate.Click
        If txtParticulars.Text = Nothing Or txtExpenseAmount.Text = Nothing Or txtCategory.SelectedItem = "" Then
            MsgBox("Please fill in the Particulars/Expense Amount/Category")
        Else
            UpdateExpense(
                transactionID, dtpExpenseDate.Text, IIf(CBPerdiem.Checked, "1", "0"), txtParticulars.Text,
                txtInvoice.Text, txtMultiplier.Text, IIf(RbLocal.Checked = True, "Local", "Foreign"), txtCategory.Text,
                txtExpenseAmount.Text, txtRemarks.Text, txtStatus.Text, txtTotal.Text,
                IIf(Trim(txtLocation.Text) = "", "Allowance", Trim(txtLocation.Text)), ModDataStore.ReportUserID,
                txtServiceNumber.Text, txtInstrument.Text, txtSerialNumber.Text, txtWorkWith.Text, modLoadingData.UserExpenseMeal, ModDataStore.UserExpenseTransportation)
            AddExpenseHisto(dtpExpenseDate.Text, IIf(CBPerdiem.Checked, "1", "0"), txtParticulars.Text, txtInvoice.Text,
                            txtMultiplier.Text, IIf(RbLocal.Checked = True, "Local", "Foreign"), txtCategory.Text,
                            txtExpenseAmount.Text, txtRemarks.Text, txtStatus.Text, txtTotal.Text,
                            IIf(Trim(txtLocation.Text) = "", "Allowance", Trim(txtLocation.Text)),
                            ModDataStore.ReportUserID, modLoadingData.ReportIDExport, transactionID,
                            txtServiceNumber.Text, txtInstrument.Text, txtSerialNumber.Text, modLoadingData.LoginuserID)
            If MessageBox.Show(
                    "Data Saved. You Want to Erase the Details Above?",
                    "Delete Details Above?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) = DialogResult.Yes Then
                ModFrmUserExpenseListDetails.ClearAllExpenseData()
            Else
                ModFrmUserExpenseListDetails.clearExpenseData()
            End If
            LoadingExpenseReport(modLoadingData.ReportIDExport, ModDataStore.ReportUserID)
            modReuse.SetTextFile(txtWorkWith.Text, txtLocation.Text, txtInstrument.Text, txtSerialNumber.Text, txtServiceNumber.Text)
            txtParticulars.Focus()
            dgvExpense.Enabled = True
            comboClick = 0
        End If
        ModDataStore.transactionID = ""
        txtStatus.SelectedIndex = 0
        txtCategory.Enabled = True
    End Sub

    Private Sub FrmUserExpenseList_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then
            Call ModFrmUserExpenseListDetails.ClearExpenseDataDetails(transactionID, comboClick)
        End If
    End Sub
    'Private Sub ClearExpenseDataDetails()
    '    If GBMeals.Visible = True And GBTransportation.Visible = False Then
    '        GBMeals.Visible = False
    '        TPExpenseReport.Enabled = True
    '        dgvExpense.Enabled = True
    '        CBBPaidFor.Checked = False
    '        CBBPaidFor.Enabled = False
    '        txtCategory.Enabled = False
    '        modLoadingData.UserExpenseMeal = ""
    '        txtCategory.SelectedItem = Nothing
    '        comboClick = 0
    '        GetUserExpenseMeal()
    '        Call OnOffControl(True)
    '    ElseIf GBMeals.Visible = False And GBTransportation.Visible = True Then
    '        GBTransportation.Visible = False
    '        TPExpenseReport.Enabled = True
    '        dgvExpense.Enabled = True
    '        txtCategory.Enabled = True
    '        txtCategory.SelectedItem = Nothing
    '        comboClick = 0
    '        Call OnOffControl(True)
    '    Else
    '        clearExpenseData1()
    '        comboClick = 0
    '        btnExpenseUpdate.Visible = False
    '        btnExpenseSave.Visible = True
    '        dgvExpense.Enabled = True
    '        ModDataStore.transactionID = ""
    '        modLoadingData.UserExpenseMeal = ""
    '        txtCategory.Enabled = True
    '        TPExpenseReport.Enabled = False
    '        GetUserExpenseMeal()
    '    End If
    'End Sub
    Private Sub btnHospital_Click(sender As Object, e As EventArgs) Handles btnHospital.Click
        modLoadingData.DataToLoad = "Hospital"
        frmHistory.Text = "Hospital"
        frmHistory.txtName.Focus()
        frmHistory.ShowDialog()
    End Sub

    Private Sub btnInstrumentHistory_Click(sender As Object, e As EventArgs) Handles btnInstrumentHistory.Click
        modLoadingData.DataToLoad = "Instrument"
        frmHistory.txtName.Focus()
        frmHistory.ShowDialog()
    End Sub

    Private Sub btnSerialNumber_Click(sender As Object, e As EventArgs) Handles btnSerialNumber.Click
        modLoadingData.DataToLoad = "Serial Number"
        frmHistory.txtName.Focus()
        frmHistory.ShowDialog()
    End Sub

    Private Sub btnServiceNo_Click(sender As Object, e As EventArgs) Handles btnServiceNo.Click
        modLoadingData.DataToLoad = "Service Number"
        frmHistory.txtName.Focus()
        frmHistory.ShowDialog()
    End Sub

    Private Sub BTNEditCategory_Click(sender As Object, e As EventArgs) Handles BTNEditCategory.Click
        comboClick = "1"
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
        Call OpeningLead(myERData(14), myERData(16))
    End Sub
    Private Sub OnOffControl(ByVal TF As Boolean)
        lblExpenseAmount.Visible = TF
        lblTotalExpenseAmount.Visible = TF
        lblExpenseRemarks.Visible = TF
        lblExpenseMultiplier.Visible = TF
        lblExpenseInvoice.Visible = TF
        lblExpenseStatus.Visible = TF
        txtExpenseAmount.Visible = TF
        txtTotal.Visible = TF
        txtRemarks.Visible = TF
        txtMultiplier.Visible = TF
        txtInvoice.Visible = TF
        txtStatus.Visible = TF
    End Sub
    Private Sub OpeningLead(ByVal WorkWith As String, ByVal ExpenseID As String)
        If CBPerdiem.Checked = False Then
            If comboClick = 1 Then
                If txtCategory.SelectedIndex = 0 Then
                    If WorkWith = "" Then
                        MsgBox("Please fill WorkWith to Proceed")
                        txtCategory.SelectedItem = Nothing
                    Else
                        Call ModFrmUserExpenseListDetails.OnOffControl(False)
                        GBTransportation.Location = New Point(99, 307)
                        GBTransportation.Visible = True
                        GBMeals.Visible = False
                        GBTransportation.BringToFront()
                        txtCategory.Enabled = False
                        BTNEditCategory.Enabled = True
                        btnExpenseUpdate.Visible = False
                        btnExpenseSave.Visible = False
                        Call TransportationDataValidation(ExpenseID)
                    End If
                ElseIf txtCategory.SelectedIndex = 1 Then
                    If WorkWith = "" Then
                        MsgBox("Please fill WorkWith to Proceed")
                        txtCategory.SelectedItem = Nothing
                    Else
                        Call ModFrmUserExpenseListDetails.OnOffControl(False)
                        GBMeals.Location = New Point(99, 307)
                        GBMeals.Visible = True
                        GBTransportation.Visible = False
                        GBMeals.BringToFront()
                        Call MealDataValidation()
                        txtCategory.Enabled = False
                        BTNEditCategory.Enabled = True
                        btnExpenseUpdate.Visible = False
                        btnExpenseSave.Visible = False
                    End If
                ElseIf txtCategory.SelectedIndex = 4 Then
                    txtParticulars.ReadOnly = False
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Text = ""
                    txtParticulars.Select()
                ElseIf txtCategory.SelectedIndex = 2 Then
                    txtParticulars.ReadOnly = True
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtParticulars.Select()
                ElseIf txtCategory.SelectedIndex = 3 Then
                    txtParticulars.ReadOnly = True
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtParticulars.Select()
                Else
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Text = txtCategory.SelectedItem
                    GBMeals.Visible = False
                    GBTransportation.Visible = False
                    txtExpenseAmount.Select()
                End If
            End If
        Else
            If txtCategory.SelectedIndex = 0 Then
                If WorkWith = "" Then
                    MsgBox("Please fill WorkWith to Proceed")
                Else
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtExpenseAmount.Text = modLoadingData.LoginTransportation
                    txtExpenseAmount.Enabled = False
                    BTNEditCategory.Enabled = False
                End If
            ElseIf txtCategory.SelectedIndex = 1 Then
                If WorkWith = "" Then
                    MsgBox("Please fill WorkWith to Proceed")
                Else
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtExpenseAmount.Text = modLoadingData.LoginMealLunch
                    txtExpenseAmount.Enabled = False
                    BTNEditCategory.Enabled = False
                End If
            End If
        End If
    End Sub

    Private Sub BTNMealClose_Click(sender As Object, e As EventArgs) Handles BTNMealClose.Click
        Call OnOffControl(True)
        If transactionID = "" Then
            txtCategory.SelectedItem = Nothing
        End If
        GBMeals.Visible = False
        txtCategory.Enabled = True
        btnExpenseSave.Visible = False
        btnExpenseUpdate.Visible = True
    End Sub

    Private Sub BTNFareClose_Click(sender As Object, e As EventArgs) Handles BTNFareClose.Click
        Call ModFrmUserExpenseListDetails.OnOffControl(True)
        If transactionID = "" Then
            txtCategory.SelectedItem = Nothing
        End If
        GBTransportation.Visible = False
        txtCategory.Enabled = True
        btnExpenseSave.Visible = False
        btnExpenseUpdate.Visible = True
        comboClick = "0"
    End Sub

    Private Sub BTNAddFare_Click(sender As Object, e As EventArgs) Handles BTNAddFare.Click
        Call InsertFare(CBBFare.Text)
        CBBFare.DropDownStyle = ComboBoxStyle.DropDownList
        FareComboValidation = "0"
        BTNAddFare.Text = "add"
        Dim dtLoadFare As New DataTable
        dtLoadFare = LoadFare()
        With CBBFare
            .DataSource = dtLoadFare
            .DisplayMember = "FareName"
            .ValueMember = "id"
        End With
    End Sub

    Private Sub CBPerdiem_CheckedChanged(sender As Object, e As EventArgs) Handles CBPerdiem.CheckedChanged
        If CBPerdiem.Checked = True Then
            Call ModFrmUserExpenseListDetails.CBPerdiemStatusTrue(ModDataStore.transactionID)
        Else
            Call ModFrmUserExpenseListDetails.CBPerdiemStatusFalse(ModDataStore.transactionID)
        End If
    End Sub
    Private Sub txtMultiplier_TextChanged(sender As Object, e As EventArgs) Handles txtMultiplier.TextChanged
        txtTotal.Text = Math.Round(Val(txtExpenseAmount.Text) * Val(txtMultiplier.Text))
    End Sub
    Private Sub txtExpenseAmount_TextChanged(sender As Object, e As EventArgs) Handles txtExpenseAmount.TextChanged
        txtTotal.Text = Math.Round(Val(txtExpenseAmount.Text) * Val(txtMultiplier.Text))
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles BTNUp.Click
        LoadingExpenseCount(modLoadingData.ReportIDExport)
        If modLoadingData.ExpenseCount = Nothing Or modLoadingData.ExpenseCount = "" Then
        Else
            If dgvExpense.Rows(0).Selected = True Then
            Else
                Try
                    swapRows(mode.up)
                    Sorting()
                    SortNumber = Val(SortNumber - 1)
                    dgvExpense.ClearSelection()
                    dgvExpense.Rows(SortNumber).Selected = True
                Catch ex As Exception
                End Try
            End If
        End If
    End Sub
    Private Sub Sorting()
        Try
            Dim sqlSaveSort As New SqlClient.SqlCommand
            Dim a As Integer
            With sqlSaveSort
                .Connection = SQLConnection
                For a = 0 To dgvExpense.Rows.Count - 1
                    .CommandText = "update tbExpenseDetails set Sort ='" & a & "' where ID='" & dgvExpense.Rows(a).Cells(0).Value & "'"
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                Next
                LoadingExpenseReport(modLoadingData.ReportIDExport, ModDataStore.ReportUserID)
            End With
        Catch ex As Exception
        End Try
    End Sub

    Private Sub BTNDown_Click(sender As Object, e As EventArgs) Handles BTNDown.Click
        LoadingExpenseCount(modLoadingData.ReportIDExport)
        If modLoadingData.ExpenseCount = Nothing Or modLoadingData.ExpenseCount = "" Then
        Else
            Dim drows As Integer
            drows = dgvExpense.Rows.Count - 1
            If dgvExpense.Rows(drows).Selected = True Then

            Else
                Try
                    swapRows(mode.down)
                    Sorting()
                    SortNumber = Val(SortNumber + 1)
                    dgvExpense.ClearSelection()
                    dgvExpense.Rows(SortNumber).Selected = True
                Catch ex As Exception
                End Try
            End If
        End If
    End Sub

    Private Sub dgvExpense_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvExpense.CellDoubleClick
        comboClick = "0"
        Call ModFrmUserExpenseListDetails.ClearExpenseDataDetails(transactionID, comboClick)
        Dim type As String
        Try
            If e.RowIndex < 0 Then
                Exit Sub
            Else
                perdiem = dgvExpense.Rows(e.RowIndex).Cells("ExpensePerdiem").Value
                transactionID = dgvExpense.Rows(e.RowIndex).Cells("ID").Value
                txtLocation.Text = dgvExpense.Rows(e.RowIndex).Cells("Location").Value
                txtExpenseAmount.Text = dgvExpense.Rows(e.RowIndex).Cells("ExpenseAmount").Value
                txtMultiplier.Text = dgvExpense.Rows(e.RowIndex).Cells("ExpenseMultiplier").Value
                txtInvoice.Text = dgvExpense.Rows(e.RowIndex).Cells("ExpenseInvoice").Value
                txtStatus.Text = dgvExpense.Rows(e.RowIndex).Cells("ExpenseStatus").Value
                txtRemarks.Text = dgvExpense.Rows(e.RowIndex).Cells("ExpenseRemarks").Value
                txtCategoryBackUpData = dgvExpense.Rows(e.RowIndex).Cells("Category").Value
                txtParticularsBackUpData = dgvExpense.Rows(e.RowIndex).Cells("Particulars").Value
                txtCategory.Text = txtCategoryBackUpData
                txtParticulars.Text = txtParticularsBackUpData
                dtpExpenseDate.Text = dgvExpense.Rows(e.RowIndex).Cells("Date").Value
                type = dgvExpense.Rows(e.RowIndex).Cells("Type").Value
                If type = "Local" Then
                    RbLocal.Checked = True
                Else
                    RBForeign.Checked = True
                End If
                txtServiceNumber.Text = dgvExpense.Rows(e.RowIndex).Cells("ServiceNumber").Value
                txtInstrument.Text = IIf(IsDBNull(dgvExpense.Rows(e.RowIndex).Cells("Instrument").Value), "", dgvExpense.Rows(e.RowIndex).Cells("Instrument").Value)
                txtSerialNumber.Text = IIf(IsDBNull(dgvExpense.Rows(e.RowIndex).Cells("Serial Number").Value), "", dgvExpense.Rows(e.RowIndex).Cells("Serial Number").Value)
                txtWorkWith.Text = dgvExpense.Rows(e.RowIndex).Cells("WorkWith").Value
                modLoadingData.WorkWith = dgvExpense.Rows(e.RowIndex).Cells("WorkWith").Value
            End If
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
        btnExpenseSave.Visible = False
        btnExpenseUpdate.Visible = True
        TPExpenseReport.Enabled = True
        If perdiem = "0" Or perdiem = "" Then
            CBPerdiem.Checked = False
            BTNEditCategory.Enabled = True
        Else
            CBPerdiem.Checked = True
            BTNEditCategory.Enabled = False
        End If
        If txtCategory.SelectedItem <> "" Then
            txtCategory.Enabled = False
        End If
        Call ModFrmUserExpenseListDetails.GetUserExpenseMeal()
    End Sub
    Private Sub CBDinnerOTMeal_CheckedChanged(sender As Object, e As EventArgs) Handles CBDinnerOTMeal.CheckedChanged
        If CBDinnerOTMeal.Checked = True Then
            RBDinner.Enabled = True
            RBOTMeal.Enabled = True
            If CLBMeals.CheckedIndices.Count = 0 Then
                CBBPaidFor.Enabled = True
            Else
                CBBPaidFor.Enabled = False
                CBBPaidFor.Checked = False
            End If
        Else
            RBDinner.Enabled = False
            RBOTMeal.Enabled = False
            RBDinner.Checked = False
            RBOTMeal.Checked = False
            If CLBMeals.CheckedIndices.Count = 0 Then
                CBBPaidFor.Enabled = False
                CBBPaidFor.Checked = False
            ElseIf CLBMeals.CheckedIndices.Count = 1 Then
                CBBPaidFor.Enabled = True
            End If
        End If
    End Sub

    Private Sub CBBFare_SelectedValueChanged(sender As Object, e As EventArgs) Handles CBBFare.SelectedValueChanged
        If Me.FareComboValidation = "1" Then
            If CBBFare.SelectedValue = 4 Then
                CBBFare.DropDownStyle = ComboBoxStyle.DropDown
                btnFareValidation = "1"
                BTNAddFare.Text = "Save"
                CBBFare.DataSource = Nothing
                CBBFare.Text = ""
                BTNAddFare.Enabled = True
                CBBFare.Select()
            End If
        End If
    End Sub

    Private Sub CBBFare_Click(sender As Object, e As EventArgs) Handles CBBFare.Click
        Me.FareComboValidation = "1"
    End Sub

End Class