Imports System.ComponentModel
Imports System.Net
Imports System.Net.Mail
Public Class frmERType
    Dim subject As String
    Dim dtp As DateTime = Date.Now
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Private Sub frmERType_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        rbtERF.Checked = True
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnSend.Click
        If rbtERF.Checked = True Then
            PbbLoading.Visible = True
            btnSend.Text = "Sending . ."
            btnSend.Enabled = False
            BWSending.RunWorkerAsync()
        ElseIf RBLocation.Checked = True Then
            If txtLocationCode.Text.Length = 0 Or txtLocationName.Text.Length = 0 Then
                MsgBox("Please fill Location Code and Location Name.")
            Else
                PbbLoading.Visible = True
                btnSend.Text = "Sending . ."
                btnSend.Enabled = False
                BWSending.RunWorkerAsync()
            End If
        End If

    End Sub

    Public Sub RBUTTON()
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        Dim myPDFLocation As String = ClsData.PDFLocation(IIf(rbtERF.Checked = True, True, False), myERData(1), txtLocationCode.Text)
        Dim myUsername As String = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "UserName", ""))
        Dim myPassword As String = TripleDes.DecryptData(My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\ER System\Connection", "Password", ""))
        Dim myBody As String = "This Attachment is original and cannot be edited by any platform." + vbNewLine + "      If they want to change these data they have to create a new report in Expense Management System."
        Try
            If ClsData.PDFExport(
        myUsername,
        myPassword, myERData(13),
        myPDFLocation) = "True" Then
                Using dtLoadUserEmailDetails As DataTable = LoadingUserAccountEmail(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0))
                    If dtLoadUserEmailDetails.Rows.Count <> 0 Then
                        With dtLoadUserEmailDetails
                            ClsData.SendExpenseEmail(
                                TripleDes.DecryptData(.Rows(0).Item("EmailAdd")), TripleDes.DecryptData(.Rows(0).Item("EmailPass")), .Rows(0).Item("EmailTo"),
                                .Rows(0).Item("EmailBCC"), "",
                                ClsData.EmailSubject(IIf(rbtERF.Checked = True, True, False), GetRegistryValue("Software\\ER System\\UserAccount", {"Username"})(0), myERData(1), txtLocationName.Text),
                                myBody, myPDFLocation)
                        End With
                    End If
                End Using
                PrintSendingReport(myERData(13))
            Else
                MsgBox("PDF Export Fail. Try again")
            End If
        Catch ex As Exception
            MsgBox(ex.Message + vbCrLf + "Please Contact IT Administrator.")
        End Try

    End Sub
    'Public Sub SendingEmail()
    '    LoadingUserAccountEmail(GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0))
    '    Dim attachment As Attachment = New Attachment(frmRpt.strExportFile)
    '    Dim message As New MailMessage(TripleDes.DecryptData(modLoadingData.EmailAdd).ToString, modLoadingData.EmailTo, subject,
    '                                   "This Attachment is original and cannot be edited by any platform." + vbNewLine + "      If they want to change these data they have to create a new report in Expense Management System.")
    '    message.Attachments.Add(attachment)
    '    Dim smtpClient As New SmtpClient("mail.marsmandrysdale.com")
    '    message.Bcc.Add(modLoadingData.EmailBCC)
    '    smtpClient.EnableSsl = False
    '    smtpClient.UseDefaultCredentials = False
    '    Dim credentials As New NetworkCredential(TripleDes.DecryptData(modLoadingData.EmailAdd).ToString, TripleDes.DecryptData(modLoadingData.EmailPass).ToString)
    '    smtpClient.Credentials = credentials
    '    smtpClient.Send(message)
    '    InsertAttachment(GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) + "|" + DateAndTime.Now.ToString("MM/dd/yyyy HH:mm") + "|" + frmRpt.strExportFile)
    'End Sub

    Private Sub rbtERF_CheckedChanged(sender As Object, e As EventArgs) Handles rbtERF.CheckedChanged
        If RBLocation.Checked = True Then
            txtLocationCode.Enabled = True
            txtLocationName.Enabled = True
        Else
            txtLocationCode.Enabled = False
            txtLocationName.Enabled = False
            txtLocationCode.Clear()
            txtLocationName.Clear()
        End If
    End Sub

    Private Sub BWSending_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BWSending.DoWork
        Threading.Thread.Sleep(1000)
        RBUTTON()
    End Sub

    Private Sub BWSending_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles BWSending.RunWorkerCompleted
        Me.btnSend.Text = "Send"
        MsgBox("E-mail Sent . . .", TopMost = True)
        Me.Close()
        Me.Enabled = True
        PbbLoading.Visible = False
    End Sub

    Private Sub frmERType_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        PbbLoading.Visible = False
        btnSend.Enabled = True
    End Sub
End Class
