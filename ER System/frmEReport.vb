Imports System.IO
Public Class frmEReport
    Dim SortNumber As Integer
    Dim counter As String = ""
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
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
            Dim sTmp(8) As String
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
            Dim iLastRow As Integer = dgvExpense.Rows.Count - 1
            If iSelectedRow < iLastRow Then
                For iTmp = iSelectedRow To iLastRow - 1
                    For iCol As Integer = 0 To dgvExpense.Columns.Count - 1
                        dgvExpense.Rows(iTmp).Cells(iCol).Value = dgvExpense.Rows(iTmp + 1).Cells(iCol).Value
                    Next
                Next
                For iCol As Integer = 0 To dgvExpense.Columns.Count - 1
                    dgvExpense.Rows(iLastRow).Cells(iCol).Value = sTmp(iCol).ToString
                Next
                toSelect(iLastRow)
            End If
        End If
    End Sub
#End Region
    Public Sub AutoSizegrid()
        dgvExpense.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        dgvExpense.Columns(1).DefaultCellStyle.WrapMode = DataGridViewTriState.True
    End Sub

    Private Sub frmEReport_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Dim ClsData As New ClsLoadData
        My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\ER System\Settings", "Additional", "1")
        ModDataStore.FormSettings = "0"
        ModDataStore.transactionID = ""
        ModDataStore.comboClick = 0
        txtCategory.SelectedItem = Nothing
        BTNEditCategory.Enabled = False
        btnExpenseUpdate.Visible = False
        btnExpenseSave.Visible = True
        modLoadingData.UserExpenseMeal = ""
        txtCategory.Enabled = True
        txtParticulars.Size = New Size(199, 68)
        LBLComputation.Visible = False
        txtComputation.Visible = False
        txtMDays.Text = 0
        txtTotalNumberOfDays.Text = 0
        txtComputation.Text = 0
        CBPerdiem.Enabled = True
        Call ModDataStore.GetUserExpenseMeal()
        Call ModDataStore.ClearExpenseData1()
        ClsData.DeleteEReportDetails(Application.StartupPath + "\settings.txt")
        ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseSettings.txt")
        ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
        ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettingsTEMP.txt")
        ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseMealSettings.txt")
        ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseMealSettingsTEMP.txt")
        GBTransportation.Visible = False
        GBMeals.Visible = False
        RTBNotification.Visible = False
        lblParticulars.Visible = True
        txtParticulars.Visible = True
        TPExpenseReport.Enabled = True
        dgvExpense.Enabled = True
        Call ModDataStore.OnOffControl(True)
        CBBFare.DropDownStyle = ComboBoxStyle.DropDownList
        ModDataStore.FareComboValidation = "0"
        BTNAddFare.Text = "add"
        CBBPaidFor.Checked = False
        CBBPaidFor.Enabled = False
        RTBNotification.Visible = False
        Call ReleasMemory()
    End Sub
    Private Sub frmEReport_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then
            Call ModDataStore.ClearExpenseDataDetails(transactionID, comboClick)
        End If
    End Sub
    Private Sub frmEReport_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim ClsData As New ClsLoadData
        Call DgExpenseVisibility({"ID", "reportid", "sort"})
        Call myFrmEReportLoad(True)
        txtCategory.Items.Clear()
        txtCategory.Items.Add("Transportation")
        txtCategory.Items.Add("Meals")
        txtCategory.Items.Add("Toll")
        txtCategory.Items.Add("Parking")
        txtCategory.Items.Add("Others")
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        dtpExpenseDate.Value = myERData(1)
        If GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0) = 3 Then
            CBPerdiem.Visible = True
        Else
            CBPerdiem.Visible = False
        End If
        Me.dtpExpenseDate.Value = DateTime.Now.ToString("MM/dd/yyyy")
        txtWorkWith.Text = "NONE"
    End Sub
    Private Sub dgvExpense_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvExpense.CellClick
        If e.RowIndex > 0 Then
            SortNumber = dgvExpense.Rows(e.RowIndex).Cells("sort").Value
        End If
    End Sub

    Private Sub dgvExpense_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvExpense.CellDoubleClick
        Try
            Dim ClsData As New ClsLoadData
            Call ModDataStore.ClearExpenseDataDetails(transactionID, comboClick)
            ClsData.SetExpenseDetails(dgvExpense.Rows(e.RowIndex).Cells("reportid").Value,
                                          dgvExpense.Rows(e.RowIndex).Cells("ID").Value)
            ClsData.SetExpenseMealDetails(dgvExpense.Rows(e.RowIndex).Cells("ID").Value)
            ClsData.SetExpenseTransDetails(dgvExpense.Rows(e.RowIndex).Cells("ID").Value)
            Dim myERData As String()
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
            Dim myExpenseData As String() = {0, 0, 0}
            If myERData.Length <> 0 Then
                If myERData.Length = 21 Then
                    txtMDays.Text = myERData(18)
                    txtComputation.Text = myERData(19)
                    txtTotalNumberOfDays.Text = myERData(20)
                End If
                dtpExpenseDate.Value = DateTime.Parse(myERData(0)).ToString("MM/dd/yyyy")
                txtWorkWith.Text = myERData(14)
                txtLocation.Text = myERData(5)
                txtInstrument.Text = myERData(12)
                txtSerialNumber.Text = myERData(13)
                txtServiceNumber.Text = myERData(11)
                RbLocal.Checked = IIf(myERData(2) = "Local", True, False)
                RBForeign.Checked = IIf(myERData(2) = "Foreign", True, False)
                txtParticulars.Text = myERData(1)
                txtExpenseAmount.Text = myERData(15)
                txtMultiplier.Text = myERData(7)
                txtTotal.Text = myERData(6)
                txtStatus.SelectedItem = myERData(9)
                txtRemarks.Text = Replace(myERData(10), "<SPLIT>", vbLf)
                CBPerdiem.Checked = IIf(myERData(4) <> 0, True, False)
                CBPerdiem.Enabled = False
                BTNEditCategory.Enabled = IIf(myERData(4) <> 0, False, True)
                txtCategory.Enabled = IIf(myERData(4) <> 0, True, False)
                txtExpenseAmount.Enabled = IIf(myExpenseData(2) = 1, True, IIf(myERData(4) = 0 And myERData(3) = "Transportation", True, False))
                txtCategory.SelectedItem = myERData(3)
                txtInvoice.Text = myERData(8)
                btnExpenseSave.Visible = False
                btnExpenseUpdate.Visible = True
            End If
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub txtExpenseAmount_KeyUp(sender As Object, e As KeyEventArgs) Handles txtExpenseAmount.KeyUp
        If e.KeyCode = Keys.Enter Then
            txtMultiplier.Focus()
        End If
    End Sub
    Private Sub txtExpenseAmount_TextChanged(sender As Object, e As EventArgs) Handles txtExpenseAmount.TextChanged
        If CBPerdiem.Checked = True Then
            If txtCategory.SelectedIndex = 4 Or txtCategory.SelectedIndex = 2 Or txtCategory.SelectedIndex = 3 Then
                txtTotal.Text = Math.Round(Val(txtExpenseAmount.Text)) * Val(txtMultiplier.Text)
            ElseIf txtCategory.SelectedIndex <> -1 Then
                txtTotal.Text = Val(txtExpenseAmount.Text) * Val(txtMultiplier.Text)
                Dim particulars As String = ""
                If CBPerdiem.Checked = True Then
                    If txtMDays.Text <> "0" And txtMDays.Text <> "" Then
                        particulars = particulars + " (" + txtTotalNumberOfDays.Text + "Days - " + txtMDays.Text + "Days) * " + txtExpenseAmount.Text
                        txtMultiplier.Text = Val(txtTotalNumberOfDays.Text - txtMDays.Text)
                    ElseIf txtMDays.Text = "" Then
                        Dim result As DialogResult = MessageBox.Show("-Days is Empty. Make Value to 0?", "Default Value", MessageBoxButtons.OKCancel)
                        If DialogResult.OK = result Then
                            txtMDays.Text = 0
                        End If
                        Exit Sub
                    Else
                        particulars = particulars + " (" + txtTotalNumberOfDays.Text + "Days) * " + txtExpenseAmount.Text
                        txtMultiplier.Text = Val(txtTotalNumberOfDays.Text - txtMDays.Text)
                    End If
                    txtComputation.Text = particulars
                End If
            End If
        Else
            If txtInvoice.Text <> "" And txtMultiplier.Text = 1 Then
                txtTotal.Text = Val(txtExpenseAmount.Text) * Val(txtMultiplier.Text)
            Else
                txtTotal.Text = Math.Round(Val(txtExpenseAmount.Text) * Val(txtMultiplier.Text))
            End If
        End If
    End Sub
    Private Sub txtMultiplier_KeyUp(sender As Object, e As KeyEventArgs) Handles txtMultiplier.KeyUp
        If e.KeyCode = Keys.Enter Then
            txtInvoice.Focus()
        End If
    End Sub
    Private Sub txtMultiplier_TextChanged(sender As Object, e As EventArgs) Handles txtMultiplier.TextChanged
        If txtInvoice.Text <> "" And txtMultiplier.Text = 1 Then
            txtTotal.Text = Val(txtExpenseAmount.Text) * Val(txtMultiplier.Text)
        Else
            txtTotal.Text = Math.Round(Val(txtExpenseAmount.Text) * Val(txtMultiplier.Text))
        End If

    End Sub
    Private Sub btnExpenseSave_Click(sender As Object, e As EventArgs) Handles btnExpenseSave.Click
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
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
                        "Data Saved. Do you Want to Clear the WorkWith, Hospital Name, Instrument and Serial Number above?",
                        "Clear Details",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) = DialogResult.Yes Then
                ModDataStore.ClearAllExpenseData()
            Else
                ModDataStore.clearExpenseData()
            End If

            Using dtLoadExpenseReport As DataTable = LoadingExpenseReport(myERData(13), GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                dgvExpense.DataSource = dtLoadExpenseReport
            End Using
            txtParticulars.Focus()
            dgvExpense.Enabled = True
            comboClick = 0
        End If
        transactionID = ""
        txtStatus.SelectedIndex = 0
        txtCategory.Enabled = True
        txtComputation.Clear()
    End Sub
    Private Sub AddExpenseReport()
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        Try
            AddExpense(dtpExpenseDate.Value, IIf(CBPerdiem.Checked, "1", "0"), txtParticulars.Text,
           txtInvoice.Text, txtMultiplier.Text, IIf(RbLocal.Checked = True,
                                                    "Local", "Foreign"), txtCategory.Text,
           txtExpenseAmount.Text, txtRemarks.Text, txtStatus.SelectedItem,
           txtTotal.Text,
           IIf(Trim(Trim(txtLocation.Text)) = "", "Allowance",
            Trim(Trim(Trim(txtLocation.Text)))),
           myERData(14), myERData(13),
           IIf(txtServiceNumber.Text = "", "N/A", txtServiceNumber.Text),
           IIf(txtInstrument.Text = "", "N/A", txtInstrument.Text),
           IIf(txtSerialNumber.Text = "", "N/A", txtSerialNumber.Text), txtWorkWith.Text,
           ClsData.GetMeal(), ClsData.GetTranspo(),
           txtMDays.Text,
           txtComputation.Text, txtTotalNumberOfDays.Text)
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        ModDataStore.ClearExpenseDataDetails(transactionID, comboClick)
    End Sub

    Private Sub btnExpenseUpdate_Click(sender As Object, e As EventArgs) Handles btnExpenseUpdate.Click
        Dim ClsData As New ClsLoadData
        If txtParticulars.Text = Nothing Or txtExpenseAmount.Text = Nothing Or txtCategory.SelectedItem = "" Then
            MsgBox("Please fill in the Particulars/Expense Amount/Category")
        Else
            Dim myExpenseData As String()
            Dim myERData As String()
            myExpenseData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
            UpdateExpense(
                myExpenseData(16), dtpExpenseDate.Text, IIf(CBPerdiem.Checked, "1", "0"), txtParticulars.Text,
                txtInvoice.Text, txtMultiplier.Text, IIf(RbLocal.Checked = True, "Local", "Foreign"), txtCategory.Text,
                txtExpenseAmount.Text, txtRemarks.Text, txtStatus.SelectedItem, txtTotal.Text,
                IIf(Trim(txtLocation.Text) = "", "Allowance", Trim(txtLocation.Text)),
                myERData(14), txtServiceNumber.Text, txtInstrument.Text,
                txtSerialNumber.Text, txtWorkWith.Text, ClsData.GetMeal(),
                ClsData.GetTranspo(), txtMDays.Text, txtComputation.Text, txtTotalNumberOfDays.Text)

            AddExpenseHisto(dtpExpenseDate.Text, IIf(CBPerdiem.Checked, "1", "0"),
                            txtParticulars.Text, txtInvoice.Text,
                            txtMultiplier.Text, IIf(RbLocal.Checked = True, "Local",
                                                    "Foreign"), txtCategory.Text,
                            txtExpenseAmount.Text, txtRemarks.Text, txtStatus.Text,
                            txtTotal.Text, IIf(Trim(txtLocation.Text) = "", "Allowance",
                                               Trim(txtLocation.Text)),
                            myERData(14), myERData(13), myExpenseData(16),
                            txtServiceNumber.Text, txtInstrument.Text, txtSerialNumber.Text,
                            GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), txtMDays.Text, txtComputation.Text, txtTotalNumberOfDays.Text)
            If MessageBox.Show(
                    "Data Saved. Do you Want to Clear the WorkWith, Hospital Name, Instrument and Serial Number above?",
                    "Clear Details",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) = DialogResult.Yes Then
                ModDataStore.ClearAllExpenseData()
                ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseSettings.txt")
            Else
                ModDataStore.clearExpenseData()
                ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseSettings.txt")
            End If
            Using dtLoadExpenseReport As DataTable = LoadingExpenseReport(myERData(13), myERData(14))
                dgvExpense.DataSource = dtLoadExpenseReport
            End Using
            modReuse.SetTextFile(txtWorkWith.Text, txtLocation.Text, txtInstrument.Text, txtSerialNumber.Text, txtServiceNumber.Text)
            txtParticulars.Focus()
            dgvExpense.Enabled = True
            comboClick = 0
        End If
        transactionID = ""
        txtStatus.SelectedIndex = 0
        txtCategory.Enabled = True
    End Sub
    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles BTNUp.Click
        Dim ClsData As New ClsLoadData
        Dim myERData As String() = {}
        If ClsData.TempFileValidation(Application.StartupPath + "\settings.txt") = True Then
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        End If
        If LoadingExpenseCount(myERData(13)) = 0 Then
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
                    MsgBox(ex.Message)
                End Try
            End If
        End If
    End Sub
    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles BTNDown.Click
        Dim ClsData As New ClsLoadData
        Dim myERData As String() = {}
        If ClsData.TempFileValidation(Application.StartupPath + "\settings.txt") = True Then
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        End If
        If LoadingExpenseCount(myERData(13)) = 0 Then
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
                    MsgBox(ex.Message)
                End Try
            End If
        End If
    End Sub
    Private Sub Sorting()
        Dim ClsData As New ClsLoadData
        DBConnection()
        Using sqlSaveSort As New SqlClient.SqlCommand
            Using SQLConnection As SqlClient.SqlConnection = mConn.SQLConnection
                With sqlSaveSort
                    .Connection = SQLConnection
                    For a = 0 To dgvExpense.Rows.Count - 1
                        .CommandText = "update tbExpenseDetails set Sort ='" & a & "' where ID='" & dgvExpense.Rows(a).Cells(0).Value & "'"
                        .CommandType = CommandType.Text
                        .ExecuteNonQuery()
                    Next
                    Dim myERData As String()
                    myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
                    Using LoadExpenseReport As DataTable = LoadingExpenseReport(myERData(13), GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                        dgvExpense.DataSource = LoadExpenseReport
                    End Using
                End With
            End Using
        End Using
    End Sub
    Private Sub CBPerdiem_CheckedChanged(sender As Object, e As EventArgs) Handles CBPerdiem.CheckedChanged
        If CBPerdiem.Checked = True Then
            Call ModDataStore.CBPerdiemStatusTrue(transactionID)
            txtCategory.Items.Clear()
            txtCategory.Items.Add("Transportation")
            txtCategory.Items.Add("Meals")
            txtCategory.Items.Add("Toll")
            txtCategory.Items.Add("Parking")
            txtCategory.Items.Add("Others")
        Else
            Call ModDataStore.CBPerdiemStatusFalse(transactionID)
            txtCategory.Items.Clear()
            txtCategory.Items.Add("Transportation")
            txtCategory.Items.Add("Meals")
            txtCategory.Items.Add("Toll")
            txtCategory.Items.Add("Parking")
            txtCategory.Items.Add("Others")
        End If
    End Sub
    Private Sub txtInstrument_KeyUp(sender As Object, e As KeyEventArgs) Handles txtInstrument.KeyUp
        If e.KeyCode = Keys.Enter Then
            txtSerialNumber.Focus()
        End If
    End Sub
    Private Sub txtSerialNumber_KeyUp(sender As Object, e As KeyEventArgs) Handles txtSerialNumber.KeyUp
        If e.KeyCode = Keys.Enter Then
            txtServiceNumber.Focus()
        End If
    End Sub
    Private Sub txtParticulars_KeyUp(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            txtType.Focus()
        End If
    End Sub
    Private Sub txtParticulars_LostFocus(sender As Object, e As EventArgs)
        If txtParticulars.Text Like "*MEAL*" Then
            txtCategory.SelectedIndex = 1
        ElseIf txtParticulars.Text Like "*TRANS*" Then
            txtCategory.SelectedIndex = 0
        Else
            txtCategory.SelectedItem = Nothing
        End If
    End Sub
    Private Sub txtType_KeyUp(sender As Object, e As KeyEventArgs) Handles txtType.KeyUp
        If e.KeyCode = Keys.Enter Then
            txtCategory.Focus()
        End If
    End Sub
    Private Sub txtCategory_KeyUp(sender As Object, e As KeyEventArgs) Handles txtCategory.KeyUp
        If e.KeyCode = Keys.Enter Then
            txtExpenseAmount.Focus()
        End If
    End Sub
    Private Sub txtServiceNumber_KeyUp(sender As Object, e As KeyEventArgs) Handles txtServiceNumber.KeyUp
        If e.KeyCode = Keys.Enter Then
            txtParticulars.Focus()
        End If
    End Sub
    Private Sub txtInvoice_KeyUp(sender As Object, e As KeyEventArgs) Handles txtInvoice.KeyUp
        If e.KeyCode = Keys.Enter Then
            txtRemarks.Focus()
        End If
    End Sub
    Private Sub txtLocation_KeyUp1(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            txtInstrument.Focus()
        End If
    End Sub
    Private Sub btnInstrumentHistory_Click(sender As Object, e As EventArgs) Handles btnInstrumentHistory.Click
        modLoadingData.DataToLoad = "Instrument"
        frmHistory.Text = "Instrument"
        Call FrmValidation(Me.txtLocation.Text)
        frmHistory.txtName.Focus()
        frmHistory.ShowDialog()
    End Sub
    Private Sub btnHospital_Click(sender As Object, e As EventArgs) Handles btnHospital.Click
        modLoadingData.DataToLoad = "Hospital"
        frmHistory.Text = "Hospital"
        Call FrmValidation(Me.txtLocation.Text)
        frmHistory.txtName.Focus()
        frmHistory.ShowDialog()
    End Sub
    Private Sub btnWorkWith_Click(sender As Object, e As EventArgs) Handles btnWorkWith.Click
        LoadUserWorkWith()
        frmAdditionalInput.dgvUser.Columns(1).ReadOnly = True
        frmAdditionalInput.dgvUser.Columns(2).ReadOnly = True
        frmAdditionalInput.dgvUser.Columns(0).Width = 30
        frmAdditionalInput.ShowDialog()
        frmAdditionalInput.BringToFront()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        If File.Exists(Application.StartupPath + "/ER.txt") = False Then
            MsgBox("No Data Available")
        Else
            Dim str() As String = {""}
            str = modReuse.GetTextFile().Split("/")
            If str.Length <> 0 Then
                txtWorkWith.Text = str(0)
                txtLocation.Text = str(1)
                txtInstrument.Text = str(2)
                txtSerialNumber.Text = str(3)
                txtServiceNumber.Text = str(4)
            Else
                MsgBox("No Data Available")
            End If
        End If
    End Sub

    Private Sub BTNMeals_Click(sender As Object, e As EventArgs) Handles BTNMeals.Click
        If CBBPaidFor.Checked = True And CLBPaidBill.CheckedIndices.Count = 0 Then
            MessageBox.Show("No selected Employee to be Paid for. Please Uncheck 'Paid For'")
        ElseIf CLBMeals.CheckedIndices.Count <> 0 Or CBDinnerOTMeal.Checked = True And
            (RBDinner.Checked = True Or RBOTMeal.Checked = True) Then

            If CBDinnerOTMeal.Checked = True And (RBDinner.Checked = False And RBOTMeal.Checked = False) Then
                MsgBox("You've checked the 'Check for Dinner or OT Meal'. Please Choose either Dinner or OT Meal")
            Else
                Call BTNMealClick()
                RTBNotification.Visible = False
                txtParticulars.Visible = True
                lblParticulars.Visible = True
            End If
        Else
            MessageBox.Show("Unable to Proceed. Please Select Meal/s")
        End If

    End Sub

    Private Sub txtCategory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles txtCategory.SelectedIndexChanged
        Dim ClsData As New ClsLoadData
        If txtCategory.SelectedItem <> Nothing Then
            If CBPerdiem.Checked = True Then
                ComputationLead()
            Else
                If ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") = True Then
                    Dim myERData As String()
                    myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
                    Call OpeningLead(myERData(14), myERData(16))
                    txtParticulars.Size = New Size(199, 68)
                    LBLComputation.Visible = False
                    txtComputation.Visible = False
                Else
                    Call OpeningLead(txtWorkWith.Text, "")
                    txtParticulars.Size = New Size(199, 68)
                    LBLComputation.Visible = False
                    txtComputation.Visible = False
                End If
            End If
        End If
    End Sub
    Private Sub ComputationLead()
        Dim ClsData As New ClsLoadData
        If txtCategory.SelectedIndex = 4 Then
            txtParticulars.Enabled = True
            txtExpenseAmount.Enabled = True
            txtParticulars.Select()
        ElseIf txtCategory.SelectedIndex = 2 Then
            txtParticulars.Enabled = False
            txtExpenseAmount.Enabled = True
            txtParticulars.Text = txtCategory.SelectedItem
            txtParticulars.Select()
        ElseIf txtCategory.SelectedIndex = 3 Then
            txtParticulars.Enabled = False
            txtExpenseAmount.Enabled = True
            txtParticulars.Text = txtCategory.SelectedItem
            txtParticulars.Select()
        Else
            If comboClick = 1 And ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") = False Then
                txtParticulars.Size = New Size(199, 40)
                GBAllowance.Location = New Point(97, 410)
                GBAllowance.Visible = True
                LBLComputation.Visible = True
                txtParticulars.Text = txtCategory.Text
                txtExpenseAmount.Enabled = True
                txtMultiplier.Enabled = False
                txtMDays.Text = 0
                txtComputation.Visible = True
                If IsDBNull(GetRegistryValue("Software\\ER System\\UserAccount", {"TotalDays"})(0)) = False Then
                    txtTotalNumberOfDays.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"TotalDays"})(0)
                End If
                If txtCategory.SelectedIndex = 0 Then
                    txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"TranspoRate"})(0)
                ElseIf txtCategory.SelectedIndex = 1 Then
                    txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"LunchRate"})(0)
                End If
            ElseIf comboClick = 1 And ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") = True Then
                txtParticulars.Size = New Size(199, 40)
                GBAllowance.Location = New Point(97, 410)
                GBAllowance.Visible = True
                If IsDBNull(GetRegistryValue("Software\\ER System\\UserAccount", {"TotalDays"})(0)) = False Then
                    txtTotalNumberOfDays.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"TotalDays"})(0)
                End If
                If txtCategory.SelectedIndex = 0 Then
                    txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"TranspoRate"})(0)
                ElseIf txtCategory.SelectedIndex = 1 Then
                    txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"LunchRate"})(0)
                End If
            ElseIf ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") = True Then
                txtCategory.Enabled = False
                BTNEditCategory.Enabled = True
                txtMultiplier.Enabled = False
                txtParticulars.Size = New Size(199, 40)
                txtComputation.Visible = True
                LBLComputation.Visible = True
                If IsDBNull(GetRegistryValue("Software\\ER System\\UserAccount", {"TotalDays"})(0)) = False Then
                    txtTotalNumberOfDays.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"TotalDays"})(0)
                End If
                If txtCategory.SelectedIndex = 0 Then
                    txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"TranspoRate"})(0)
                ElseIf txtCategory.SelectedIndex = 1 Then
                    txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"LunchRate"})(0)
                End If
            End If
        End If
    End Sub
    Private Sub OpeningLead(Optional ByVal WorkWith As String = "", Optional ByVal ExpenseID As String = "")
        If CBPerdiem.Checked = False Then
            If comboClick = 1 Then
                If txtCategory.SelectedIndex = 0 Then
                    If WorkWith = "" Then
                        MsgBox("Please fill WorkWith to Proceed")
                        txtCategory.SelectedItem = Nothing
                    Else
                        Call ModDataStore.OnOffControl(False)
                        GBTransportation.Location = New Point(97, 410)
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
                        Call ModDataStore.OnOffControl(False)
                        GBMeals.Location = New Point(97, 410)
                        GBMeals.Visible = True
                        GBTransportation.Visible = False
                        GBMeals.BringToFront()
                        Call MealDataValidation(WorkWith, ExpenseID)
                        txtCategory.Enabled = False
                        BTNEditCategory.Enabled = True
                        btnExpenseUpdate.Visible = False
                        btnExpenseSave.Visible = False
                    End If

                ElseIf txtCategory.SelectedIndex = 4 Then
                    txtParticulars.Enabled = True
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Select()
                ElseIf txtCategory.SelectedIndex = 2 Then
                    txtParticulars.Enabled = False
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtParticulars.Select()
                ElseIf txtCategory.SelectedIndex = 3 Then
                    txtParticulars.Enabled = False
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
            Else
                If txtCategory.SelectedIndex = 4 Then
                    txtParticulars.Enabled = True
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Select()
                    BTNEditCategory.Enabled = False
                    txtCategory.Enabled = True
                ElseIf txtCategory.SelectedIndex = 2 Then
                    txtParticulars.Enabled = False
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtParticulars.Select()
                    BTNEditCategory.Enabled = False
                ElseIf txtCategory.SelectedIndex = 3 Then
                    txtParticulars.Enabled = False
                    txtExpenseAmount.Enabled = True
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtParticulars.Select()
                    BTNEditCategory.Enabled = False
                End If
            End If
        Else
            If txtCategory.SelectedIndex = 0 Then
                If WorkWith = "" Then
                    MsgBox("Please fill WorkWith to Proceed")
                Else
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"TranspoRate"})(0)
                    txtExpenseAmount.Enabled = False
                    BTNEditCategory.Enabled = False
                End If
            ElseIf txtCategory.SelectedIndex = 1 Then
                If WorkWith = "" Then
                    MsgBox("Please fill WorkWith to Proceed")
                Else
                    txtParticulars.Text = txtCategory.SelectedItem
                    txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"LunchRate"})(0)
                    txtExpenseAmount.Enabled = False
                    BTNEditCategory.Enabled = False
                End If
            End If
        End If
    End Sub
    Private Sub TransportationDataValidation(ByVal ExpenseID As String)
        Dim ClsData As New ClsLoadData
        If ExpenseID <> Nothing Then
            Dim myERData As String()
            myERData = ClsData.GetEReportDetails(IIf(
                ClsData.TempFileValidation(
                Application.StartupPath + "\expenseTransSettingsTEMP.txt"),
                Application.StartupPath + "\expenseTransSettingsTEMP.txt",
                Application.StartupPath + "\expenseTransSettings.txt"))

            If myERData.Length <> 0 Then
                ModDataStore.MyFare()
                If myERData(1) = 4 Then
                    CBBFare.SelectedValue = myERData(1)
                    txtFrom.Text = myERData(2)
                    txtTo.Text = myERData(3)
                    BTNEditCategory.Enabled = False
                    txtFrom.Enabled = False
                    txtTo.Enabled = False
                Else
                    CBBFare.SelectedValue = myERData(1)
                    txtFrom.Text = myERData(2)
                    txtTo.Text = myERData(3)
                    BTNEditCategory.Enabled = False
                    txtFrom.Enabled = True
                    txtTo.Enabled = True
                End If
            Else
                ModDataStore.MyFare()
                CBBFare.SelectedIndex = 0
                txtFrom.Clear()
                txtTo.Clear()
                BTNEditCategory.Enabled = False
                txtFrom.Enabled = True
                txtTo.Enabled = True
            End If
        Else
            Dim myERData As String() = {}
            If ClsData.TempFileValidation(Application.StartupPath + "\expenseTransSettingsTEMP.txt") = True Then
                myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseTransSettingsTEMP.txt")
            End If
            If myERData.Length <> 0 Then
                ModDataStore.MyFare()
                CBBFare.SelectedValue = myERData(1)
                txtFrom.Text = myERData(2)
                txtTo.Text = myERData(3)
            Else
                ModDataStore.MyFare()
                CBBFare.SelectedIndex = 0
                txtFrom.Clear()
                txtTo.Clear()
                BTNEditCategory.Enabled = False
            End If
            txtExpenseAmount.Select()
            txtTotal.Clear()
        End If
    End Sub
    Private Sub MealDataValidation(ByVal WorkWith As String, ByVal ExpenseID As String)
        Dim ClsData As New ClsLoadData
        If WorkWith <> "NONE" And WorkWith <> "" Then
            GetWorkWith(WorkWith)
            If ExpenseID <> Nothing Then
                Dim myERData As String()
                myERData = ClsData.GetEReportDetails(IIf(
                ClsData.TempFileValidation(
                Application.StartupPath + "\expenseMealSettingsTEMP.txt") = True,
                Application.StartupPath + "\expenseMealSettingsTEMP.txt",
                Application.StartupPath + "\expenseMealSettings.txt"))
                If ExpenseID.Length <> 0 Then
                    SetUserMealExpenseItem(myERData)
                Else
                    If modLoadingData.GetUserMeal = "" Then
                        ModDataStore.GetUserExpenseMeal()
                        CBBPaidFor.Enabled = True
                    Else
                        SetUserMealExpenseItem(myERData)
                    End If
                End If
            Else
                Dim myERData As String() = {}
                If ClsData.TempFileValidation(Application.StartupPath + "\expenseMealSettingsTEMP.txt") = True Then
                    myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseMealSettingsTEMP.txt")
                End If
                If myERData.Length <> 0 Then
                    SetUserMealExpenseItem(myERData)
                Else
                    ModDataStore.GetUserExpenseMeal()
                End If
                If CLBMeals.CheckedIndices.Count = 1 Then
                    CBBPaidFor.Enabled = True
                Else
                    CBBPaidFor.Enabled = False
                End If
            End If
            CBBPaidFor.Visible = True
            CLBPaidBill.Visible = True
        ElseIf WorkWith = "NONE" Then
            GetWorkWith(WorkWith)
            If ExpenseID <> Nothing Then
                Dim myERData As String()
                myERData = ClsData.GetEReportDetails(IIf(
                ClsData.TempFileValidation(
                Application.StartupPath + "\expenseMealSettingsTEMP.txt") = True,
                Application.StartupPath + "\expenseMealSettingsTEMP.txt",
                Application.StartupPath + "\expenseMealSettings.txt"))
                If ExpenseID.Length <> 0 Then
                    SetUserMealExpenseItem(myERData)
                    Call SharedProcedure()
                    Call UserMealSettingsWithOutWorkWith()
                Else
                    ModDataStore.GetUserExpenseMeal()
                    Call UserMealSettingsWithOutWorkWith()
                End If
            Else
                If ClsData.TempFileValidation(Application.StartupPath + "\expenseMealSettingsTEMP.txt") = True Then
                    Dim myERData As String()
                    myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseMealSettingsTEMP.txt")
                    SetUserMealExpenseItem(myERData)
                    Call SharedProcedure()
                    Call UserMealSettingsWithOutWorkWith()
                Else
                    ModDataStore.GetUserExpenseMeal()
                    Call UserMealSettingsWithOutWorkWith()
                End If
            End If
        End If
    End Sub
    Private Sub UserMealSettingsWithOutWorkWith()
        CBBPaidFor.Enabled = False
        CBBPaidFor.Visible = False
        CLBPaidBill.Visible = False
        CLBMeals.Enabled = True
    End Sub
    Private Sub SetUserMealExpenseItem(ByVal myERData As String())
        If myERData(1).Contains("&") = True Then
            Call ResetMeal()
            For x = 0 To myERData(1).Split("&").Count - 1
                If myERData(1).Split("&")(x).Split("^")(1) = "Dinner" Then
                    CBDinnerOTMeal.Checked = True
                    RBDinner.Checked = True
                ElseIf myERData(1).Split("&")(x).Split("^")(1) = "OT Meal" Then
                    CBDinnerOTMeal.Checked = True
                    RBOTMeal.Checked = True
                Else
                    CLBMeals.SetItemChecked(myERData(1).Split("&")(x).Split("^")(0), True)
                End If
            Next
        ElseIf myERData(2) = 0 Then
            Call ResetMeal()
            If myERData(1).Split("^")(1) = "Dinner" Then
                CBDinnerOTMeal.Checked = True
                RBDinner.Checked = True
                CBBPaidFor.Enabled = True
            ElseIf myERData(1).Split("^")(1) = "OT Meal" Then
                CBDinnerOTMeal.Checked = True
                RBOTMeal.Checked = True
                CBBPaidFor.Enabled = True
            Else
                CLBMeals.SetItemChecked(myERData(1).Split("^")(0), True)
                CBBPaidFor.Enabled = True
            End If

        ElseIf myERData(2) = 1 Then
            Call ResetMeal()
            If myERData(1).Split("^")(1) = "Dinner" Then
                CBDinnerOTMeal.Checked = True
                RBDinner.Checked = True
                CBBPaidFor.Checked = True
                CBBPaidFor.Enabled = False
            ElseIf myERData(1).Split("^")(1) = "OT Meal" Then
                CBDinnerOTMeal.Checked = True
                RBOTMeal.Checked = True
                CBBPaidFor.Checked = True
                CBBPaidFor.Enabled = False
            Else
                CLBMeals.SetItemChecked(myERData(1).Split("^")(0), True)
                CBBPaidFor.Checked = True
                CBBPaidFor.Enabled = False
            End If

            For x = 0 To myERData(3).Split("&").Count - 1
                CLBPaidBill.SetItemChecked(myERData(3).Split("&")(x).Split("^")(0), True)
            Next
        Else
            Call ModDataStore.GetUserExpenseMeal()
        End If
    End Sub
    Private Sub ResetMeal()
        CLBMeals.SetItemChecked(0, False)
        CLBMeals.SetItemChecked(1, False)
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
    Private Sub BTNTransportation_Click(sender As Object, e As EventArgs) Handles BTNTransportation.Click
        Dim ClsData As New ClsLoadData
        If txtFrom.Text = "" Or txtTo.Text = "" Then
            MsgBox("Please fill all Fields")
        ElseIf CBBFare.SelectedValue = 4 Then
            txtParticulars.Text = "Transportation"
            txtExpenseAmount.Text = GetRegistryValue("Software\\ER System\\UserAccount", {"TranspoRate"})(0)
            GBTransportation.Visible = False
            If ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") = True Then
                Dim myExpenseData As String()
                myExpenseData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
                ClsData.SetExpenseTransDetailsTemp({myExpenseData(0), CBBFare.SelectedValue, txtFrom.Text, txtTo.Text, CBBFare.SelectedValue.ToString() + "/" + txtFrom.Text + "/" + txtTo.Text})
                txtExpenseAmount.Enabled = False
            Else
                ClsData.SetExpenseTransDetailsTemp({"0", CBBFare.SelectedValue, txtFrom.Text, txtTo.Text, CBBFare.SelectedValue.ToString() + "/" + txtFrom.Text + "/" + txtTo.Text})
                txtExpenseAmount.Enabled = False
            End If
        Else
            GBTransportation.Visible = False
            txtParticulars.Text = txtFrom.Text + " To " + txtTo.Text + " (" + CBBFare.Text + ")"
            TPExpenseReport.Enabled = True
            If ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") = True Then
                Dim myExpenseData As String()
                myExpenseData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
                ClsData.SetExpenseTransDetailsTemp({myExpenseData(0), CBBFare.SelectedValue, txtFrom.Text, txtTo.Text, CBBFare.SelectedValue.ToString() + "/" + txtFrom.Text + "/" + txtTo.Text})
                txtExpenseAmount.Enabled = True
            Else
                ClsData.SetExpenseTransDetailsTemp({"0", CBBFare.SelectedValue, txtFrom.Text, txtTo.Text, CBBFare.SelectedValue.ToString() + "/" + txtFrom.Text + "/" + txtTo.Text})
                txtExpenseAmount.Enabled = True
            End If
        End If
        Call ModDataStore.OnOffControl(True)
        Dim myERData As String() = {}
        If ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") Then
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
        End If
        If myERData.Length <> 0 Then
            btnExpenseSave.Visible = False
            btnExpenseUpdate.Visible = True
        Else
            btnExpenseSave.Visible = True
            btnExpenseUpdate.Visible = False
        End If
        txtExpenseAmount.Select()
    End Sub
    Private Sub txtCategory_Click(sender As Object, e As EventArgs) Handles txtCategory.Click
        comboClick = 1
    End Sub
    Private Sub GBMeals_KeyDown(sender As Object, e As KeyEventArgs) Handles GBMeals.KeyDown
        If e.KeyCode = Keys.Escape Then
            GBMeals.Visible = False
            comboClick = 0
            CLBMeals.Enabled = True
            CBBPaidFor.Checked = False
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

    Private Sub BTNFareClose_Click(sender As Object, e As EventArgs) Handles BTNFareClose.Click
        Dim ClsData As New ClsLoadData
        Call ModDataStore.OnOffControl(True)
        Dim myERData As String() = {}
        If ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") Then
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
        End If
        If myERData.Length <> 0 Then
            btnExpenseSave.Visible = False
            btnExpenseUpdate.Visible = True
        Else
            btnExpenseSave.Visible = True
            btnExpenseUpdate.Visible = False
        End If
        txtExpenseAmount.Select()
        GBTransportation.Visible = False
        CBBFare.DropDownStyle = ComboBoxStyle.DropDownList
        'txtCategory.Enabled = True
        'btnExpenseSave.Visible = False
        'btnExpenseUpdate.Visible = True
        'comboClick = "0"
    End Sub

    Private Sub BTNMealClose_Click(sender As Object, e As EventArgs) Handles BTNMealClose.Click
        Call ModDataStore.OnOffControl(True)
        GBMeals.Visible = False
        txtCategory.Enabled = True
        btnExpenseSave.Visible = False
        btnExpenseUpdate.Visible = True
        RTBNotification.Visible = False
        lblParticulars.Visible = True
        txtParticulars.Visible = True
    End Sub

    Private Sub BTNEditCategory_Click(sender As Object, e As EventArgs) Handles BTNEditCategory.Click
        Dim ClsData As New ClsLoadData
        comboClick = "1"
        If CBPerdiem.Checked = True Then
            ComputationLead()
        Else
            txtParticulars.Size = New Size(199, 68)
            If ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") = True Then
                Dim myERData As String()
                myERData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseSettings.txt")
                Call OpeningLead(myERData(14), myERData(16))
            Else
                Call OpeningLead(txtWorkWith.Text, "")
            End If
        End If
    End Sub

    Private Sub CBBFare_SelectedValueChanged(sender As Object, e As EventArgs) Handles CBBFare.SelectedValueChanged
        If FareComboValidation = "1" Then
            If CBBFare.SelectedValue = 2 Then
                CBBFare.DropDownStyle = ComboBoxStyle.DropDown
                FareComboValidation = "1"
                BTNAddFare.Text = "Save"
                CBBFare.DataSource = Nothing
                CBBFare.Text = ""
                BTNAddFare.Enabled = True
                CBBFare.Select()

            ElseIf CBBFare.SelectedValue = 4 Then
                txtFrom.Text = "Allowance"
                txtTo.Text = "Allowance"
                txtFrom.Enabled = False
                txtTo.Enabled = False
                BTNAddFare.Enabled = False
            Else
                txtFrom.Text = ""
                txtTo.Text = ""
                txtFrom.Enabled = True
                txtTo.Enabled = True
                BTNAddFare.Enabled = False
            End If
        End If
    End Sub
    Private Sub RBDinnerOTMeal_CheckedChanged(sender As Object, e As EventArgs) Handles CBDinnerOTMeal.CheckedChanged
        Dim str As String = modLoadingData.LoadNotification(dtpExpenseDate.Text,
                GetRegistryValue("Software\\ER System\\UserAccount", {"Username"})(0),
                "")

        If str <> "" Then
            Dim strSplit As String() = str.Split("/")
            For x = 0 To UBound(strSplit)
                If strSplit(x).Split("^")(2) = "Dinner" Or strSplit(x).Split("^")(2) = "OT Meal" Then
                    If counter = "" Then
                        counter = "1"
                        MsgBox(strSplit(x).Split("^")(2) + " Meal is Already Filed by " & strSplit(x).Split("^")(1))
                        CBDinnerOTMeal.Checked = False

                    Else
                        counter = ""
                        CBDinnerOTMeal.Checked = False
                    End If

                End If
            Next
        Else
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
        End If
    End Sub
    Private Sub CBBFare_Click(sender As Object, e As EventArgs) Handles CBBFare.Click
        FareComboValidation = "1"
    End Sub
    Private Sub txtInvoice_TextChanged(sender As Object, e As EventArgs) Handles txtInvoice.TextChanged
        Dim ClsData As New ClsLoadData
        Dim myExpenseData As String()
        If ClsData.TempFileValidation(Application.StartupPath + "\expenseMealSettings.txt") = True Then
            myExpenseData = ClsData.GetEReportDetails(Application.StartupPath + "\expenseMealSettings.txt")
            If myExpenseData(2) = 1 Then
                txtExpenseAmount.Enabled = True
            Else
                If txtCategory.SelectedIndex = 0 Then
                    txtExpenseAmount.Enabled = True
                Else
                    txtExpenseAmount.Enabled = False
                End If
            End If
        End If
        If txtInvoice.Text <> "" And txtMultiplier.Text = 1 Then
            txtTotal.Text = Val(txtExpenseAmount.Text)
        End If
    End Sub

    Private Sub btnFileReport_Click(sender As Object, e As EventArgs) Handles btnFileReport.Click
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")

        LoadingExpenseCount(myERData(13))
        If LoadingOfficersToSign(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0)) = Nothing Then
            MsgBox("Please Insert Your Signatory " & vbNewLine & " Go to Account Settings > Signatory", TopMost = True)
        ElseIf LoadingExpenseCount(myERData(13)) = 0 Then
            MsgBox("No Expense Data to File", TopMost = True)
        ElseIf myERData(12) = "New" Or myERData(12) = "Returned" Then
            Dim y As MsgBoxResult
            y = MessageBox.Show("Are you sure you want to File your Expense Report?" + vbNewLine + "You can not update the expense once you file", "File", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
            If y = MsgBoxResult.Yes Then
                RefileER(myERData(13), "1")
                Call UpdateEReportData()
                ClsData.SetEReportDetails(myERData(13))
                Call EReportOpenValidation()
            Else
                Exit Sub
            End If
        ElseIf myERData(12) = "Approved" Or myERData(12) = "For Approval" Then
            Dim y As MsgBoxResult
            y = MessageBox.Show("Are you sure you want to Update your Expense Report?", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
            If y = MsgBoxResult.Yes Then
                Call UpdatePrintStatus(myERData(13))
                DeleteImage(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), myERData(13))
                ClsData.SetEReportDetails(myERData(13))
                Call UpdateEReportData()
                Call EReportOpenValidation()
            Else
                Exit Sub
            End If
        End If
    End Sub

    Private Sub btnPrintPreview_Click(sender As Object, e As EventArgs) Handles btnPrintPreview.Click
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        If LoadingOfficersToSign(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0)) = Nothing Then
            MsgBox("Please Insert Your Signatory " & vbNewLine & " Go to Account Settings > Signatory", TopMost = True)
        Else
            If myERData(13) = Nothing Then
                MsgBox("No Report Selected")
            ElseIf LoadingExpenseCount(myERData(13)) = 0 Then
                MsgBox("No Expense Data to Load", TopMost = True)
            Else
                frmRpt.ShowDialog()
            End If
        End If
    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles txtTotalNumberOfDays.TextChanged
        If txtExpenseAmount.Text <> "" And txtMDays.Text <> "" And txtMultiplier.Text <> "" And txtTotalNumberOfDays.Text <> "" Then
            Dim particulars As String = ""
            If CBPerdiem.Checked = True Then
                If txtMDays.Text <> "0" And txtMDays.Text <> "" Then
                    particulars = particulars + " (" + txtTotalNumberOfDays.Text + "Days - " + txtMDays.Text + "Days) * " + txtExpenseAmount.Text
                    txtMultiplier.Text = Val(txtTotalNumberOfDays.Text - txtMDays.Text)
                Else
                    particulars = particulars + " (" + txtTotalNumberOfDays.Text + "Days) * " + txtExpenseAmount.Text
                    txtMultiplier.Text = Val(txtTotalNumberOfDays.Text - txtMDays.Text)
                End If
                txtComputation.Text = particulars
            End If
        End If
    End Sub

    Private Sub txtMDays_TextChanged(sender As Object, e As EventArgs) Handles txtMDays.TextChanged
        If txtExpenseAmount.Text <> "" And txtTotalNumberOfDays.Text <> "" And txtMultiplier.Text <> "" And txtMDays.Text <> "" Then
            Dim particulars As String = ""
            If CBPerdiem.Checked = True Then
                If txtMDays.Text <> "0" And txtMDays.Text <> "" Then
                    particulars = particulars + " (" + txtTotalNumberOfDays.Text + "Days - " + txtMDays.Text + "Days) * " + txtExpenseAmount.Text
                    txtMultiplier.Text = Val(txtTotalNumberOfDays.Text - txtMDays.Text)
                Else
                    particulars = particulars + " (" + txtTotalNumberOfDays.Text + "Days) * " + txtExpenseAmount.Text
                    txtMultiplier.Text = Val(txtTotalNumberOfDays.Text - txtMDays.Text)
                End If
                txtComputation.Text = particulars
            End If
        End If
    End Sub
    Private Sub BTNComputation_Click(sender As Object, e As EventArgs) Handles BTNComputation.Click
        If txtMultiplier.Text = 0 Or txtMultiplier.Text < 0 Then
            MsgBox("0 or Less than 0 Multiplier is Not Allowed!")
        Else
            If txtTotalNumberOfDays.Text.Equals("") And txtTotal.Text.Equals("") Then
                MsgBox("Please Fill all Fields.")
            Else
                Dim ClsData As New ClsLoadData
                BTNEditCategory.Enabled = True
                txtCategory.Enabled = False
                GBAllowance.Visible = False
                txtMultiplier.Enabled = False
                Dim ValueName As String() = {"TotalDays"}
                Dim Value As String() = {txtTotalNumberOfDays.Text}
                ClsData.RegistrySettings("HKEY_CURRENT_USER\Software\ER System", "UserAccount", ValueName, Value)
                If ClsData.RegistryGetValue("Software\\ER System\\UserAccount", {"emp_Dept"})(0) = "IMS" Then
                    txtExpenseAmount.Enabled = True
                End If
            End If
        End If
    End Sub
    Private Sub BTNCloseComputation_Click(sender As Object, e As EventArgs) Handles BTNCloseComputation.Click
        Dim ClsData As New ClsLoadData
        If ClsData.TempFileValidation(Application.StartupPath + "\expenseSettings.txt") = True Then
            GBAllowance.Visible = False
        Else
            txtCategory.SelectedItem = Nothing
            GBAllowance.Visible = False
            txtParticulars.Clear()
        End If
    End Sub
    Private Sub CLBMeals_SelectedValueChanged(sender As Object, e As EventArgs) Handles CLBMeals.SelectedValueChanged
        Dim str As String = modLoadingData.LoadNotification(dtpExpenseDate.Text,
            GetRegistryValue("Software\\ER System\\UserAccount", {"Username"})(0),
            "")
        If str <> "" Then
            Dim strSplit As String() = Split(str, "/")
            For x = 0 To UBound(strSplit)
                If CLBMeals.SelectedIndex = 0 And strSplit(x).Split("^")(2) = "Breakfast" Then
                    MsgBox("Lunch Meal is Already Filed by " & strSplit(x).Split("^")(1))
                    CLBMeals.SetItemChecked(0, False)
                ElseIf CLBMeals.SelectedIndex = 1 And strSplit(x).Split("^")(2) = "Lunch" Then
                    MsgBox("Lunch Meal is Already Filed by " & strSplit(x).Split("^")(1))
                    CLBMeals.SetItemChecked(1, False)
                End If
            Next
        Else
            If txtWorkWith.Text <> "NONE" Then
                Dim Num As Integer
                Num = CLBMeals.CheckedItems.Count
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
        End If
    End Sub
End Class