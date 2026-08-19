Public Class frmMain
    Dim SearchButtonValidation As String = "0"
    Private ReadOnly _selectedReportContextService As New AppServices.SelectedReportContextService()
    Private ReadOnly _financeReviewService As ERSystem.Infrastructure.Data.IFinanceReviewService = New ERSystem.Infrastructure.Data.FinanceReviewService()
    Private ReadOnly _financeMenuItem As New ToolStripMenuItem("Finance Review")
    Private ReadOnly _accountSettingsMenuItem As New ToolStripMenuItem("My Account Settings")

    Private Sub frmMain_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Application.Exit()
    End Sub
    Private Sub UpdatePrintStatus()
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        DBConnection()
        Using sqlcmdPrintStatus As New SqlClient.SqlCommand
            Using SQLConnection As SqlClient.SqlConnection = mConn.SQLConnection
                With sqlcmdPrintStatus
                    .Connection = SQLConnection
                    .CommandText = "UPDATE tbReportDetails SET ReportPrintStatus ='1',ReportNumberStatus = '0' where ID='" & myERData(13) & "'"
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Private Sub ActiveFormsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ActiveFormsToolStripMenuItem.Click
        SetRegistry("Settings", {"ChangeLoading"}, {"1"})
        frmApprove.ShowDialog()
    End Sub
    Private Sub PreviousFormsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PreviousFormsToolStripMenuItem.Click
        LoadingUserAccountPending(GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0))
        With frmApprove
            .ShowDialog()
            SetRegistry("Settings", {"ChangeLoading"}, {"0"})
            .BringToFront()
            .btnApprove.Enabled = False
            .btnReject.Enabled = False
        End With
    End Sub
    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs)
        MsgBox("IMS")
    End Sub
    Private Sub Logout()
        Me.Hide()
        frmLogin.Show()
        Me.Enabled = False
        frmLogin.txtUsername.Focus()
        ttuser.Text = Nothing
        tsslUserDept.Text = Nothing
        frmUserRegistration.Close()
    End Sub
    Private Sub LogoutdupAcc()
        DBConnection()
        Using dt As New DataTable
            Using sqlcmdDupAcc As New SqlClient.SqlCommand
                Using SQLConnection As SqlClient.SqlConnection = mConn.SQLConnection
                    With sqlcmdDupAcc
                        .Connection = SQLConnection
                        .CommandText = "update tbUserRegistration set [Status] = '0' where UserID = '" & GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0) & "'"
                        .ExecuteNonQuery()
                        Timer2.Enabled = False
                        Timer2.Stop()
                    End With
                End Using
            End Using
        End Using
    End Sub
    Private Sub frmMain_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Dim ClsData As New ClsLoadData
        If (e.Control AndAlso e.KeyValue = Keys.L) = True Then
            frmLogin.txtUsername.Clear()
            frmLogin.txtPassword.Clear()
            Logout()
            Me.DgvReportDetails.DataSource = Nothing
            ClsData.DeleteEReportDetails(Application.StartupPath + "\settings.txt")
            ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseSettings.txt")
            ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
            ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettingsTEMP.txt")
        ElseIf (e.KeyValue = Keys.Escape) = True Then
            If ModDataStore.FormSettings <> "1" Then
                modLoadingData.sDate = DateAndTime.Now
                modLoadingData.eDate = DateAndTime.Now
                ClsData.DeleteEReportDetails(Application.StartupPath + "\settings.txt")
                ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseSettings.txt")
                ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
                ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettingsTEMP.txt")
            End If
        ElseIf (e.Control AndAlso e.KeyValue = Keys.F5) Then
            frmConnection.Show()
            Me.Hide()
        End If
    End Sub
    Private Sub btnReportData_Click_1(sender As Object, e As EventArgs) Handles btnReportData.Click
        _selectedReportContextService.Clear()
        If FrmEReportDetailsV2.ShowDialog() = DialogResult.OK Then
            RefreshEReportData()
        End If
    End Sub
    Private Sub PictureBox2_Click(sender As Object, e As EventArgs) Handles PictureBox2.Click
        Dim ClsData As New ClsLoadData
        frmLogin.txtUsername.Clear()
        frmLogin.txtPassword.Clear()
        Logout()
        Me.DgvReportDetails.DataSource = Nothing
        _selectedReportContextService.Clear()
        ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseSettings.txt")
        ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettings.txt")
        ClsData.DeleteEReportDetails(Application.StartupPath + "\expenseTransSettingsTEMP.txt")
        frmEReport.Close()
    End Sub
    Private Sub PictureBox4_Click(sender As Object, e As EventArgs) Handles PictureBox4.Click
        Dim x As String
        x = MessageBox.Show("Are you sure you want to exit?", "Exit?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If x = DialogResult.Yes Then
            Me.Close()
        Else
        End If
    End Sub
    Private Sub ToolStripButton1_DoubleClick(sender As Object, e As EventArgs) Handles btnSearchOpen.DoubleClick
        DgvReportDetails.Visible = False
    End Sub
    Private Sub ChangePasswordToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ChangePasswordToolStripMenuItem1.Click
        frmChangePassword.ShowDialog()
    End Sub
    Private Sub SignatoryToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SignatoryToolStripMenuItem.Click
        frmDeptSignature.ShowDialog()
    End Sub
    Private Sub UserAccountToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles UserAccountToolStripMenuItem.Click
        Try
            LoadingUserAccount(GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0))
            frmUserRegistration.Show()
            frmUserRegistration.dgvUserAccount.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            frmUserRegistration.dgvUserAccount.Columns("Signature").Visible = False
            frmUserRegistration.dgvUserAccount.Columns("EmailBCC").Visible = False
            frmUserRegistration.dgvUserAccount.Columns("DeptID").Visible = False
            frmUserRegistration.dgvUserAccount.Columns("Email Address").Visible = False
            frmUserRegistration.dgvUserAccount.Columns("EmailPass").Visible = False
            frmUserRegistration.dgvUserAccount.Columns("Password").Visible = False
            frmUserRegistration.dgvUserAccount.Columns("Number of file").Visible = False
        Catch ex As Exception
        End Try
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        lbltime.Text = TimeOfDay
    End Sub
    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles tsmiPrev.Click
        frmPreviousER.ShowDialog()
        frmPreviousER.TopMost = True
        Me.TopMost = False
    End Sub
    Private Sub AboutToolStripMenuItem_Click_1(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("On-Going")
    End Sub
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        DBConnection()
        Using dt As New DataTable
            Using sqlcmdLoadLogin As New SqlClient.SqlCommand
                Using SQLConnection As SqlClient.SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadLogin
                        .Connection = SQLConnection
                        .CommandText = "select a.[Status],a.UserID from tbUserRegistration as a where UserID ='" & GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0) & "'"
                        .CommandType = CommandType.Text
                        dt.Load(.ExecuteReader)
                    End With
                End Using
            End Using
        End Using
    End Sub
    Private Sub HelpToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles HelpToolStripMenuItem1.Click
        frmHelp.Show()
        frmHelp.TopMost = True
        Me.TopMost = False
    End Sub
    Private Sub DgvReportDetails_KeyDown(sender As Object, e As KeyEventArgs) Handles DgvReportDetails.KeyDown
        If e.KeyCode = Keys.Escape Then
            Me.btnPrintPreview.Enabled = False
            Me.btnFileReport.Enabled = False
            Me.btnFileReport.Text = "File Report"
        End If
    End Sub
    Private Sub FToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MenuExpenseDetails.Click
        frmExpenseDetails.ShowDialog()
        frmExpenseDetails.TopMost = True
        Me.TopMost = False
    End Sub
    Private Sub ReportsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles fmsExpenseSummary.Click
        frmExpenseSummary.ShowDialog()
    End Sub

    Private Sub DgvReportDetails_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles DgvReportDetails.CellDoubleClick

        If GetRegistryValue("Software\\ER System\\UserAccount", {"BreakFastRate"})(0) = "" And
            GetRegistryValue("Software\\ER System\\UserAccount", {"DinnerRate"})(0) = "" And
            GetRegistryValue("Software\\ER System\\UserAccount", {"LunchRate"})(0) = "" And
            GetRegistryValue("Software\\ER System\\UserAccount", {"OTMeal"})(0) = "" Then
            MsgBox("Please Contact IMS Administrator to verify your Meal Rates.", Title:="Help")
        ElseIf GetRegistryValue("Software\\ER System\\UserAccount", {"BreakFastRate"})(0) = "" Then
            Dim datatb As DataTable = LoadingUserAccDept(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))

            If datatb.Rows.Count <> 0 Then
                If datatb.Rows(0).Item("emp_Endorser") = "" And
                    datatb.Rows(0).Item("emp_Reviewer") = "" And
                    datatb.Rows(0).Item("emp_Approver") = "" Then
                    MsgBox("Please Add your Signatory. Go to Account Settings > Signatory", MsgBoxStyle.MsgBoxHelp)
                End If
            End If
        Else
            Dim ClsData As New ClsLoadData
            Try
                If e.RowIndex < 0 Then
                    Exit Sub
                Else
                    ClsData.DeleteEReportDetails(Application.StartupPath + "\settings.txt")
                    ClsData.SetEReportDetails(DgvReportDetails.Rows(e.RowIndex).Cells("Report ID").Value)
                    DGVLoadExpenseReport(EReportOpenValidation(), GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                    ClsData.RegistrySettings("HKEY_CURRENT_USER\Software\ER System", "Settings", {"Approver"}, {"0"})
                    frmEReport.ShowDialog()
                End If
            Catch ex As Exception
                MsgBox(ex.ToString)
            End Try
        End If
    End Sub
    Private Sub DgvReportDetails_CellMouseDown(sender As Object, e As DataGridViewCellMouseEventArgs) Handles DgvReportDetails.CellMouseDown
        Dim ClsData As New ClsLoadData
        If e.RowIndex < 0 Then
        Else
            If e.Button = MouseButtons.Right Then
                DgvReportDetails.Rows(e.RowIndex).Selected = True
                DgvReportDetails.CurrentCell = DgvReportDetails.Rows(e.RowIndex).Cells(1)
                ClsData.SetEReportDetails(DgvReportDetails.Rows(e.RowIndex).Cells("Report ID").Value)
                ContextMenuStrip1.Show(DgvReportDetails, e.Location)
                ContextMenuStrip1.Show(Cursor.Position)
            End If
        End If
    End Sub

    Private Sub EditReportDetailsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditReportDetailsToolStripMenuItem.Click
        If DgvReportDetails.CurrentRow Is Nothing Then
            Return
        End If

        _selectedReportContextService.Save(DgvReportDetails.CurrentRow.Cells("Report ID").Value.ToString())
        If FrmEReportDetailsV2.ShowDialog() = DialogResult.OK Then
            RefreshEReportData()
        End If
    End Sub

    Private Sub BtnSearch_Click(sender As Object, e As EventArgs) Handles BtnSearch.Click
        UpdateEReportData()
    End Sub

    Private Sub BTNReset_Click(sender As Object, e As EventArgs) Handles BTNReset.Click
        DgvReportDetails.DataSource = Nothing
        TxtSearchBy.Clear()
        CBBFilter.SelectedItem = Nothing
        Call ReleasMemory()
    End Sub

    Private Sub EditExpensesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditExpensesToolStripMenuItem.Click
        Dim ClsData As New ClsLoadData
        Try
            If DgvReportDetails.Rows.IndexOf(DgvReportDetails.CurrentRow) < 0 Then
                Exit Sub
            Else
                _selectedReportContextService.Save(DgvReportDetails.Rows(DgvReportDetails.Rows.IndexOf(DgvReportDetails.CurrentRow)).Cells("Report ID").Value.ToString())
                DGVLoadExpenseReport(EReportOpenValidation(), GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
                ClsData.RegistrySettings("HKEY_CURRENT_USER\Software\ER System", "Settings", {"Approver"}, {"0"})
                frmEReport.ShowDialog()
            End If
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CBBFilter.SelectedIndex = 0
        ConfigureAccountSettingsMenu()
        ConfigureFinanceMenu()
        ShowMissingPhysicalReceiptsPrompt()
    End Sub

    Private Sub ResendingEmailToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ResendingEmailToolStripMenuItem.Click
        frmERType.ShowDialog()
    End Sub

    Private Sub ConfigureFinanceMenu()
        If Not MenuForms.DropDownItems.Contains(_financeMenuItem) Then
            AddHandler _financeMenuItem.Click, AddressOf FinanceMenuItem_Click
            MenuForms.DropDownItems.Add(_financeMenuItem)
        End If

        Dim userLevel As String = GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0)
        _financeMenuItem.Visible = String.Equals(userLevel, "Finance", StringComparison.OrdinalIgnoreCase)
    End Sub

    Private Sub FinanceMenuItem_Click(sender As Object, e As EventArgs)
        Using financeForm As New frmFinanceErfReview()
            financeForm.ShowDialog(Me)
        End Using
    End Sub

    Private Sub ConfigureAccountSettingsMenu()
        If Not MenuFile.DropDownItems.Contains(_accountSettingsMenuItem) Then
            _accountSettingsMenuItem.Image = CreateAccountSettingsIcon()
            AddHandler _accountSettingsMenuItem.Click, AddressOf AccountSettingsMenuItem_Click
            MenuFile.DropDownItems.Insert(0, _accountSettingsMenuItem)
        End If

        _accountSettingsMenuItem.Visible = Not String.IsNullOrWhiteSpace(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
    End Sub

    Private Sub AccountSettingsMenuItem_Click(sender As Object, e As EventArgs)
        Using accountSettingsForm As New frmAccountSettings()
            accountSettingsForm.ShowDialog(Me)
        End Using
    End Sub

    Private Function CreateAccountSettingsIcon() As Image
        Dim icon As New Bitmap(18, 16)

        Using graphics As Graphics = Graphics.FromImage(icon)
            graphics.Clear(Color.Transparent)
            graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            Using userBrush As New SolidBrush(Color.FromArgb(72, 128, 190))
            Using bodyBrush As New SolidBrush(Color.FromArgb(93, 156, 221))
            Using gearBrush As New SolidBrush(Color.FromArgb(95, 95, 95))
            Using highlightPen As New Pen(Color.White, 1.0F)
                graphics.FillEllipse(userBrush, 3, 2, 6, 6)
                graphics.FillPie(bodyBrush, 1, 8, 10, 8, 180, 180)
                graphics.DrawArc(highlightPen, 2, 8, 8, 6, 200, 140)

                graphics.FillEllipse(gearBrush, 11, 7, 6, 6)
                graphics.FillRectangle(gearBrush, 13, 5, 2, 10)
                graphics.FillRectangle(gearBrush, 9, 9, 10, 2)
                graphics.FillEllipse(Brushes.White, 13, 9, 2, 2)
            End Using
            End Using
            End Using
            End Using
        End Using

        Return icon
    End Function

    Private Sub ShowMissingPhysicalReceiptsPrompt()
        Try
            Dim userLevel As String = GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0)
            If Not String.Equals(userLevel, "User", StringComparison.OrdinalIgnoreCase) Then
                Return
            End If

            Dim userIdValue As String = GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0)
            Dim userId As Integer
            If Not Integer.TryParse(userIdValue, userId) Then
                Return
            End If

            Dim missingReceipts = _financeReviewService.GetMissingPhysicalReceiptsForUser(userId)
            If missingReceipts Is Nothing OrElse missingReceipts.Count = 0 Then
                Return
            End If

            MessageBox.Show(
                "You still need to submit physical receipts to Finance for " &
                missingReceipts.Count.ToString() &
                " approved ERF(s).",
                "Physical Receipts Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)
        Catch ex As Exception
            Debug.WriteLine("Unable to load physical receipt reminder: " & ex.Message)
        End Try
    End Sub
End Class
