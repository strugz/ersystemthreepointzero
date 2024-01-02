Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Web.WebSockets
Imports Microsoft.Win32
Module modMaintenance
    Dim dtUpdateUserAccount As New DataTable
    Dim strError As String
    Public strPassword As String
    Dim dtLoadPassword As New DataTable
    Dim dtDeptPassword As New DataTable
    Dim sqlAddDeptPassword As New SqlCommand
    Dim sqlcmdLoadPassword As New SqlCommand
    Public Const MyKey As String = "crimsonmonastery2003"
    Public TripleDes As New clsEncryption(MyKey)
    Public Sub AddReport(ByVal dateFrom As String, ByVal dateto As String,
                         ByVal Description As String, ByVal CashAdvance As String,
                         ByVal cashDate As String, ByVal cashrefdoc As String,
                         ByVal cashrefNumber As String, ByVal balto As String,
                         ByVal revolvingfund As String, ByVal cashCheck As String,
                         ByVal userID As String, ByVal status As String,
                         ByVal Approved As String, ByVal dateFiled As String,
                         ByVal fileStatus As String)
        DBConnection()
        Using sqlAddReport As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlAddReport
                    .Connection = SQLConnection
                    .CommandText = "EXEC sp2_AddReportData '" & dateFrom & "','" & dateto &
                        "','" & Replace(LTrim(RTrim(Description)), vbLf, "") & "','" & CashAdvance & "','" & cashDate & "','" & cashrefdoc &
                        "','" & cashrefNumber & "','" & balto & "','" & revolvingfund & "','" & cashCheck &
                        "','" & userID & "','" & status & "','" & Approved & "','" & dateFiled & "','" & fileStatus & "'"
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Sub UpdateReport(ByVal reportID As String, ByVal dateFrom As String, ByVal dateto As String,
                         ByVal Description As String, ByVal CashAdvance As String,
                         ByVal cashDate As String, ByVal cashrefdoc As String,
                         ByVal cashrefNumber As String, ByVal revolvingfund As String,
                         ByVal cashCheck As String)
        DBConnection()
        Using sqlUpdateReport As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlUpdateReport
                    .Connection = SQLConnection
                    .CommandText = "EXEC sp2_UpdateReportData '" & reportID & "','" & dateFrom & "','" & dateto &
                        "','" & Replace(LTrim(RTrim(Description)), vbLf, "") & "','" & CashAdvance & "','" & cashDate & "','" & cashrefdoc &
                        "','" & cashrefNumber & "','" & revolvingfund & "','" & cashCheck & "'"
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Sub RefileER(ByVal reportID As String, ByVal status As String)
        DBConnection()
        Using sqlcmdRefileER As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlcmdRefileER
                    .Connection = SQLConnection
                    .CommandText = "sp2_RefileER '" & reportID & "','" & status & "'"
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Private Sub ExtractUserExpenseMeal(ByVal UserExpenseMeal As String)
        UserExpenseMeal.Split("/")

    End Sub

    Public Sub AddExpense(ByVal transdate As String, ByVal perdiem As String,
                          ByVal particulars As String, ByVal invoice As String,
                          ByVal multiplier As String, ByVal type As String,
                          ByVal category As String, ByVal amount As String,
                          ByVal remarks As String, ByVal status As String,
                          ByVal totalamount As String, ByVal location As String,
                          ByVal userid As String, ByVal reportID As String, ByVal ServiceNumber As String,
                          ByVal Instrument As String, ByVal SerialNumber As String, ByVal WorkWith As String,
                          ByVal UserExpenseMeal As String, ByVal UserExpenseTransportation As String,
                          Optional ByVal mdays As String = "", Optional ByVal computation As String = "", Optional ByVal totdays As String = "")
        DBConnection()
        Using sqlAddExpense As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlAddExpense
                    .Connection = SQLConnection
                    If WorkWith = Nothing Then
                        .CommandText = "EXEC [sp2_AddExpense] '" & transdate & "','" & perdiem &
                            "','" & particulars & "','" & invoice & "','" & multiplier & "','" & type &
                            "','" & category & "','" & amount & "','" & remarks & "','" & status &
                            "','" & totalamount & "','" & Replace(location, "'", "''") & "','" & userid & "','" & reportID & "','" & "NONE" & "','" & ServiceNumber &
                            "','" & Instrument & "','" & SerialNumber &
                            "','" & mdays & "','" & computation & "','" & totdays &
                            "', '" & UserExpenseMealValidation(UserExpenseMeal) &
                            "', '" & UserExpenseTransValidation(UserExpenseTransportation) & "'"
                    Else
                        .CommandText = "EXEC [sp2_AddExpense] '" & transdate & "','" & perdiem &
                           "','" & particulars & "','" & invoice & "','" & multiplier & "','" & type &
                           "','" & category & "','" & amount & "','" & remarks & "','" & status &
                           "','" & totalamount & "','" & Replace(location, "'", "''") & "','" & userid & "','" & reportID & "','" & WorkWith & "','" & ServiceNumber &
                           "','" & Instrument & "','" & SerialNumber &
                           "','" & mdays & "','" & computation & "','" & totdays &
                           "', '" & UserExpenseMealValidation(UserExpenseMeal) &
                           "', '" & UserExpenseTransValidation(UserExpenseTransportation) & "'"
                    End If
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Sub AddExpenseHisto(ByVal transdate As String, ByVal perdiem As String,
                      ByVal particulars As String, ByVal invoice As String,
                      ByVal multiplier As String, ByVal type As String,
                      ByVal category As String, ByVal amount As String,
                      ByVal remarks As String, ByVal status As String,
                      ByVal totalamount As String, ByVal location As String,
                      ByVal userid As String, ByVal reportID As String, ByVal TransID As String,
                      ByVal ServiceNumber As String, ByVal Instrument As String,
                      ByVal SerialNumber As String, ByVal EditedBy As String,
                      Optional ByVal mdays As String = "", Optional ByVal computation As String = "", Optional ByVal totdays As String = "")
        DBConnection()
        Using sqlAddExpense As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlAddExpense
                    .Connection = SQLConnection
                    If WorkWith = Nothing Then
                        .CommandText = "EXEC " & ExpenseLogs() & "'" & transdate & "','" & perdiem &
                            "','" & particulars & "','" & invoice & "','" & multiplier & "','" & type &
                            "','" & category & "','" & amount & "','" & remarks & "','" & status &
                            "','" & totalamount & "','" & location & "','" & userid & "','" & reportID & "','" &
                            "NONE" & "','" & TransID & "','" & ServiceNumber & "','" & Instrument & "','" & SerialNumber &
                            "','" & mdays & "','" & computation & "','" & totdays &
                            "'" & EditedByLogs(EditedBy)
                    Else
                        .CommandText = "EXEC " & ExpenseLogs() & "'" & transdate & "','" & perdiem &
                           "','" & particulars & "','" & invoice & "','" & multiplier & "','" & type &
                           "','" & category & "','" & amount & "','" & remarks & "','" & status &
                           "','" & totalamount & "','" & location & "','" & userid & "','" & reportID & "','" & WorkWith &
                           "','" & TransID & "','" & ServiceNumber & "','" & Instrument & "','" & SerialNumber &
                           "','" & mdays & "','" & computation & "','" & totdays &
                           "'" & EditedByLogs(EditedBy)
                    End If
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Function ExpenseLogs() As String
        Dim str As String
        If GetRegistryValue("Software\\ER System\\Settings", {"Approver"})(0) = "1" Then
            str = "sp2_AddExpenseLogs "
        Else
            str = "sp2_AddExpenseHisto "
        End If
        Return str
    End Function
    Private Function EditedByLogs(ByVal EditedID As String) As String
        Dim str As String
        If GetRegistryValue("Software\\ER System\\Settings", {"Approver"})(0) = "1" Then
            str = "," & "'" & EditedID & "'"
        Else
            str = ""
        End If
        Return str
    End Function
    Public Sub UpdateExpense(ByVal transID As String, ByVal transdate As String,
                           ByVal perdiem As String, ByVal particulars As String,
                           ByVal invoice As String, ByVal multiplier As String,
                           ByVal type As String, ByVal category As String,
                           ByVal amount As String, ByVal remarks As String,
                           ByVal status As String, ByVal totalamount As String,
                           ByVal location As String, ByVal userid As String, ByVal ServiceNumber As String,
                           ByVal Instrument As String, ByVal SerialNumber As String, ByVal WorkWith As String,
                           ByVal UserExpenseMeal As String, ByVal UserExpenseTransportation As String,
                           Optional ByVal mdays As String = "", Optional ByVal computation As String = "", Optional ByVal totdays As String = "")
        DBConnection()
        Using sqlUpdateExpense As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlUpdateExpense
                    .Connection = SQLConnection
                    If WorkWith = Nothing Then
                        .CommandText = "EXEC [sp2_updateExpense] '" & transID & "','" & transdate &
                "','" & perdiem & "','" & particulars & "','" & invoice & "','" & multiplier &
                "','" & type & "','" & category & "','" & amount & "','" & remarks & "','" & status &
                "','" & totalamount & "','" & Replace(location, "'", "''") & "','" & userid & "','" & "NONE" & "','" & ServiceNumber &
                "','" & Instrument & "','" & SerialNumber &
                "','" & mdays & "','" & computation & "','" & totdays &
                "', '" & UserExpenseMealValidation(UserExpenseMeal) &
                "', '" & UserExpenseTransValidation(UserExpenseTransportation) & "'"
                    Else
                        .CommandText = "EXEC [sp2_updateExpense] '" & transID & "','" & transdate &
                    "','" & perdiem & "','" & particulars & "','" & invoice & "','" & multiplier &
                    "','" & type & "','" & category & "','" & amount & "','" & remarks & "','" & status &
                    "','" & totalamount & "','" & Replace(location, "'", "''") & "','" & userid & "','" & WorkWith & "','" & ServiceNumber &
                    "','" & Instrument & "','" & SerialNumber &
                    "','" & mdays & "','" & computation & "','" & totdays &
                    "', '" & UserExpenseMealValidation(UserExpenseMeal) &
                    "', '" & UserExpenseTransValidation(UserExpenseTransportation) & "'"
                    End If
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub

    Private Function UserExpenseMealValidation(ByVal UserExpenseMeal As String) As String
        Dim str As String
        If UserExpenseMeal = "" Then
            str = "" & "', '" & "" & "', '" & ""
            Return str
        Else
            str = UserExpenseMeal.Split("/")(0) & "', '" & UserExpenseMeal.Split("/")(1) & "', '" & UserExpenseMeal.Split("/")(2)
            Return str
        End If
    End Function
    Public Function UserExpenseTransValidation(ByVal UserExpenseTransportation As String) As String
        Dim str As String
        If UserExpenseTransportation = "" Then
            str = "" & "', '" & "" & "', '" & ""
            Return str
        Else
            str = UserExpenseTransportation.Split("/")(0) & "', '" & UserExpenseTransportation.Split("/")(1) & "', '" & UserExpenseTransportation.Split("/")(2)
            Return str

        End If
    End Function
    Public Sub AdduserAccount(ByVal UserID As String, ByVal Fullname As String,
                              ByVal Position As String, ByVal Department As String,
                              ByVal username As String, ByVal Password As String,
                              ByVal emailAdd As String, ByVal EmailPassword As String,
                              ByVal EmailTo As String, ByVal EmailBcc As String,
                              ByVal userlevel As String, ByVal Approver1 As String,
                              ByVal Approver2 As String, ByVal TransportationRate As String,
                              ByVal BreakFastRate As String, ByVal LunchRate As String, ByVal DinnerRate As String,
                              ByVal OTMeal As String)
        DBConnection()
        If Trim(frmUserRegistration.picName) = "" Then
            Dim ms As New IO.MemoryStream()
            '    frmUserRegistration.picSignature.Image.Save(ms, frmUserRegistration.picSignature.Image.RawFormat)
            Dim arrImage() As Byte = ms.GetBuffer
            Try
                Using sqlAddUserAccount As New SqlCommand
                    Using SQLConnection As SqlConnection = mConn.SQLConnection
                        With sqlAddUserAccount
                            .Connection = SQLConnection
                            .CommandType = CommandType.Text
                            .CommandText = "EXEC sp2_AddUserAccount '" & UserID & "','" & Fullname & "','" & Position &
                                "','" & Department & "','" & username & "','" & Password & "','" & emailAdd & "','" & EmailPassword &
                                "','" & EmailTo & "','" & EmailBcc & "',@Signature,'" & userlevel & "', '" & Approver1 & "', '" & Approver2 &
                                "', '" & TransportationRate & "', '" & BreakFastRate & "', '" & LunchRate & "', '" & DinnerRate & "', '" & OTMeal & "'"
                            .Parameters.Add(New SqlParameter("@Signature", SqlDbType.VarBinary)).Value = arrImage
                            .ExecuteNonQuery()
                        End With
                    End Using
                End Using
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        Else
            Dim ms As New IO.MemoryStream()
            frmUserRegistration.picSignature.Image.Save(ms, frmUserRegistration.picSignature.Image.RawFormat)
            Dim arrImage() As Byte = ms.GetBuffer
            Try
                Using sqlAddUserAccount As New SqlCommand
                    Using SQLConnection As SqlConnection = mConn.SQLConnection
                        With sqlAddUserAccount
                            .Connection = SQLConnection
                            .CommandType = CommandType.Text
                            .CommandText = "EXEC sp2_AddUserAccount '" & UserID & "','" & Fullname & "','" & Position &
                            "','" & Department & "','" & username & "','" & Password & "','" & emailAdd & "','" & EmailPassword &
                            "','" & EmailTo & "','" & EmailBcc & "',@Signature,'" & userlevel & "', '" & Approver1 & "', '" & Approver2 &
                            "', '" & TransportationRate & "', '" & BreakFastRate & "', '" & LunchRate & "', '" & DinnerRate & "', '" & OTMeal & "'"
                            .Parameters.Add(New SqlParameter("@Signature", SqlDbType.VarBinary)).Value = arrImage
                            .ExecuteNonQuery()
                        End With
                    End Using
                End Using
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
    End Sub
    Public Sub UpdateUserAccount(ByVal UserID As String, ByVal Fullname As String,
                              ByVal Position As String, ByVal Department As String,
                              ByVal username As String, ByVal Password As String,
                              ByVal EmailTo As String, ByVal EmailBcc As String,
                              ByVal userlevel As String, ByVal Approver1 As String,
                              ByVal Approver2 As String, ByVal TransportationRate As String,
                              ByVal BreakFastRate As String, ByVal LunchRate As String, ByVal DinnerRate As String,
                              ByVal OTMeal As String)
        DBConnection()
        If Trim(frmUserRegistration.picName) = "" Then

            Dim ms As New IO.MemoryStream()
            '    frmUserRegistration.picSignature.Image.Save(ms, frmUserRegistration.picSignature.Image.RawFormat)
            Dim arrImage() As Byte = ms.GetBuffer
            Try
                Using sqlUpdateUserAccount As New SqlCommand
                    Using SQLConnection As SqlConnection = mConn.SQLConnection
                        With sqlUpdateUserAccount
                            .Connection = SQLConnection
                            .CommandType = CommandType.Text
                            .Parameters.Clear()
                            .CommandText = "sp2_UpdateUserAcc '" & UserID & "','" & Fullname & "','" & Position &
                                "','" & Department & "','" & username & "','" & Password &
                                "','" & EmailTo & "','" & EmailBcc & "',@Signature,'" & userlevel & "', '" & Approver1 & "', '" & Approver2 &
                            "', '" & TransportationRate & "', '" & BreakFastRate & "', '" & LunchRate & "', '" & DinnerRate & "', '" & OTMeal & "'"
                            .Parameters.Add(New SqlParameter("@Signature", SqlDbType.VarBinary)).Value = arrImage
                            .ExecuteNonQuery()
                        End With
                    End Using
                End Using

            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        Else
            Dim ms As New IO.MemoryStream()
            frmUserRegistration.picSignature.Image.Save(ms, frmUserRegistration.picSignature.Image.RawFormat)
            Dim arrImage() As Byte = ms.GetBuffer
            Try
                Using sqlUpdateUserAccount As New SqlCommand
                    Using SQLConnection As SqlConnection = mConn.SQLConnection
                        With sqlUpdateUserAccount
                            .Connection = SQLConnection
                            .CommandType = CommandType.Text
                            .Parameters.Clear()
                            .CommandText = "sp2_UpdateUserAcc '" & UserID & "','" & Fullname & "','" & Position &
                                "','" & Department & "','" & username & "','" & Password &
                                "','" & EmailTo & "','" & EmailBcc & "',@Signature,'" & userlevel & "', '" & Approver1 & "', '" & Approver2 &
                            "', '" & TransportationRate & "', '" & BreakFastRate & "', '" & LunchRate & "', '" & DinnerRate & "', '" & OTMeal & "'"
                            .Parameters.Add(New SqlParameter("@Signature", SqlDbType.VarBinary)).Value = arrImage
                            .ExecuteNonQuery()
                        End With
                    End Using
                End Using
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
    End Sub
    Public Sub AddDeptSign(ByVal deptID As String, ByVal review As String, ByVal endorse As String,
                           ByVal approve As String, ByVal UserID As String)
        Try
            DBConnection()
            Using sqlAddDeptSign As New SqlCommand
                Using SqlConnection As SqlConnection = mConn.SQLConnection
                    With sqlAddDeptSign
                        .Connection = SqlConnection
                        .CommandText = "sp2_AddDeptSign '" & deptID & "','" & review & "','" & endorse & "','" & approve &
                            "','" & UserID & "'"
                        .CommandType = CommandType.Text
                        .ExecuteNonQuery()
                    End With
                End Using
            End Using
        Catch ex As Exception
            strError = ex.Message
        End Try
    End Sub
    Public Sub UpdateDeptSign(ByVal UserID As String, ByVal deptID As String, ByVal review As String, ByVal endorse As String,
                          ByVal approve As String)
        Try
            DBConnection()
            Using sqlAddDeptSign As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlAddDeptSign
                        .Connection = SQLConnection
                        .CommandText = "[sp2_UpdateDeptSign] '" & UserID & "','" & deptID & "','" & review & "','" & endorse & "','" & approve & "'"
                        .CommandType = CommandType.Text
                        .ExecuteNonQuery()
                    End With
                End Using
            End Using
        Catch ex As Exception
            strError = ex.Message
        End Try
    End Sub
    Public Sub AddSign(ByVal userID As String, ByVal signID As String, ByVal reportID As String)
        Dim sqlcmdAddSign As New SqlCommand
        Try
            With sqlcmdAddSign
                .Connection = SQLConnection
                .CommandText = "sp2_AddSignature '" & userID & "','" & signID & "','" & reportID & "'"
                .CommandType = CommandType.Text
                .ExecuteNonQuery()
            End With
        Catch ex As Exception
            strError = ex.Message
        End Try
    End Sub
    Public Sub DeleteImage(ByVal userID As String, ByVal reportID As String)
        Try
            DBConnection()
            Using sqlDelete As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlDelete
                        .Connection = SQLConnection
                        .CommandText = "sp2_DeleteVar"
                        .CommandType = CommandType.StoredProcedure
                        .Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                        .Parameters.AddWithValue("@userID", userID).SqlDbType = SqlDbType.VarChar
                        .Parameters.AddWithValue("@Image", DBNull.Value).SqlDbType = SqlDbType.VarBinary
                        .ExecuteNonQuery()
                    End With
                End Using
            End Using
        Catch ex As Exception
            strError = ex.Message
        End Try
    End Sub
    Public Sub ChangePassword(ByVal userid As String, ByVal password As String)
        DBConnection()
        Try
            Using sqlcmdLoadPassword As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadPassword
                        .Connection = SQLConnection
                        .CommandText = "sp2_ChangePassword"
                        .CommandType = CommandType.StoredProcedure
                        .Parameters.Add("@userID", SqlDbType.VarChar).Value = userid
                        .Parameters.AddWithValue("@password", password).SqlDbType = SqlDbType.VarChar
                        .ExecuteNonQuery()
                    End With
                End Using
            End Using
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub
    Public Sub DeptAddPassword(ByVal deptID As String, ByVal password As String)
        DBConnection()
        Using sqlAddDeptPassword As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlAddDeptPassword
                    .Connection = SQLConnection
                    .CommandText = "sp2_InsertAdminLogin"
                    .CommandType = CommandType.StoredProcedure
                    .Parameters.AddWithValue("DeptID", deptID).SqlDbType = SqlDbType.VarChar
                    .Parameters.AddWithValue("@password", password).SqlDbType = SqlDbType.VarChar
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Sub UpdateEmailSetup(ByVal empId As String, ByVal emailAdd As String, ByVal emailPassword As String, ByVal emailTo As String,
                                ByVal emailBcc As String)
        DBConnection()
        Using sqlcmdUpdateEmailSetup As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlcmdUpdateEmailSetup
                    .Connection = SQLConnection
                    .CommandText = "sp2_UpdateEmailSetup"
                    .CommandType = CommandType.StoredProcedure
                    .Parameters.Add("@empID", SqlDbType.VarChar).Value = empId
                    .Parameters.AddWithValue("@emailAdd", emailAdd).SqlDbType = SqlDbType.VarChar
                    .Parameters.AddWithValue("@emailPassword", emailPassword).SqlDbType = SqlDbType.VarChar
                    .Parameters.AddWithValue("@emailTo", emailTo).SqlDbType = SqlDbType.VarChar
                    .Parameters.AddWithValue("@emailBcc", emailBcc).SqlDbType = SqlDbType.VarChar
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Sub AddClient(ByVal ClientName As String)
        Dim sqlcmdAddClient As New SqlCommand
        With sqlcmdAddClient
            .Connection = SQLConnection
            .CommandText = "Insert into tblClient (ClientName) values ('" & ClientName & "')"
            .CommandType = CommandType.Text
            .ExecuteNonQuery()
        End With
    End Sub
    Public Sub InsertAttachment(ByVal ReportAttachment As String)
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(Application.StartupPath + "\settings.txt")
        Dim sqlcmdReportAttachment As New SqlCommand
        With sqlcmdReportAttachment
            .Connection = SQLConnection
            .CommandText = "sp2_InsertAttachment"
            .Parameters.Add("@ReportID", SqlDbType.VarChar).Value = myERData(13)
            .Parameters.AddWithValue("@ReportAttachment", ReportAttachment).SqlDbType = SqlDbType.VarChar
            .CommandType = CommandType.StoredProcedure
            .ExecuteNonQuery()
        End With
    End Sub
    Public Sub PrintSendingReport(ByVal myERDataReportID As String)
        DBConnection()
        Using sqlcmdSendReport As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlcmdSendReport
                    .Connection = SQLConnection
                    .CommandText = "sp_InsertSendingStatus"
                    .Parameters.Add("@ExportID", SqlDbType.VarChar).Value = myERDataReportID
                    .CommandType = CommandType.StoredProcedure
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Sub InsertFare(ByVal FareName As String)
        DBConnection()
        Using sqlcmdInsertFare As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlcmdInsertFare
                    .Connection = SQLConnection
                    .CommandText = "[sp2_InsertFare]"
                    .Parameters.Add("@FareName", SqlDbType.VarChar).Value = FareName
                    .CommandType = CommandType.StoredProcedure
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub

    Public Sub RejectFiledER(ByVal reportID As String, ByVal rejectNote As String)
        DBConnection()
        Using sqlcmdInsertFare As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlcmdInsertFare
                    .Connection = SQLConnection
                    .CommandText = "[sp2_LoadUserReportDetailsCancel]"
                    .Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                    .Parameters.AddWithValue("@reportCancelNote", rejectNote).SqlDbType = SqlDbType.VarChar
                    .CommandType = CommandType.StoredProcedure
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub

    Friend Sub UpdateFileStatus(ByVal UserIDToApprover As String, ByVal ReportIDToAPprove As String, ByVal LoginUserID As String)
        DBConnection()
        Using sqlcmdUpdateFileStatus As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlcmdUpdateFileStatus
                    .Connection = SQLConnection
                    .CommandText = "sp2_UpdateReportNumberStatus '" & UserIDToApprover & "','" & ReportIDToAPprove & "','" & LoginUserID & "'"
                    .CommandType = CommandType.Text
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
End Module
