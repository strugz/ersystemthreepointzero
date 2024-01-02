Imports JFramework
Imports System.IO
Public Class ClsLoadData : Inherits AppFramework
    Friend Function SetEReportDetails(ByVal ReportID As String) As String
        Dim EReportDetails As DataTable
        Dim str As String = ""
        Dim ifNull As String = ""
        EReportDetails = LoadEReportDetails(ReportID, GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0))
        If EReportDetails.Rows.Count <> 0 Then
            For i = 0 To EReportDetails.Columns.Count - 1
                ifNull = IIf(IsDBNull(EReportDetails.Rows(0).Item(i)) = True, "", EReportDetails.Rows(0).Item(i))
                str = IIf(str = "", "", str & vbCrLf) & IIf(IsDBNull(EReportDetails.Rows(0).Item(i)) = True, "", Replace(ifNull, vbLf, ""))
            Next
            FileMakerWithData(Application.StartupPath + "\settings.txt", str) 'Replace(EReportDetails.Rows(0).Item(i), vbLf, "")
        End If
        Return ""
    End Function
    Friend Function SetExpenseDetails(ByVal ReportID As String, ByVal ExpenseID As String) As String
        Using ExpenseDetails As DataTable = LoadExpenseReportDetails(ReportID, ExpenseID)
            Dim str As String = ""
            Dim strRemarks As String = ""
            If ExpenseDetails.Rows.Count <> 0 Then
                For i = 0 To ExpenseDetails.Columns.Count - 1
                    If ExpenseDetails.Rows(0).Item(i) Like "*" & vbLf & "*" Then
                        For X = 0 To UBound(Split(ExpenseDetails.Rows(0).Item(i), vbLf))
                            strRemarks = IIf(strRemarks = "", "", strRemarks & "<SPLIT>") & Split(ExpenseDetails.Rows(0).Item(i), vbLf)(X)
                        Next
                        str = str & vbCrLf & strRemarks
                    Else
                        str = IIf(str = "", "", str & vbCrLf) & ExpenseDetails.Rows(0).Item(i)
                    End If

                    If ExpenseDetails.Columns.Count - 1 = i And ExpenseDetails.Rows(0).Item(i).ToString() = "" Then
                        str = str & vbCrLf & ""
                    End If
                Next
                FileMakerWithData(Application.StartupPath + "\expenseSettings.txt", str)
            End If
        End Using
        Return ""
    End Function
    Friend Function SetExpenseTransDetails(ByVal ExpenseID As String) As String
        Using ExpenseTransDetails As DataTable = LoadUserExpenseTrans(ExpenseID)
            Dim str As String = ""
            If ExpenseTransDetails.Rows.Count <> 0 Then
                For i = 0 To ExpenseTransDetails.Columns.Count - 1
                    str = IIf(str = "", "", str & vbCrLf) & ExpenseTransDetails.Rows(0).Item(i)
                Next
                FileMakerWithData(Application.StartupPath + "\expenseTransSettings.txt", str)
            End If
            Return ""
        End Using
    End Function
    Friend Function SetExpenseMealDetails(ByVal ExpenseID As String) As String
        Using ExpenseMealDetails As DataTable = LoadUserExpenseMeal(ExpenseID)
            Dim str As String = ""
            If ExpenseMealDetails.Rows.Count <> 0 Then
                For i = 0 To ExpenseMealDetails.Columns.Count - 1
                    str = IIf(str = "", "", str & vbCrLf) & ExpenseMealDetails.Rows(0).Item(i)
                Next
                FileMakerWithData(Application.StartupPath + "\expenseMealSettings.txt", str)
            End If
        End Using
        Return ""
    End Function
    Friend Sub SetExpenseTransDetailsTemp(ByVal DataTemp As String())
        Dim str As String = ""
        For i = 0 To DataTemp.Length - 1
            str = IIf(str = "", "", str & vbCrLf) & DataTemp(i)
        Next
        FileMakerWithData(Application.StartupPath + "\expenseTransSettingsTEMP.txt", str)
    End Sub
    Friend Sub SetExpenseMealDetailsTemp(ByVal DataTemp As String())
        Dim str As String = ""
        For i = 0 To DataTemp.Length - 1
            str = IIf(str = "", "", str & vbCrLf) & DataTemp(i)
        Next
        FileMakerWithData(Application.StartupPath + "\expenseMealSettingsTEMP.txt", str)
    End Sub
    Friend Sub SetOfficerToSign(ByVal DataTemp As String())
        Dim str As String = ""
        For i = 0 To DataTemp.Length - 1
            str = IIf(str = "", "", str & vbCrLf) & DataTemp(i)
        Next
        FileMakerWithData(Application.StartupPath + "\officerToSign.txt", str)
    End Sub
    Friend Sub DeleteEReportDetails(ByVal path As String)
        DeleteFile(path)
    End Sub
    Friend Function GetEReportDetails(ByVal path As String) As String()
        Return GetFileData(path)
    End Function
    Friend Function TempFileValidation(ByVal path As String) As String
        Return CheckFile(path)
    End Function
    Friend Function GetTranspo() As String
        Dim myERData As String() = {""}
        If TempFileValidation(Application.StartupPath + "\expenseTransSettingsTEMP.txt") = True Then
            myERData = GetEReportDetails(Application.StartupPath + "\expenseTransSettingsTEMP.txt")
            Return myERData(4)
        Else
            Return myERData(0)
        End If
    End Function
    Friend Function GetMeal() As String
        Dim myERData As String() = {""}
        If TempFileValidation(Application.StartupPath + "\expenseMealSettingsTEMP.txt") = True Then
            myERData = GetEReportDetails(Application.StartupPath + "\expenseMealSettingsTEMP.txt")
            Return myERData(4)
        Else
            Return myERData(0)
        End If
    End Function
    Friend Function PDFCreator(ByVal username As String, ByVal password As String, ByVal ReportID As String) As ReportDocument
        Return ReportLoad(Application.StartupPath & "\ER Report.rpt", username, password, {"@UserID", "@reportID"}, {GetRegistryValue("Software\\ER System\\UserAccount", {"UserID"})(0), ReportID})
    End Function
    Friend Function PDFExport(ByVal username As String, ByVal password As String, ByVal reportID As String, ByVal path As String) As String
        Return PDF(PDFCreator(username, password, reportID), path)
    End Function
    Friend Function PDFLocation(ByVal rbt As Boolean, ByVal ERdate As DateTime, Optional ByVal LocationCode As String = "") As String
        If rbt = True Then
            Return Application.StartupPath & "\ERPDF\" & GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) & "ER" & ERdate.ToString("ddMMMyyyy").ToUpper & ".pdf".ToString
        Else
            Return Application.StartupPath & "\ERPDF\" & GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) & LocationCode & ERdate.ToString("ddMMMyyyy").ToUpper & ".pdf".ToString
        End If
    End Function
    Friend Function SendExpenseEmail(ByVal EmailSender As String, ByVal Password As String,
                                     ByVal EmailReceiver As String, ByVal EmailBCC As String, ByVal EmailCC As String,
                                     ByVal Subject As String, ByVal Body As String, ByVal Attachment As String) As String
        Try
            'MsgBox("mail.marsmandrysdale.com" + " / " + EmailSender + " / " + Password + " / " + EmailReceiver + " / " + EmailBCC + " / " +
            '    EmailCC + " / " + Subject + " / " + Body + " / " + Attachment)

            EmailSend("mail.marsmandrysdale.com", EmailSender, Password, EmailReceiver, EmailBCC, EmailCC, Subject, Body, Attachment)
            Return "True"
        Catch ex As Exception
            MsgBox(ex.ToString())
            Return "False"
        End Try
    End Function
    Friend Function EmailSubject(ByVal RBT As String, ByVal Username As String, ByVal ERDate As DateTime, ByVal LocationName As String) As String
        If RBT = True Then
            Return Username + " " + "ER FOR THE MONTH OF " + ERDate.ToString("MMM").ToUpper + " " + ERDate.ToString("yyyy").ToUpper
        Else
            Return Username + " " + "Liquidation for  " + LocationName + " Trip " + ERDate.ToString("MMM").ToUpper + " " + ERDate.ToString("yyyy").ToUpper
        End If
    End Function
    Friend Function ApproverValidation(ByVal UserID As String, ByVal SignID As String, ByVal ReportID As String) As String
        Dim returnValue As String = ""
        Using dtLoadUserAuthorityAccept As DataTable = LoadUserAuthorityAccept(ReportID, SignID, UserID)
            Using dtLoadUserAuthority As DataTable = LoadUserAuthority(UserID, SignID)
                If dtLoadUserAuthorityAccept.Rows.Count = 0 Then
                    For i = 0 To dtLoadUserAuthority.Rows.Count - 1
                        If dtLoadUserAuthority.Rows(i).Item("Sort") = i + 1 Then
                            returnValue = "True"
                        Else
                            returnValue = "False"
                        End If
                    Next
                    Return returnValue
                ElseIf dtLoadUserAuthorityAccept.Rows(0).Item("SignID") = SignID Then
                    Return "Done"
                Else
                    For i = 0 To dtLoadUserAuthority.Rows.Count - 1
                        If dtLoadUserAuthority.Rows(i).Item("Sort") = dtLoadUserAuthorityAccept.Rows(0).Item("myCount") + 1 Then
                            returnValue = "True"
                        Else
                            returnValue = "False"
                        End If
                    Next
                    Return returnValue
                End If
            End Using
        End Using
    End Function
    Friend Function MyReportDocument(ByVal ReportPath As String, ByVal Username As String, ByVal Password As String,
                                     ByVal Param As String(), ByVal ParamValue As String()) As ReportDocument
        Return ReportLoad(ReportPath, Username, Password, Param, ParamValue)
    End Function
End Class