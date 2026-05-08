Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports Microsoft.Win32
Module modMaintenance
    Private ReadOnly StartupPath As String = System.Windows.Forms.Application.StartupPath
    Dim strError As String
    Public strPassword As String

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
                    .CommandText = "EXEC sp2_AddReportData @dateFrom,@dateTo,@description,@cashAdvance,@cashDate,@cashrefdoc,@cashrefNumber,@balto,@revolvingfund,@cashCheck,@userID,@status,@approved,@dateFiled,@fileStatus"
                    .CommandType = CommandType.Text
                    .Parameters.Add("@dateFrom", SqlDbType.VarChar).Value = dateFrom
                    .Parameters.Add("@dateTo", SqlDbType.VarChar).Value = dateto
                    .Parameters.Add("@description", SqlDbType.VarChar).Value = Replace(LTrim(RTrim(Description)), vbLf, "")
                    .Parameters.Add("@cashAdvance", SqlDbType.VarChar).Value = CashAdvance
                    .Parameters.Add("@cashDate", SqlDbType.VarChar).Value = cashDate
                    .Parameters.Add("@cashrefdoc", SqlDbType.VarChar).Value = cashrefdoc
                    .Parameters.Add("@cashrefNumber", SqlDbType.VarChar).Value = cashrefNumber
                    .Parameters.Add("@balto", SqlDbType.VarChar).Value = balto
                    .Parameters.Add("@revolvingfund", SqlDbType.VarChar).Value = revolvingfund
                    .Parameters.Add("@cashCheck", SqlDbType.VarChar).Value = cashCheck
                    .Parameters.Add("@userID", SqlDbType.VarChar).Value = userID
                    .Parameters.Add("@status", SqlDbType.VarChar).Value = status
                    .Parameters.Add("@approved", SqlDbType.VarChar).Value = Approved
                    .Parameters.Add("@dateFiled", SqlDbType.VarChar).Value = dateFiled
                    .Parameters.Add("@fileStatus", SqlDbType.VarChar).Value = fileStatus
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
                    .CommandText = "EXEC sp2_UpdateReportData @reportID,@dateFrom,@dateTo,@description,@cashAdvance,@cashDate,@cashrefdoc,@cashrefNumber,@revolvingfund,@cashCheck"
                    .CommandType = CommandType.Text
                    .Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                    .Parameters.Add("@dateFrom", SqlDbType.VarChar).Value = dateFrom
                    .Parameters.Add("@dateTo", SqlDbType.VarChar).Value = dateto
                    .Parameters.Add("@description", SqlDbType.VarChar).Value = Replace(LTrim(RTrim(Description)), vbLf, "")
                    .Parameters.Add("@cashAdvance", SqlDbType.VarChar).Value = CashAdvance
                    .Parameters.Add("@cashDate", SqlDbType.VarChar).Value = cashDate
                    .Parameters.Add("@cashrefdoc", SqlDbType.VarChar).Value = cashrefdoc
                    .Parameters.Add("@cashrefNumber", SqlDbType.VarChar).Value = cashrefNumber
                    .Parameters.Add("@revolvingfund", SqlDbType.VarChar).Value = revolvingfund
                    .Parameters.Add("@cashCheck", SqlDbType.VarChar).Value = cashCheck
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
                    .CommandText = "EXEC sp2_RefileER @reportID,@status"
                    .CommandType = CommandType.Text
                    .Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                    .Parameters.Add("@status", SqlDbType.VarChar).Value = status
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
        Dim workWithValue As String = If(String.IsNullOrEmpty(WorkWith), "NONE", WorkWith)
        Dim mealValues() As String = GetExpenseParts(UserExpenseMeal)
        Dim transValues() As String = GetExpenseParts(UserExpenseTransportation)

        Using sqlAddExpense As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlAddExpense
                    .Connection = SQLConnection
                    .CommandText = "EXEC [sp2_AddExpense] @transdate,@perdiem,@particulars,@invoice,@multiplier,@type,@category,@amount,@remarks,@status,@totalamount,@location,@userid,@reportID,@workWith,@serviceNumber,@instrument,@serialNumber,@mdays,@computation,@totdays,@meal1,@meal2,@meal3,@trans1,@trans2,@trans3"
                    .CommandType = CommandType.Text
                    .Parameters.Add("@transdate", SqlDbType.VarChar).Value = transdate
                    .Parameters.Add("@perdiem", SqlDbType.VarChar).Value = perdiem
                    .Parameters.Add("@particulars", SqlDbType.VarChar).Value = particulars
                    .Parameters.Add("@invoice", SqlDbType.VarChar).Value = invoice
                    .Parameters.Add("@multiplier", SqlDbType.VarChar).Value = multiplier
                    .Parameters.Add("@type", SqlDbType.VarChar).Value = type
                    .Parameters.Add("@category", SqlDbType.VarChar).Value = category
                    .Parameters.Add("@amount", SqlDbType.VarChar).Value = amount
                    .Parameters.Add("@remarks", SqlDbType.VarChar).Value = remarks
                    .Parameters.Add("@status", SqlDbType.VarChar).Value = status
                    .Parameters.Add("@totalamount", SqlDbType.VarChar).Value = totalamount
                    .Parameters.Add("@location", SqlDbType.VarChar).Value = location
                    .Parameters.Add("@userid", SqlDbType.VarChar).Value = userid
                    .Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                    .Parameters.Add("@workWith", SqlDbType.VarChar).Value = workWithValue
                    .Parameters.Add("@serviceNumber", SqlDbType.VarChar).Value = ServiceNumber
                    .Parameters.Add("@instrument", SqlDbType.VarChar).Value = Instrument
                    .Parameters.Add("@serialNumber", SqlDbType.VarChar).Value = SerialNumber
                    .Parameters.Add("@mdays", SqlDbType.VarChar).Value = mdays
                    .Parameters.Add("@computation", SqlDbType.VarChar).Value = computation
                    .Parameters.Add("@totdays", SqlDbType.VarChar).Value = totdays
                    .Parameters.Add("@meal1", SqlDbType.VarChar).Value = mealValues(0)
                    .Parameters.Add("@meal2", SqlDbType.VarChar).Value = mealValues(1)
                    .Parameters.Add("@meal3", SqlDbType.VarChar).Value = mealValues(2)
                    .Parameters.Add("@trans1", SqlDbType.VarChar).Value = transValues(0)
                    .Parameters.Add("@trans2", SqlDbType.VarChar).Value = transValues(1)
                    .Parameters.Add("@trans3", SqlDbType.VarChar).Value = transValues(2)
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
        Dim workWithValue As String = If(String.IsNullOrEmpty(WorkWith), "NONE", WorkWith)
        Dim isApprover As Boolean = GetRegistryValue(RegistryKeys.SettingsPath, {RegistryKeys.Approver})(0) = "1"
        Dim procedureName As String = ExpenseLogs().Trim()

        Using sqlAddExpense As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlAddExpense
                    .Connection = SQLConnection
                    .CommandText = "EXEC " & procedureName & " @transdate,@perdiem,@particulars,@invoice,@multiplier,@type,@category,@amount,@remarks,@status,@totalamount,@location,@userid,@reportID,@workWith,@transID,@serviceNumber,@instrument,@serialNumber,@mdays,@computation,@totdays"
                    If isApprover Then
                        .CommandText &= ",@editedBy"
                    End If
                    .CommandType = CommandType.Text
                    .Parameters.Add("@transdate", SqlDbType.VarChar).Value = transdate
                    .Parameters.Add("@perdiem", SqlDbType.VarChar).Value = perdiem
                    .Parameters.Add("@particulars", SqlDbType.VarChar).Value = particulars
                    .Parameters.Add("@invoice", SqlDbType.VarChar).Value = invoice
                    .Parameters.Add("@multiplier", SqlDbType.VarChar).Value = multiplier
                    .Parameters.Add("@type", SqlDbType.VarChar).Value = type
                    .Parameters.Add("@category", SqlDbType.VarChar).Value = category
                    .Parameters.Add("@amount", SqlDbType.VarChar).Value = amount
                    .Parameters.Add("@remarks", SqlDbType.VarChar).Value = remarks
                    .Parameters.Add("@status", SqlDbType.VarChar).Value = status
                    .Parameters.Add("@totalamount", SqlDbType.VarChar).Value = totalamount
                    .Parameters.Add("@location", SqlDbType.VarChar).Value = location
                    .Parameters.Add("@userid", SqlDbType.VarChar).Value = userid
                    .Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                    .Parameters.Add("@workWith", SqlDbType.VarChar).Value = workWithValue
                    .Parameters.Add("@transID", SqlDbType.VarChar).Value = TransID
                    .Parameters.Add("@serviceNumber", SqlDbType.VarChar).Value = ServiceNumber
                    .Parameters.Add("@instrument", SqlDbType.VarChar).Value = Instrument
                    .Parameters.Add("@serialNumber", SqlDbType.VarChar).Value = SerialNumber
                    .Parameters.Add("@mdays", SqlDbType.VarChar).Value = mdays
                    .Parameters.Add("@computation", SqlDbType.VarChar).Value = computation
                    .Parameters.Add("@totdays", SqlDbType.VarChar).Value = totdays
                    If isApprover Then
                        .Parameters.Add("@editedBy", SqlDbType.VarChar).Value = EditedBy
                    End If
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Function ExpenseLogs() As String
        Dim str As String
        If GetRegistryValue(RegistryKeys.SettingsPath, {RegistryKeys.Approver})(0) = "1" Then
            str = "sp2_AddExpenseLogs "
        Else
            str = "sp2_AddExpenseHisto "
        End If
        Return str
    End Function
    Private Function EditedByLogs(ByVal EditedID As String) As String
        Dim str As String
        If GetRegistryValue(RegistryKeys.SettingsPath, {RegistryKeys.Approver})(0) = "1" Then
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
        Dim workWithValue As String = If(String.IsNullOrEmpty(WorkWith), "NONE", WorkWith)
        Dim mealValues() As String = GetExpenseParts(UserExpenseMeal)
        Dim transValues() As String = GetExpenseParts(UserExpenseTransportation)

        Using sqlUpdateExpense As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlUpdateExpense
                    .Connection = SQLConnection
                    .CommandText = "EXEC [sp2_updateExpense] @transID,@transdate,@perdiem,@particulars,@invoice,@multiplier,@type,@category,@amount,@remarks,@status,@totalamount,@location,@userid,@workWith,@serviceNumber,@instrument,@serialNumber,@mdays,@computation,@totdays,@meal1,@meal2,@meal3,@trans1,@trans2,@trans3"
                    .CommandType = CommandType.Text
                    .Parameters.Add("@transID", SqlDbType.VarChar).Value = transID
                    .Parameters.Add("@transdate", SqlDbType.VarChar).Value = transdate
                    .Parameters.Add("@perdiem", SqlDbType.VarChar).Value = perdiem
                    .Parameters.Add("@particulars", SqlDbType.VarChar).Value = particulars
                    .Parameters.Add("@invoice", SqlDbType.VarChar).Value = invoice
                    .Parameters.Add("@multiplier", SqlDbType.VarChar).Value = multiplier
                    .Parameters.Add("@type", SqlDbType.VarChar).Value = type
                    .Parameters.Add("@category", SqlDbType.VarChar).Value = category
                    .Parameters.Add("@amount", SqlDbType.VarChar).Value = amount
                    .Parameters.Add("@remarks", SqlDbType.VarChar).Value = remarks
                    .Parameters.Add("@status", SqlDbType.VarChar).Value = status
                    .Parameters.Add("@totalamount", SqlDbType.VarChar).Value = totalamount
                    .Parameters.Add("@location", SqlDbType.VarChar).Value = location
                    .Parameters.Add("@userid", SqlDbType.VarChar).Value = userid
                    .Parameters.Add("@workWith", SqlDbType.VarChar).Value = workWithValue
                    .Parameters.Add("@serviceNumber", SqlDbType.VarChar).Value = ServiceNumber
                    .Parameters.Add("@instrument", SqlDbType.VarChar).Value = Instrument
                    .Parameters.Add("@serialNumber", SqlDbType.VarChar).Value = SerialNumber
                    .Parameters.Add("@mdays", SqlDbType.VarChar).Value = mdays
                    .Parameters.Add("@computation", SqlDbType.VarChar).Value = computation
                    .Parameters.Add("@totdays", SqlDbType.VarChar).Value = totdays
                    .Parameters.Add("@meal1", SqlDbType.VarChar).Value = mealValues(0)
                    .Parameters.Add("@meal2", SqlDbType.VarChar).Value = mealValues(1)
                    .Parameters.Add("@meal3", SqlDbType.VarChar).Value = mealValues(2)
                    .Parameters.Add("@trans1", SqlDbType.VarChar).Value = transValues(0)
                    .Parameters.Add("@trans2", SqlDbType.VarChar).Value = transValues(1)
                    .Parameters.Add("@trans3", SqlDbType.VarChar).Value = transValues(2)
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub

    Private Function GetExpenseParts(ByVal rawValue As String) As String()
        Dim values() As String = {"", "", ""}

        If String.IsNullOrEmpty(rawValue) Then
            Return values
        End If

        Dim parts() As String = rawValue.Split("/"c)
        Dim i As Integer

        For i = 0 To Math.Min(parts.Length, 3) - 1
            values(i) = parts(i)
        Next

        Return values
    End Function
    ' Returns the user's signature image bytes from frmUserRegistration.
    ' If no image name is set, returns an empty byte array (placeholder).
    Private Function GetSignatureBytes() As Byte()
        Using ms As New IO.MemoryStream()
            If Trim(frmUserRegistration.picName) <> "" Then
                frmUserRegistration.picSignature.Image.Save(ms, frmUserRegistration.picSignature.Image.RawFormat)
            End If

            Return ms.GetBuffer()
        End Using
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
        Try
            Using sqlAddUserAccount As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlAddUserAccount
                        .Connection = SQLConnection
                        .CommandType = CommandType.Text
                        .CommandText = "EXEC sp2_AddUserAccount @UserID,@Fullname,@Position,@Department,@username,@Password,@emailAdd,@EmailPassword,@EmailTo,@EmailBcc,@Signature,@userlevel,@Approver1,@Approver2,@TransportationRate,@BreakFastRate,@LunchRate,@DinnerRate,@OTMeal"
                        .Parameters.Add("@UserID", SqlDbType.VarChar).Value = UserID
                        .Parameters.Add("@Fullname", SqlDbType.VarChar).Value = Fullname
                        .Parameters.Add("@Position", SqlDbType.VarChar).Value = Position
                        .Parameters.Add("@Department", SqlDbType.VarChar).Value = Department
                        .Parameters.Add("@username", SqlDbType.VarChar).Value = username
                        .Parameters.Add("@Password", SqlDbType.VarChar).Value = Password
                        .Parameters.Add("@emailAdd", SqlDbType.VarChar).Value = emailAdd
                        .Parameters.Add("@EmailPassword", SqlDbType.VarChar).Value = EmailPassword
                        .Parameters.Add("@EmailTo", SqlDbType.VarChar).Value = EmailTo
                        .Parameters.Add("@EmailBcc", SqlDbType.VarChar).Value = EmailBcc
                        .Parameters.Add("@Signature", SqlDbType.VarBinary).Value = GetSignatureBytes()
                        .Parameters.Add("@userlevel", SqlDbType.VarChar).Value = userlevel
                        .Parameters.Add("@Approver1", SqlDbType.VarChar).Value = Approver1
                        .Parameters.Add("@Approver2", SqlDbType.VarChar).Value = Approver2
                        .Parameters.Add("@TransportationRate", SqlDbType.VarChar).Value = TransportationRate
                        .Parameters.Add("@BreakFastRate", SqlDbType.VarChar).Value = BreakFastRate
                        .Parameters.Add("@LunchRate", SqlDbType.VarChar).Value = LunchRate
                        .Parameters.Add("@DinnerRate", SqlDbType.VarChar).Value = DinnerRate
                        .Parameters.Add("@OTMeal", SqlDbType.VarChar).Value = OTMeal
                        .ExecuteNonQuery()
                    End With
                End Using
            End Using
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
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
        Try
            Using sqlUpdateUserAccount As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlUpdateUserAccount
                        .Connection = SQLConnection
                        .CommandType = CommandType.Text
                        .Parameters.Clear()
                        .CommandText = "EXEC sp2_UpdateUserAcc @UserID,@Fullname,@Position,@Department,@username,@Password,@EmailTo,@EmailBcc,@Signature,@userlevel,@Approver1,@Approver2,@TransportationRate,@BreakFastRate,@LunchRate,@DinnerRate,@OTMeal"
                        .Parameters.Add("@UserID", SqlDbType.VarChar).Value = UserID
                        .Parameters.Add("@Fullname", SqlDbType.VarChar).Value = Fullname
                        .Parameters.Add("@Position", SqlDbType.VarChar).Value = Position
                        .Parameters.Add("@Department", SqlDbType.VarChar).Value = Department
                        .Parameters.Add("@username", SqlDbType.VarChar).Value = username
                        .Parameters.Add("@Password", SqlDbType.VarChar).Value = Password
                        .Parameters.Add("@EmailTo", SqlDbType.VarChar).Value = EmailTo
                        .Parameters.Add("@EmailBcc", SqlDbType.VarChar).Value = EmailBcc
                        .Parameters.Add("@Signature", SqlDbType.VarBinary).Value = GetSignatureBytes()
                        .Parameters.Add("@userlevel", SqlDbType.VarChar).Value = userlevel
                        .Parameters.Add("@Approver1", SqlDbType.VarChar).Value = Approver1
                        .Parameters.Add("@Approver2", SqlDbType.VarChar).Value = Approver2
                        .Parameters.Add("@TransportationRate", SqlDbType.VarChar).Value = TransportationRate
                        .Parameters.Add("@BreakFastRate", SqlDbType.VarChar).Value = BreakFastRate
                        .Parameters.Add("@LunchRate", SqlDbType.VarChar).Value = LunchRate
                        .Parameters.Add("@DinnerRate", SqlDbType.VarChar).Value = DinnerRate
                        .Parameters.Add("@OTMeal", SqlDbType.VarChar).Value = OTMeal
                        .ExecuteNonQuery()
                    End With
                End Using
            End Using
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Public Sub AddDeptSign(ByVal deptID As String, ByVal review As String, ByVal endorse As String,
                           ByVal approve As String, ByVal UserID As String)
        Try
            DBConnection()
            Using sqlAddDeptSign As New SqlCommand
                Using SqlConnection As SqlConnection = mConn.SQLConnection
                    With sqlAddDeptSign
                        .Connection = SqlConnection
                        .CommandText = "EXEC sp2_AddDeptSign @deptID,@review,@endorse,@approve,@UserID"
                        .CommandType = CommandType.Text
                        .Parameters.Add("@deptID", SqlDbType.VarChar).Value = deptID
                        .Parameters.Add("@review", SqlDbType.VarChar).Value = review
                        .Parameters.Add("@endorse", SqlDbType.VarChar).Value = endorse
                        .Parameters.Add("@approve", SqlDbType.VarChar).Value = approve
                        .Parameters.Add("@UserID", SqlDbType.VarChar).Value = UserID
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
                        .CommandText = "EXEC [sp2_UpdateDeptSign] @UserID,@deptID,@review,@endorse,@approve"
                        .CommandType = CommandType.Text
                        .Parameters.Add("@UserID", SqlDbType.VarChar).Value = UserID
                        .Parameters.Add("@deptID", SqlDbType.VarChar).Value = deptID
                        .Parameters.Add("@review", SqlDbType.VarChar).Value = review
                        .Parameters.Add("@endorse", SqlDbType.VarChar).Value = endorse
                        .Parameters.Add("@approve", SqlDbType.VarChar).Value = approve
                        .ExecuteNonQuery()
                    End With
                End Using
            End Using
        Catch ex As Exception
            strError = ex.Message
        End Try
    End Sub

    Public Sub AddSign(ByVal userID As String, ByVal signID As String, ByVal reportID As String)
        Try
            DBConnection()
            Using sqlcmdAddSign As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdAddSign
                        .Connection = SQLConnection
                        .CommandText = "EXEC sp2_AddSignature @userID,@signID,@reportID"
                        .CommandType = CommandType.Text
                        .Parameters.Add("@userID", SqlDbType.VarChar).Value = userID
                        .Parameters.Add("@signID", SqlDbType.VarChar).Value = signID
                        .Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                        .ExecuteNonQuery()
                    End With
                End Using
            End Using
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
        DBConnection()
        Using sqlcmdAddClient As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlcmdAddClient
                    .Connection = SQLConnection
                    .CommandText = "sp2_InsertClient"
                    .CommandType = CommandType.StoredProcedure
                    .Parameters.Add("@ClientName", SqlDbType.VarChar).Value = ClientName
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
    Public Sub InsertAttachment(ByVal ReportAttachment As String)
        Dim ClsData As New ClsLoadData
        Dim myERData As String()
        myERData = ClsData.GetEReportDetails(StartupPath + "\settings.txt")
        DBConnection()
        Using sqlcmdReportAttachment As New SqlCommand
            Using SQLConnection As SqlConnection = mConn.SQLConnection
                With sqlcmdReportAttachment
                    .Connection = SQLConnection
                    .CommandText = "sp2_InsertAttachment"
                    .Parameters.Add("@ReportID", SqlDbType.VarChar).Value = myERData(13)
                    .Parameters.AddWithValue("@ReportAttachment", ReportAttachment).SqlDbType = SqlDbType.VarChar
                    .CommandType = CommandType.StoredProcedure
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
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
                    .CommandText = "sp2_UpdateReportNumberStatus"
                    .CommandType = CommandType.StoredProcedure
                    .Parameters.Add("@UserID", SqlDbType.VarChar).Value = UserIDToApprover
                    .Parameters.Add("@ReportID", SqlDbType.VarChar).Value = ReportIDToAPprove
                    .Parameters.Add("@SignID", SqlDbType.VarChar).Value = LoginUserID
                    .ExecuteNonQuery()
                End With
            End Using
        End Using
    End Sub
End Module
