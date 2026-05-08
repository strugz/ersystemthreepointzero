Module ModFrmUserExpenseListDetails
    Friend Sub BTNMealClickExpenseListDetails()
        Dim checkboxlistSelected As String
        With FrmUserExpenseList
            .GBMeals.Visible = False
            comboClick = 0
            checkboxlistSelected = ""
            UserTotalAmountPaidFor = 0.0
            UserTotalExpenseAmount = 0.0
            For Each item In .CLBMeals.CheckedItems
                checkboxlistSelected = IIf(checkboxlistSelected = "", "", checkboxlistSelected + ", ") + item
            Next
            If .CBDinnerOTMeal.Checked = True Then
                If .RBDinner.Checked = True Then
                    checkboxlistSelected += IIf(checkboxlistSelected = "", "", ", ") & "Dinner"
                Else
                    checkboxlistSelected += IIf(checkboxlistSelected = "", "", ", ") & "OT Meal"
                End If
            End If
            .txtParticulars.Text = .txtCategory.Text + " (" + checkboxlistSelected + ")"
            .TPExpenseReport.Enabled = True
            Call ModFrmUserExpenseListDetails.SetUserExpenseMeal(.CLBMeals, IIf(.CBBPaidFor.Checked = True, 1, 0), .CLBPaidBill)
            .txtExpenseAmount.Text = 0
            .txtExpenseAmount.Text = Val(UserTotalAmountPaidFor + UserTotalExpenseAmount)
            .txtExpenseAmount.Enabled = False
            .txtRemarks.Clear()
            .txtRemarks.Text = ModDataStore.UserPaidMeal
            Call OnOffControl(True)
            If ModDataStore.transactionID = "" Then
                .btnExpenseSave.Visible = True
                .btnExpenseUpdate.Visible = False
            Else
                .btnExpenseSave.Visible = False
                .btnExpenseUpdate.Visible = True
            End If
        End With
    End Sub
    Friend Sub SetUserExpenseMeal(ByVal CLBMeals As CheckedListBox, ByVal PaidFor As String, ByVal PaidEmp As CheckedListBox)
        Dim UserMealExpense As String = ""
        Dim UserPaidExpense As String = ""
        Dim UserPaidMeal As String = ""
        For index = 0 To CLBMeals.CheckedIndices.Count - 1
            UserMealExpense = IIf(UserMealExpense = "", "", UserMealExpense & "&") & CLBMeals.CheckedIndices.Item(index) & "^" & CLBMeals.CheckedItems.Item(index)
            Call ModFrmUserExpenseListDetails.GetUserMealExpense(CLBMeals.CheckedItems.Item(index))
        Next
        If frmEReport.RBDinner.Checked = True Then
            Call ModFrmUserExpenseListDetails.GetUserMealExpense("Dinner")
        Else
            Call ModFrmUserExpenseListDetails.GetUserMealExpense("OT Meal")
        End If
        UserMealExpense = ModFrmUserExpenseListDetails.SetDinnerOTAmount(UserMealExpense)
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
    Friend Function SetDinnerOTAmount(ByVal UserMealExpense As String) As String
        Dim i As Integer = ModFrmUserExpenseListDetails.SplitData(UserMealExpense, "&").Length
        With FrmUserExpenseList
            If .CBDinnerOTMeal.Checked = True Then
                If .RBDinner.Checked = True Then
                    If UserMealExpense = "" Then
                        UserMealExpense = Val(0) & "^" & "Dinner"
                    Else
                        UserMealExpense += "&" & Val(i) & "^" & "Dinner"
                    End If

                Else
                    If UserMealExpense = "" Then
                        UserMealExpense = Val(0) & "^" & "OT Meal"
                    Else
                        UserMealExpense += "&" & Val(i) & "^" & "OT Meal"
                    End If
                End If
            End If
        End With
        Return UserMealExpense
    End Function
    Private Function SplitData(ByVal mySplitData As String, ByVal separator As String) As String()
        SplitData = mySplitData.Split(separator)
    End Function
    Friend Sub GetUserMealExpense(ByVal MealName As String)
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
    Friend Sub OnOffControl(ByVal TF As Boolean)
        With FrmUserExpenseList
            .lblExpenseAmount.Visible = TF
            .lblTotalExpenseAmount.Visible = TF
            .lblExpenseRemarks.Visible = TF
            .lblExpenseMultiplier.Visible = TF
            .lblExpenseInvoice.Visible = TF
            .lblExpenseStatus.Visible = TF
            .txtExpenseAmount.Visible = TF
            .txtTotal.Visible = TF
            .txtRemarks.Visible = TF
            .txtMultiplier.Visible = TF
            .txtInvoice.Visible = TF
            .txtStatus.Visible = TF
            .btnCancel.Visible = TF
            .BTNEditCategory.Enabled = TF
        End With
    End Sub
    Friend Sub ClearAllExpenseData()
        With FrmUserExpenseList
            .CBPerdiem.Checked = False
            .dtpExpenseDate.Value = DateTime.Now
            .txtWorkWith.Text = ""
            modLoadingData.WorkWith = ""
            .txtLocation.Clear()
            .txtInstrument.Clear()
            .txtSerialNumber.Clear()
            .txtServiceNumber.Clear()
            .RbLocal.Checked = True
            .txtCategory.SelectedItem = Nothing
            .txtParticulars.Text = ""
            .txtExpenseAmount.Clear()
            .txtTotal.Clear()
            .txtMultiplier.Text = 1
            .txtInvoice.Clear()
            .txtStatus.SelectedIndex = 0
            .txtRemarks.Text = ""
            .btnExpenseUpdate.Visible = False
            .btnExpenseSave.Visible = True
        End With
    End Sub
    Friend Sub clearExpenseData()
        With FrmUserExpenseList
            .txtParticulars.Clear()
            .txtCategory.SelectedItem = Nothing
            .txtExpenseAmount.Clear()
            .txtMultiplier.Text = "1"
            .txtTotal.Text = ""
            .txtInvoice.Clear()
            .txtRemarks.Clear()
            .CBPerdiem.Checked = False
            .btnExpenseUpdate.Visible = False
            .btnExpenseSave.Visible = True
        End With
    End Sub
    Friend Sub ClearExpenseDataDetails(ByVal transactionID As String, ByVal comboClick As String)
        With FrmUserExpenseList
            If .GBMeals.Visible = True And .GBTransportation.Visible = False Then
                .GBMeals.Visible = False
                .TPExpenseReport.Enabled = True
                .dgvExpense.Enabled = True
                .CBBPaidFor.Checked = False
                .CBBPaidFor.Enabled = False
                .txtCategory.Enabled = True
                modLoadingData.UserExpenseMeal = ""
                ModDataStore.comboClick = 0
                Call ModFrmUserExpenseListDetails.GetUserExpenseMeal()
                Call ModFrmUserExpenseListDetails.OnOffControl(True)
                If transactionID = "" Then
                    .txtCategory.SelectedItem = Nothing
                End If
                .btnExpenseSave.Visible = False
                .btnExpenseUpdate.Visible = True
            ElseIf .GBMeals.Visible = False And .GBTransportation.Visible = True Then
                .GBTransportation.Visible = False
                .TPExpenseReport.Enabled = True
                .dgvExpense.Enabled = True
                .txtCategory.Enabled = True
                ModDataStore.comboClick = 0
                Call ModFrmUserExpenseListDetails.OnOffControl(True)
                If transactionID = "" Then
                    .txtCategory.SelectedItem = Nothing
                End If
                .btnExpenseSave.Visible = False
                .btnExpenseUpdate.Visible = True
            Else
                ModDataStore.transactionID = ""
                ModDataStore.comboClick = 0
                .txtCategory.SelectedItem = Nothing
                .BTNEditCategory.Enabled = False
                .btnExpenseUpdate.Visible = False
                .btnExpenseSave.Visible = True
                .dgvExpense.Enabled = True
                modLoadingData.UserExpenseMeal = ""
                .txtCategory.Enabled = True
                Call ModFrmUserExpenseListDetails.GetUserExpenseMeal()
                Call ModFrmUserExpenseListDetails.ClearExpenseData1()
            End If
        End With
    End Sub
    Friend Sub GetUserExpenseMeal()
        With FrmUserExpenseList
            If modLoadingData.UserExpenseMeal = "" Then
                .CLBMeals.SetItemChecked(0, False)
                .CLBMeals.SetItemChecked(1, True)
            End If
        End With
    End Sub
    Friend Sub ClearExpenseData1()
        With FrmUserExpenseList
            .txtParticulars.Text = ""
            .txtMultiplier.Text = "1"
            .txtExpenseAmount.Clear()
            .txtMultiplier.Text = "1"
            .txtTotal.Text = ""
            .txtInvoice.Clear()
            .txtRemarks.Text = ""
            .CBPerdiem.Checked = False
            .btnExpenseUpdate.Visible = False
            .btnExpenseSave.Visible = True
            .txtServiceNumber.Clear()
            .txtInstrument.Clear()
            .txtSerialNumber.Clear()
            .txtWorkWith.Clear()
            modLoadingData.WorkWith = ""
            .txtLocation.Clear()
        End With
    End Sub
    Public Sub CBPerdiemStatusTrue(ByVal transactionID As String)
        With FrmUserExpenseList
            .txtLocation.Enabled = False
            .txtLocation.Text = "Allowance"
            .txtInstrument.Enabled = False
            .txtInstrument.Text = "N/A"
            .txtSerialNumber.Enabled = False
            .txtSerialNumber.Text = "N/A"
            .txtServiceNumber.Enabled = False
            .txtServiceNumber.Text = "N/A"
            .txtMultiplier.Enabled = True
            .txtWorkWith.Text = "NONE"
            .txtExpenseAmount.Enabled = False
            modLoadingData.WorkWith = "NONE"
            If .txtWorkWith.Text <> "" And modLoadingData.WorkWith <> "" Then
                If transactionID <> "" Then
                    .txtCategory.Text = ModDataStore.txtCategoryBackUpData
                    .txtParticulars.Text = ModDataStore.txtParticularsBackUpData
                End If
            Else
                If transactionID = "" Then
                    .txtCategory.Enabled = False
                Else
                    .txtCategory.Enabled = True
                End If
            End If
        End With
    End Sub
    Public Sub CBPerdiemStatusFalse(ByVal transactionID As String)
        With FrmUserExpenseList
            .txtLocation.Enabled = True
            .txtLocation.Clear()
            .txtInstrument.Enabled = True
            .txtInstrument.Clear()
            .txtSerialNumber.Enabled = True
            .txtSerialNumber.Clear()
            .txtServiceNumber.Enabled = True
            .txtServiceNumber.Clear()
            .txtMultiplier.Enabled = False
            .txtExpenseAmount.Enabled = True
            .txtCategory.Enabled = True
            .txtWorkWith.Text = ""
            modLoadingData.WorkWith = ""
            If .txtWorkWith.Text = "" And modLoadingData.WorkWith = "" Then
                If transactionID <> "" Then
                    .txtCategory.Text = ModDataStore.txtCategoryBackUpData
                    .txtParticulars.Text = ModDataStore.txtParticularsBackUpData
                End If
            Else
                .txtCategory.SelectedItem = Nothing
                .txtParticulars.Text = ""
            End If
        End With
    End Sub
End Module
