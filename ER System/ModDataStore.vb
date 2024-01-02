Imports CrystalDecisions.[Shared].Json
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Module ModDataStore
    Public GetUserFareID As String
    Public GetUserFareFrom As String
    Public GetUserFareTo As String

    Public UserExpenseTransportation As String = ""

    Public UserPaidMeal As String = ""

    Public FormSettings As String = "0"

    Public ReportUserID As String

    Public ExpenseReportEdit As String = "0"

    Public UserExpenseListWorkWith As String

    Public UserSignatoryAssigned1 As String
    Public UserSignatoryAssigned2 As String

    Public UserWorkWithExpense As Double = 0.0

    Public txtCategoryBackUpData As String = ""
    Public txtParticularsBackUpData As String = ""
    Friend txtHospitalName As String
    Friend txtSerialNumber As String
    Friend txtSRNo As String
    Friend Type As String

    Public RBValidation As Boolean

    Friend comboClick As Integer = 0
    Friend transactionID As String
    Friend DinnerOTMeal As Double = 0.0
    Friend FareComboValidation As String = "0"
    Friend myApprover As String = ""
    Friend SortNumber As Integer
    Private ReadOnly ClsData As New ClsLoadData

    Public Sub CBPerdiemStatusTrue(ByVal transactionID As String)
        With frmEReport
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
        With frmEReport
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
    Friend Sub ClearExpenseDataDetails(ByVal transactionID As String, ByVal comboClick As String)
        With frmEReport
            Dim myERData As String()
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
            If (myERData(12) = "New" Or myERData(12) = "Returned") Or GetRegistryValue("Software\\ER System\\UserAccount", {"UserLevel"})(0) = "Admin" Then
                If .GBMeals.Visible = True And .GBTransportation.Visible = False Then
                    .GBMeals.Visible = False
                    .TPExpenseReport.Enabled = True
                    .dgvExpense.Enabled = True
                    .CBBPaidFor.Checked = False
                    .CBBPaidFor.Enabled = False
                    .txtCategory.Enabled = True
                    modLoadingData.UserExpenseMeal = ""
                    ModDataStore.comboClick = 0
                    Call GetUserExpenseMeal()
                    Call OnOffControl(True)
                    .BTNEditCategory.Enabled = True
                    .btnExpenseSave.Visible = False
                    .btnExpenseUpdate.Visible = True
                    .RTBNotification.Visible = False
                    .lblParticulars.Visible = True
                    .txtParticulars.Visible = True
                ElseIf .GBMeals.Visible = False And .GBTransportation.Visible = True Then
                    .GBTransportation.Visible = False
                    .TPExpenseReport.Enabled = True
                    .dgvExpense.Enabled = True
                    ModDataStore.comboClick = 0
                    Call ModDataStore.OnOffControl(True)
                    .txtCategory.Enabled = False
                    .BTNEditCategory.Enabled = True
                    .btnExpenseSave.Visible = False
                    .btnExpenseUpdate.Visible = True
                    .CBBFare.DropDownStyle = ComboBoxStyle.DropDownList
                    ModDataStore.FareComboValidation = "0"
                    .BTNAddFare.Text = "add"
                ElseIf .GBAllowance.Visible = True Then
                    .GBAllowance.Visible = False
                    .txtParticulars.Size = New Size(199, 40)
                    .GBAllowance.Location = New Point(97, 410)
                    .LBLComputation.Visible = True
                    .txtComputation.Visible = True
                Else
                    ModDataStore.transactionID = ""
                    ModDataStore.comboClick = 0
                    .txtCategory.SelectedItem = Nothing
                    .BTNEditCategory.Enabled = False
                    .btnExpenseUpdate.Visible = False
                    .btnExpenseSave.Visible = True
                    modLoadingData.UserExpenseMeal = ""
                    .txtCategory.Enabled = True
                    .txtParticulars.Size = New Size(199, 68)
                    .LBLComputation.Visible = False
                    .txtComputation.Visible = False
                    .txtMDays.Text = 0
                    .txtTotalNumberOfDays.Text = 0
                    .txtComputation.Text = 0
                    .CBPerdiem.Enabled = True
                    Call ModDataStore.GetUserExpenseMeal()
                    Call ModDataStore.ClearExpenseData1()
                    ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseSettings.txt")
                    ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
                    ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettingsTEMP.txt")
                    ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseMealSettings.txt")
                    ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseMealSettingsTEMP.txt")
                    .RTBNotification.Visible = False
                    .lblParticulars.Visible = True
                    .txtParticulars.Visible = True
                End If
            End If
        End With
    End Sub
    Friend Sub OnOffControl(ByVal TF As Boolean)
        With frmEReport
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
    Friend Sub GetUserExpenseMeal()
        'With frmEReport
        '    .CLBMeals.SetItemChecked(1, True)
        '    .CLBMeals.SetItemChecked(0, False)
        '    .CBDinnerOTMeal.CheckState = False
        '    .CBBPaidFor.Checked = False
        'End With
        With frmEReport
            Dim str As String = modLoadingData.LoadNotification(.dtpExpenseDate.Text,
                GetRegistryValue("Software\\ER System\\UserAccount", {"Username"})(0),
                "")
            If str <> "" Then
                .RTBNotification.Location = New Point(98, 212)
                .RTBNotification.Size = New Size(199, 68)
                .RTBNotification.Visible = True
                .lblParticulars.Visible = False
                .txtParticulars.Visible = False
                Dim strSplit As String() = str.Split("/")
                Dim myStrDate As String = ""
                Dim myStrMeal As String = ""
                myStrDate = "Date: " + .dtpExpenseDate.Text
                For x = 0 To UBound(strSplit)
                    myStrMeal = IIf(myStrMeal = "", "", myStrMeal + vbLf) + strSplit(x).Split("^")(2) +
                        " Filed By: " + strSplit(x).Split("^")(1)
                    If strSplit(x).Split("^")(2) = "Breakfast" Then
                        .CLBMeals.SetItemChecked(0, False)
                    ElseIf strSplit(x).Split("^")(2) = "Lunch" Then
                        .CLBMeals.SetItemChecked(1, False)
                    ElseIf strSplit(x).Split("^")(2) = "Dinner" Or strSplit(x).Split("^")(2) = "OT Meal" Then
                        .CBDinnerOTMeal.Checked = False
                    End If
                Next
                myStrMeal = "Meal: " + vbLf + myStrMeal
                .RTBNotification.Text = myStrDate + vbLf + myStrMeal

            Else
                .CLBMeals.SetItemChecked(1, True)
                .CLBMeals.SetItemChecked(0, False)
                .CBDinnerOTMeal.CheckState = False
                .CBBPaidFor.Checked = False
            End If
        End With
        '2^CAN^Lunch/1^CAN^Breakfast
    End Sub
    Friend Sub ClearExpenseData1()
        With frmEReport
            .txtParticulars.Text = ""
            .txtExpenseAmount.Clear()
            .txtMultiplier.Text = 1
            .txtTotal.Text = 0
            .txtInvoice.Clear()
            .txtRemarks.Text = ""
            .CBPerdiem.Checked = False
            .btnExpenseUpdate.Visible = False
            .btnExpenseSave.Visible = True
            .txtServiceNumber.Clear()
            .txtInstrument.Clear()
            .txtSerialNumber.Clear()
            .txtWorkWith.Text = "NONE"
            modLoadingData.WorkWith = ""
            .txtLocation.Clear()
        End With
    End Sub

    Friend Function SetDinnerOTAmount(ByVal UserMealExpense As String) As String
        Dim i As Integer = ModDataStore.SplitData(UserMealExpense, "&").Length
        With frmEReport
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

    Friend Sub BTNMealClick()
        Dim checkboxlistSelected As String
        With frmEReport
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
            Call ModDataStore.SetUserExpenseMeal(.CLBMeals, IIf(.CBBPaidFor.Checked = True, 1, 0), .CLBPaidBill)
            .txtExpenseAmount.Text = 0
            .txtExpenseAmount.Text = Val(UserTotalAmountPaidFor + UserTotalExpenseAmount)
            .txtExpenseAmount.Enabled = False
            .txtRemarks.Clear()
            .txtRemarks.Text = ModDataStore.UserPaidMeal
            Call ModDataStore.OnOffControl(True)
            Dim myERData As String() = {}
            If ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") Then
                myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
            End If
            Dim myExpenseData As String() = {}
            If ClsData.TempFileValidation(Application.StartupPath + "\expenseMealSettingsTEMP.txt") Then
                myExpenseData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseMealSettingsTEMP.txt")
                If myExpenseData(2) = 1 Then
                    .txtExpenseAmount.Clear()
                    .txtExpenseAmount.Enabled = True
                    .txtExpenseAmount.Select()
                Else
                    .txtExpenseAmount.Enabled = False
                End If
            End If
            If myERData.Length <> 0 Then
                .btnExpenseSave.Visible = False
                .btnExpenseUpdate.Visible = True
            Else
                .btnExpenseSave.Visible = True
                .btnExpenseUpdate.Visible = False
            End If
        End With
    End Sub
    Friend Sub LoadNotification(ByVal ReportID As String, ByVal ExpenseID As String, ByVal DateIncluded As String,
                                ByVal Username As String)

    End Sub
    Friend Sub SetUserExpenseMeal(ByVal CLBMeals As CheckedListBox, ByVal PaidFor As String, ByVal PaidEmp As CheckedListBox)
        Dim UserMealExpense As String = ""
        Dim UserPaidExpense As String = ""
        Dim UserPaidMeal As String = ""
        For index = 0 To CLBMeals.CheckedIndices.Count - 1
            UserMealExpense = IIf(UserMealExpense = "", "", UserMealExpense & "&") & CLBMeals.CheckedIndices.Item(index) & "^" & CLBMeals.CheckedItems.Item(index)
            ModDataStore.GetUserMealExpense(CLBMeals.CheckedItems.Item(index))
        Next
        If frmEReport.RBDinner.Checked = True Then
            ModDataStore.GetUserMealExpense("Dinner")
        ElseIf frmEReport.RBOTMeal.Checked = True Then
            ModDataStore.GetUserMealExpense("OT Meal")
        End If
        UserMealExpense = ModDataStore.SetDinnerOTAmount(UserMealExpense)
        For index = 0 To PaidEmp.CheckedIndices.Count - 1
            UserPaidExpense = IIf(UserPaidExpense = "", "", UserPaidExpense & "&") & PaidEmp.CheckedIndices.Item(index) & "^" & PaidEmp.CheckedItems.Item(index)
            UserPaidMeal = IIf(UserPaidMeal = "", "", UserPaidMeal & ",") & PaidEmp.CheckedItems.Item(index)
        Next
        ModDataStore.UserPaidMeal = UserPaidMeal
        Dim myERData As String()
        If ClsData.TempFileValidation(Application.StartupPath + "\expenseMealSettings.txt") = True Then
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseMealSettings.txt")
        Else
            myERData = {0}
        End If
        ClsData.SetExpenseMealDetailsTemp({myERData(0),
                                          UserMealExpense,
                                          PaidFor,
                                          UserPaidExpense,
                                          UserMealExpense + "/" + PaidFor + "/" & IIf(UserPaidExpense = "", "", UserPaidExpense)})
    End Sub

    Friend Sub GetUserMealExpense(ByVal MealName As String)
        If MealName = "Breakfast" Then
            UserTotalExpenseAmount = IIf(UserTotalExpenseAmount = 0.0, 0.0, UserTotalExpenseAmount) + GetRegistryValue("Software\\ER System\\UserAccount", {"BreakFastRate"})(0)
        ElseIf MealName = "Lunch" Then
            UserTotalExpenseAmount = IIf(UserTotalExpenseAmount = 0.0, 0.0, UserTotalExpenseAmount) + GetRegistryValue("Software\\ER System\\UserAccount", {"LunchRate"})(0)
        ElseIf MealName = "Dinner" Then
            UserTotalExpenseAmount = IIf(UserTotalExpenseAmount = 0.0, 0.0, UserTotalExpenseAmount) + GetRegistryValue("Software\\ER System\\UserAccount", {"DinnerRate"})(0)
        ElseIf MealName = "OT Meal" Then
            UserTotalExpenseAmount = IIf(UserTotalExpenseAmount = 0.0, 0.0, UserTotalExpenseAmount) + GetRegistryValue("Software\\ER System\\UserAccount", {"OTMeal"})(0)
        End If
    End Sub
    Friend Sub clearExpenseData()
        With frmEReport
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
            .txtParticulars.Enabled = False
        End With
    End Sub
    Friend Sub ClearAllExpenseData()
        With frmEReport
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
            .txtParticulars.Enabled = False
        End With
    End Sub
    Friend Sub RPTValidation(ByVal ReportStatus As String, ByVal PrintStatus As String)
        With frmRpt
            If ReportStatus = "For Approval" Then
                .CrystalReportViewer1.ShowNextPage()
                .CrystalReportViewer1.DisplayToolbar = True
                .CrystalReportViewer1.ShowPrintButton = False
                .CrystalReportViewer1.ShowCopyButton = False
                .CrystalReportViewer1.ShowExportButton = False
                .btnSendPrint.Enabled = False
            ElseIf ReportStatus = "New" Or ReportStatus = "Returned" Then
                .btnSendPrint.Enabled = False
                .CrystalReportViewer1.ShowNextPage()
                .CrystalReportViewer1.DisplayToolbar = True
                .CrystalReportViewer1.ShowPrintButton = False
                .CrystalReportViewer1.ShowCopyButton = False
                .CrystalReportViewer1.ShowExportButton = False
            ElseIf PrintStatus = "0" Then
                .CrystalReportViewer1.ShowNextPage()
                .CrystalReportViewer1.DisplayToolbar = True
                .CrystalReportViewer1.ShowPrintButton = False
                .CrystalReportViewer1.ShowCopyButton = False
                .CrystalReportViewer1.ShowExportButton = False
                .btnSendPrint.Enabled = True
            End If
        End With
    End Sub
    Friend Function EReportOpenValidation() As String
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        With frmEReport
            If myERData(12) = "New" Or myERData(12) = "Returned" Then
                .btnFileReport.Enabled = True
                .btnFileReport.Visible = True
                .btnPrintPreview.Enabled = True
                .btnFileReport.Text = "File Report"
                .TPExpenseReport.Enabled = True
                .dgvExpense.Enabled = True
                .btnFileReport.Location = New Point(5, 2)
                .btnPrintPreview.Location = New Point(129, 2)
            ElseIf myERData(12) = "For Approval" Or myERData(12) = "Approved" And
                GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0) <> "Admin" Then
                .btnFileReport.Enabled = True
                .btnFileReport.Visible = True
                .btnPrintPreview.Enabled = True
                .btnFileReport.Text = "Update Report"
                .TPExpenseReport.Enabled = False
                .dgvExpense.Enabled = False
                .btnFileReport.Location = New Point(5, 2)
                .btnPrintPreview.Location = New Point(129, 2)
            ElseIf myERData(12) = "For Approval" Or myERData(12) = "Approved" And
                GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0) = "Admin" Then
                If GetRegistryValue("Software\\ER System\\UserAccount", {"Approver1"})(0).Length = 0 And GetRegistryValue("Software\\ER System\\UserAccount", {"Approver2"})(0).Length = 0 Then
                    .btnFileReport.Enabled = True
                    .btnFileReport.Visible = False
                    .btnPrintPreview.Enabled = True
                    .btnFileReport.Text = "File Report"
                    .TPExpenseReport.Enabled = True
                    .dgvExpense.Enabled = True
                    .btnPrintPreview.Location = New Point(5, 2)
                Else
                    .btnFileReport.Enabled = True
                    .btnFileReport.Visible = True
                    .btnPrintPreview.Enabled = True
                    .btnFileReport.Text = "Update Report"
                    .TPExpenseReport.Enabled = False
                    .dgvExpense.Enabled = False
                    .btnFileReport.Location = New Point(5, 2)
                    .btnPrintPreview.Location = New Point(129, 2)
                End If
            End If
            Return myERData(13)
        End With
    End Function
    Friend Function EReportOpenValidatiionApprover() As String
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        With frmEReport
            .btnFileReport.Enabled = False
            .btnPrintPreview.Enabled = False
            .TPExpenseReport.Enabled = True
            .dgvExpense.Enabled = True
        End With
        Return myERData(13)
    End Function
    Friend Sub DgExpenseVisibility(ByVal ColumnName As String())
        With frmEReport
            For index = 0 To ColumnName.Length - 1
                .dgvExpense.Columns(ColumnName(index)).Visible = False
            Next
            .dgvExpense.Columns("Particulars").DefaultCellStyle.WrapMode = DataGridViewTriState.True
            .dgvExpense.Columns("Location").DefaultCellStyle.WrapMode = DataGridViewTriState.True
            .dgvExpense.Columns("Instrument").DefaultCellStyle.WrapMode = DataGridViewTriState.True
            .dgvExpense.Columns("Location").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            .dgvExpense.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
        End With
    End Sub
    Friend Sub DgReportDataVisibility(ByVal ColumnName As String())
        With frmMain
            For index = 0 To ColumnName.Length - 1
                .DgvReportDetails.Columns(ColumnName(index)).Visible = False
            Next
            .DgvReportDetails.Columns("Description").DefaultCellStyle.WrapMode = DataGridViewTriState.True
            .DgvReportDetails.Columns("Description").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            .DgvReportDetails.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
        End With
    End Sub
    Friend Sub DgUserDataVisibility(ByVal ColumnName As String())
        With frmApprove
            For index = 0 To ColumnName.Length - 1
                .dgvUser.Columns(ColumnName(index)).Visible = False
            Next
            .dgvUser.Columns("Fullname").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        End With
    End Sub
    Friend Sub MyFare()
        Dim dtLoadFare As DataTable = LoadFare()
        With frmEReport.CBBFare
            .DataSource = dtLoadFare
            .ValueMember = "id"
            .DisplayMember = "FareName"
        End With
    End Sub
    Friend Sub SetRegistryValue(ByVal dtLoginUser As DataTable)
        Dim ValueName As String() = {"UserID", "username", "Userlevel", "DeptID", "Fullname", "emp_Dept", "BreakFastRate",
            "LunchRate", "DinnerRate", "OTMeal", "TranspoRate", "Password", "Approver1", "Approver2"}
        Dim Value As String() = {dtLoginUser.Rows(0).Item(ValueName(0)).ToString(),
            dtLoginUser.Rows(0).Item(ValueName(1)).ToString(), dtLoginUser.Rows(0).Item(ValueName(2)).ToString(), dtLoginUser.Rows(0).Item(ValueName(3)).ToString(),
            dtLoginUser.Rows(0).Item(ValueName(4)).ToString(), dtLoginUser.Rows(0).Item(ValueName(5)).ToString(), dtLoginUser.Rows(0).Item(ValueName(6)).ToString(),
            dtLoginUser.Rows(0).Item(ValueName(7)).ToString(), dtLoginUser.Rows(0).Item(ValueName(8)).ToString(), dtLoginUser.Rows(0).Item(ValueName(9)).ToString(),
            dtLoginUser.Rows(0).Item(ValueName(10)).ToString(), dtLoginUser.Rows(0).Item(ValueName(11)).ToString(), dtLoginUser.Rows(0).Item(ValueName(12)).ToString(),
            dtLoginUser.Rows(0).Item(ValueName(13)).ToString()}
        ClsData.RegistrySettings("HKEY_CURRENT_USER\Software\ER System", "UserAccount", ValueName, Value)
    End Sub
    Friend Function GetRegistryValue(ByVal subKey As String, ByVal valueName As String()) As List(Of String)
        Return ClsData.RegistryGetValue(subKey, valueName)
    End Function
    Friend Sub SetRegistry(ByVal subkey As String, ByVal ValueName As String(), ByVal Value As String())
        ClsData.RegistrySettings("HKEY_CURRENT_USER\Software\ER System", subkey, ValueName, Value)
    End Sub
    Friend Sub LoginSettingsControl(ByVal MenuForms As Boolean, ByVal MenuFile As Boolean, ByVal Enabled As Boolean,
                                    ByVal tsmiPrev As Boolean, ByVal UserAccountToolStripMenuItem As Boolean, ByVal fmsExpenseSummary As Boolean)
        With frmMain
            .MenuForms.Visible = MenuForms
            .MenuFile.Visible = MenuFile
            .Enabled = Enabled
            .tsmiPrev.Visible = tsmiPrev
            .UserAccountToolStripMenuItem.Visible = UserAccountToolStripMenuItem
            .BringToFront()
            .ShowDialog()
        End With
    End Sub
    Friend Sub myFrmEReportLoad(ByVal type As Boolean)
        With frmEReport
            .RbLocal.Checked = type
            .txtStatus.SelectedIndex = 0
        End With
    End Sub
    Friend Sub RefreshEReportData()
        With frmMain
            .DgvReportDetails.DataSource = LoadingDataReport(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), "01/01/1990", "12/01/2200")
            Call DgReportDataVisibility({"Report ID"})
        End With
    End Sub
    Friend Sub UpdatePrintStatus(ByVal ReportID As String)
        DBConnection()
        Using sqlcmdPrintStatus As New SqlClient.SqlCommand
            Using SQLConnection As SqlClient.SqlConnection = mConn.SQLConnection
                With sqlcmdPrintStatus
                    .Connection = SQLConnection
                    .CommandText = "UPDATE tbReportDetails SET ReportPrintStatus ='1', ReportEndorseStatus = 'NOT APPROVED', ReportFileStatus = '0',ReportNumberStatus = '0' where ID='" & ReportID & "'"
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Friend Function DgvSearch(ByVal TxtSearchBy As String) As DataView
        Using ReportDataTable As DataTable = LoadingDataReport(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), "01/01/1990", "12/01/2200")
            Return New DataView(ReportDataTable, TxtSearchBy, "Date From desc", DataViewRowState.CurrentRows)
        End Using
    End Function
    Friend Sub UpdateEReportData()
        With frmMain
            If .TxtSearchBy.Text <> "" Then
                .DgvReportDetails.Visible = True
                .DgvReportDetails.BringToFront()
                If .CBBFilter.SelectedItem = "All" Then
                    .DgvReportDetails.DataSource = DgvSearch("Description like '" & "%" & .TxtSearchBy.Text & "%" & "'")
                    Call DgReportDataVisibility({"Report ID"})
                Else
                    .DgvReportDetails.DataSource = DgvSearch("Description like '" & "%" & .TxtSearchBy.Text & "%" & "' and Status = '" & .CBBFilter.SelectedItem & "'")
                    Call DgReportDataVisibility({"Report ID"})
                End If
            Else
                .DgvReportDetails.Visible = True
                .DgvReportDetails.BringToFront()
                If .CBBFilter.SelectedItem = "All" Then
                    .DgvReportDetails.DataSource = DgvSearch("")
                    Call DgReportDataVisibility({"Report ID"})
                Else
                    If .CBBFilter.SelectedItem = Nothing Then
                        MessageBox.Show("Please Select Filter")
                    Else
                        .DgvReportDetails.DataSource = DgvSearch("Status = '" & .CBBFilter.SelectedItem & "'")
                        Call DgReportDataVisibility({"Report ID"})
                    End If
                End If
            End If
        End With
    End Sub


    Friend Function DgvHospitalSearch(ByVal TxtSearchBy As String) As DataView
        Dim ClsData As ClsLoadData = New ClsLoadData()
        Dim myJObject As JObject = New JObject()
        myJObject = ClsData.JsonStringAsync(
            "https://inventory.mdmpi.com.ph/api/instruments/account?srch=" & TxtSearchBy,
            "").Result
        Using dt As DataTable = JsonConvert.DeserializeObject(Of DataTable)(Convert.ToString(myJObject.Value(Of JArray)("data")))
            If dt.Rows.Count <> 0 Then
                With frmHistory
                    Return New DataView(
                        dt, "name like '" & "%" & TxtSearchBy & "%" & "' or acronym like '" & "%" & TxtSearchBy & "%" & "'",
                        "name asc", DataViewRowState.CurrentRows)
                End With
            Else
                Return New DataView()
            End If
        End Using
    End Function
    Friend Function DgvInstrumentSearch(ByVal TxtSearchBy As String) As DataView
        Dim ClsData As ClsLoadData = New ClsLoadData()
        Dim myJObject As JObject = New JObject()
        myJObject = ClsData.JsonStringAsync(
            "https://inventory.mdmpi.com.ph/api/instruments/account/" & TxtSearchBy,
            "").Result
        Using dt As DataTable = JsonConvert.DeserializeObject(Of DataTable)(Convert.ToString(myJObject.Value(Of JArray)("tbinstruments")))
            If dt.Rows.Count <> 0 Then
                With frmHistory
                    Return New DataView(dt, "", "ITEM_CODE asc", DataViewRowState.CurrentRows)
                End With
            End If
        End Using
        Return New DataView()
    End Function
    Friend Sub SaveHospitalID(ByVal hospID As String)
        Dim ClsData As ClsLoadData = New ClsLoadData()
        Dim ValueName As String() = {"HospitalID"}
        Dim Value As String() = {hospID}
        ClsData.RegistrySettings("HKEY_CURRENT_USER\Software\ER System", "Expense", ValueName, Value)
    End Sub
    Friend Sub FrmValidation(Optional ByVal strValid As String = "")
        With frmHistory
            If modLoadingData.DataToLoad = "Hospital" Then
                .txtName.Size = New Size(394, 20)
                .dgvHistory.Size = New Size(453, 314)
                .BTNInsert.Visible = False
                .BTNSearch.Visible = True
            ElseIf modLoadingData.DataToLoad = "Instrument" Then
                .txtName.Size = New Size(453, 20)
                .dgvHistory.Size = New Size(453, 285)
                .BTNInsert.Visible = True
                .BTNSearch.Visible = False
                If strValid <> "" Then
                    With frmHistory
                        .dgvHistory.DataSource = Nothing
                        .dgvHistory.Rows.Clear()
                        .dgvHistory.Columns.Clear()
                        .dgvHistory.DataSource = DgvInstrumentSearch(GetRegistryValue("Software\\ER System\\Expense", {"HospitalID"})(0))
                        If .dgvHistory.Rows.Count <> 0 Then
                            Dim chkbox As New DataGridViewCheckBoxColumn With {.FlatStyle = FlatStyle.Standard}
                            .dgvHistory.Columns.Insert(0, chkbox)
                            .dgvHistory.Columns(0).Width = 30
                            .dgvHistory.Columns(1).HeaderText = "Instrument Name"
                            .dgvHistory.Columns(3).HeaderText = "Serial Number"
                            .dgvHistory.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                            .dgvHistory.Columns(2).Visible = False
                            .dgvHistory.Columns(1).ReadOnly = True
                            .dgvHistory.Columns(2).ReadOnly = True
                        End If
                    End With
                End If
            End If
        End With
    End Sub
    Friend Sub DGVLoadExpenseReport(ByVal ReportID As String, ByVal UserID As String)
        With frmEReport
            .dgvExpense.DataSource = LoadingExpenseReport(ReportID, UserID)
        End With
    End Sub
    Friend Sub MyDepartment(ByVal dtLoadDepartment As DataTable)
        With frmDeptSignature.cbbDept
            .DataSource = dtLoadDepartment
            .ValueMember = "ID"
            .DisplayMember = "emp_Dept"
        End With
    End Sub
    Friend Sub SharedProcedure()
        With frmEReport
            Dim str As String = modLoadingData.LoadNotification(
                .dtpExpenseDate.Text,
                GetRegistryValue("Software\\ER System\\UserAccount", {"Username"})(0),
                "")
            If str <> "" Then
                .RTBNotification.Location = New Point(98, 212)
                .RTBNotification.Size = New Size(199, 68)
                .RTBNotification.Visible = True
                .lblParticulars.Visible = False
                .txtParticulars.Visible = False
                Dim strSplit As String() = str.Split("/")
                Dim myStrDate As String = ""
                Dim myStrMeal As String = ""
                myStrDate = "Date: " + .dtpExpenseDate.Text
                For x = 0 To UBound(strSplit)
                    myStrMeal = IIf(myStrMeal = "", "", myStrMeal + vbLf) + strSplit(x).Split("^")(2) +
                        " Filed By: " + strSplit(x).Split("^")(1)

                Next
                myStrMeal = "Meal: " + myStrMeal
                .RTBNotification.Text = myStrDate + vbLf + myStrMeal
            End If
        End With
    End Sub
End Module

