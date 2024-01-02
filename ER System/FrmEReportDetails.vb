Public Class FrmEReportDetails
    Private ReadOnly ClsData As New ClsLoadData
    Private Sub BtnSave_Click(sender As Object, e As EventArgs) Handles BtnSave.Click
        If Trim(LTrim(RTrim(txtPofExpense.Text)) = "") Then
            MsgBox("Please Fill All Fields")
        Else
            If CbCashAdvanceReceive.Checked = False Or txtAmount.Text = Nothing Then
                AddReport(DtpReportFrom.Text, DtpReportTo.Text, txtPofExpense.Text, "0", "",
                          txtRefDoc.Text, txtRefNum.Text,
                          "Employee", txtRevFund.Text, IIf(CbCashAdvanceReceive.Checked, "1", "0"),
                          GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), "", "NOT APPROVED", Date.Now.ToString("yyyy-MM-dd"), "")
                MsgBox("Add Successfully")
                Call RefreshEReportData()
                Me.Close()
            ElseIf GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0) = "Admin" Then
                AddReport(
                    DtpReportFrom.Text, DtpReportTo.Text, txtPofExpense.Text, txtAmount.Text,
                    DtpReportDate.Value.ToString("MM-dd-yyyy"), txtRefDoc.Text, txtRefNum.Text, "Employee",
                    txtRevFund.Text, IIf(CbCashAdvanceReceive.Checked, "1", "0"),
                    GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), "", "APPROVED",
                    Date.Now.ToString("yyyy-MM-dd"), "0")
                MsgBox("Add Successfully")
                Call RefreshEReportData()
                Me.Close()
            ElseIf GetRegistryValue("Software\\ER System\\UserAccount", {"Userlevel"})(0) = "Admin" And (CbCashAdvanceReceive.Checked = False Or txtAmount.Text = Nothing) Then
                AddReport(DtpReportFrom.Text, DtpReportTo.Text, txtPofExpense.Text, txtAmount.Text, "", txtRefDoc.Text,
                    txtRefNum.Text, "Employee", txtRevFund.Text, IIf(CbCashAdvanceReceive.Checked, "1", "0"),
                    GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), "", "APPROVED",
                    Date.Now.ToString("yyyy-MM-dd"), "0")
                MsgBox("Add Successfully")
                Call RefreshEReportData()
                Me.Close()
            Else
                AddReport(DtpReportFrom.Text, DtpReportTo.Text, txtPofExpense.Text, txtAmount.Text,
                    DtpReportDate.Value.ToString("MM-dd-yyyy"), txtRefDoc.Text, txtRefNum.Text, "Employee",
                    txtRevFund.Text, IIf(CbCashAdvanceReceive.Checked, "1", "0"),
                    GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), "", "NOT APPROVED",
                    Date.Now.ToString("yyyy-MM-dd"), "")
                MsgBox("Add Successfully")
                Call RefreshEReportData()
                Me.Close()
            End If
        End If
    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        CbCashAdvanceReceive.Checked = False
        btnUpdate.Visible = False
        BtnSave.Visible = True
        Me.Close()
    End Sub

    Private Sub btnUpdate_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
        If ClsData.TempFileValidation(Application.StartupPath + "\settings.txt") = True Then
            Dim myERData As String()
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
            UpdateReport(myERData(13), DtpReportFrom.Text, DtpReportTo.Text, txtPofExpense.Text, txtAmount.Text,
                DtpReportDate.Text, txtRefDoc.Text, txtRefNum.Text, txtRevFund.Text,
                IIf(CbCashAdvanceReceive.Checked, "1", "0"))
            MsgBox("Update Successfully")
            Call RefreshEReportData()
            Me.Close()
        End If
    End Sub

    Private Sub FrmEReportDetails_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If IO.File.Exists(Application.StartupPath + "\settings.txt") Then
            Dim myERData As String()
            myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
            If myERData(6) = "0" Then
                txtPofExpense.Text = myERData(0)
                DtpReportFrom.Value = myERData(1)
                DtpReportTo.Value = myERData(2)
                CbCashAdvanceReceive.Checked = False
                btnUpdate.Visible = True
                BtnSave.Visible = False
            Else
                txtPofExpense.Text = myERData(0)
                DtpReportFrom.Value = myERData(1)
                DtpReportTo.Value = myERData(2)
                CbCashAdvanceReceive.Checked = True
                DtpReportDate.Value = myERData(7)
                txtRefDoc.Text = myERData(8)
                txtRefNum.Text = myERData(9)
                txtAmount.Text = myERData(10)
                txtRevFund.Text = myERData(11)
                btnUpdate.Visible = True
                BtnSave.Visible = False
            End If
        Else
            txtPofExpense.Clear()
            DtpReportFrom.Value = DateTime.Now
            DtpReportTo.Value = DateTime.Now
            CbCashAdvanceReceive.Checked = False
            DtpReportDate.Value = DateTime.Now
            txtRefDoc.Clear()
            txtRefNum.Clear()
            txtAmount.Clear()
            txtRevFund.Clear()
            btnUpdate.Visible = False
            BtnSave.Visible = True
        End If

    End Sub

    Private Sub FrmEReportDetails_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        ClsData.DeleteEReportDetails(Application.StartupPath + "\settings.txt")
        txtPofExpense.Clear()
        DtpReportFrom.Value = DateTime.Now
        DtpReportTo.Value = DateTime.Now
        CbCashAdvanceReceive.Checked = False
        DtpReportDate.Value = DateTime.Now
        txtRefDoc.Clear()
        txtRefNum.Clear()
        txtAmount.Clear()
        txtRevFund.Clear()
        Call ReleasMemory()
    End Sub

    Private Sub CbCashAdvanceReceive_CheckedChanged(sender As Object, e As EventArgs) Handles CbCashAdvanceReceive.CheckedChanged
        If CbCashAdvanceReceive.Checked = True Then
            GroupBox3.Enabled = True
        Else
            GroupBox3.Enabled = False
            txtAmount.Clear()
            txtRefDoc.Clear()
            txtRefNum.Clear()
            txtRevFund.Clear()
        End If
    End Sub
End Class