Imports System.Data.SqlClient
Module modLoadingData
    Private Const MyKey As String = "crimsonmonastery2003"
    Private TripleDes As New clsEncryption(MyKey)
    Public LoadEndorse, LoadReview, LoadApprove, DeptID As String
    Public EmailAdd As String
    Public EmailPass As String
    Public EmailTo As String
    Public EmailBCC As String
    Public ExpenseCount As String
    Public OfficersToSign As String
    Public ReportID As String
    Public DuplicateUserID As String
    Public LoginuserID As String = Nothing
    Public LoginDeptID As String = Nothing
    Public LoginMealBF As String = Nothing
    Public LoginMealLunch As String = Nothing
    Public LoginMealDinner As String = Nothing
    Public LoginMealOTMeal As String = Nothing
    Public LoginTransportation As String = Nothing
    Public sDate As Date = Now.Date
    Public eDate As Date = Now.Date
    Public ReportIDExport As String
    Public ReportNumberStatus As String
    Public RBT As String
    Public LocationCode As String
    Public LocationName As String
    Public MaxUserID As String
    Public ChangeLoad As String
    Public ExpenseSummaryDateFrom As String
    Public ExpenseSummaryDateTo As String
    Public WorkWith As String = ""
    Public DataToLoad As String = ""
    Public ReportStatusToLoad As String = ""
    Public UserExpenseMeal As String = ""
    Public UserTotalAmountPaidFor As Double = 0.0
    Public UserTotalExpenseAmount As Double = 0.0
    Public GetUserMeal As String = ""
    Public GetUserPaidFor As String
    Public GetUserPaidEmp As String
    Public Function LoadDuplicateUser(ByVal username As String) As DataTable
        DBConnection()
        Using dtLoadDupUser As New DataTable
            Using sqlcmdLoadDupUser As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadDupUser
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadDuplicateUser"
                        .CommandType = CommandType.StoredProcedure
                        .Parameters.Clear()
                        dtLoadDupUser.Reset()
                        .Parameters.Add("@username", SqlDbType.VarChar).Value = username
                        dtLoadDupUser.Load(.ExecuteReader)
                        Return dtLoadDupUser
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Sub LoadDuplicateUserID(ByVal userid As String)
        DBConnection()
        Using dtLoadUserID As New DataTable
            Using sqlcmdLoadUserID As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserID
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadDuplicateUserID"
                        .CommandType = CommandType.StoredProcedure
                        .Parameters.Clear()
                        dtLoadUserID.Reset()
                        .Parameters.Add("@userid", SqlDbType.VarChar).Value = userid
                        dtLoadUserID.Load(.ExecuteReader)
                        If dtLoadUserID.Rows.Count <> 0 Then
                            DuplicateUserID = dtLoadUserID.Rows(0).Item("UserID")
                        End If
                    End With
                End Using
            End Using
        End Using
    End Sub
    Public Function LoadingDataReport(ByVal userID As String, ByVal sDate As String, ByVal eDate As String) As DataTable
        DBConnection()
        Using dtLoadingER As New DataTable
            Using sqlLoaddata As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoaddata
                        .Connection = SQLConnection
                        .Parameters.Clear()
                        dtLoadingER.Reset()
                        .CommandText = "sp2_LoadDataReport_Three '" & userID & "','" & sDate & "','" & eDate & "'"
                        .Parameters.Add("@userID", SqlDbType.VarChar).Value = userID
                        .Parameters.Add("@sDate", SqlDbType.VarChar).Value = sDate
                        .Parameters.Add("@eDate", SqlDbType.VarChar).Value = eDate
                        .CommandType = CommandType.Text
                        dtLoadingER.Load(.ExecuteReader)
                        Return dtLoadingER
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadingExpenseReport(ByVal reportID As String, ByVal userID As String) As DataTable
        DBConnection()
        Using dt As New DataTable
            Using sqlLoadExpense As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoadExpense
                        .Connection = SQLConnection
                        .CommandText = "EXEC sp2_LoadExpense_Three'" & reportID & "','" & userID & "'"
                        .CommandType = CommandType.Text
                        dt.Load(.ExecuteReader)
                        Return dt
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadingExpenseCount(ByVal reportID As String) As Integer
        DBConnection()
        ExpenseCount = ""
        Using sqlcmdLoadExpenseCount As New SqlCommand
            Using dtLoadExpenseCount As New DataTable
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadExpenseCount
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadingExpenseCount"
                        .CommandType = CommandType.StoredProcedure
                        .Parameters.Clear()
                        dtLoadExpenseCount.Reset()
                        .Parameters.Add("@reportID", SqlDbType.VarChar).Value = reportID
                        dtLoadExpenseCount.Load(.ExecuteReader)
                        SQLConnection.Dispose()
                        Return dtLoadExpenseCount.Rows(0).Item("ExpenseCount")
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadingOfficersToSign(ByVal userid As String) As String
        DBConnection()
        Using dtLoadOfficersToSign As New DataTable
            Using sqlcmdLoadOfficersToSign As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadOfficersToSign
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadOfficersToSign"
                        .CommandType = CommandType.StoredProcedure
                        .Parameters.Clear()
                        dtLoadOfficersToSign.Reset()
                        .Parameters.Add("@userid", SqlDbType.VarChar).Value = userid
                        dtLoadOfficersToSign.Load(.ExecuteReader)
                        SQLConnection.Dispose()
                        Return dtLoadOfficersToSign.Rows(0).Item("UserID")
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadingUserAccountEmail(ByVal userid As String, ByVal deptID As String) As DataTable
        DBConnection()
        Using dt As New DataTable
            Using sqlLoadUserAccEmail As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoadUserAccEmail
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadUserAccEmail"
                        .CommandType = CommandType.StoredProcedure
                        dt.Reset()
                        .Parameters.Clear()
                        .Parameters.Add("@userid", SqlDbType.Int).Value = userid
                        .Parameters.Add("@deptID", SqlDbType.Int).Value = deptID
                        dt.Load(.ExecuteReader)
                        Return dt
                        'If dt.Rows.Count <> 0 Then
                        '    EmailAdd = dt.Rows(0).Item("EmailAdd")
                        '    EmailPass = dt.Rows(0).Item("EmailPass")
                        '    EmailTo = dt.Rows(0).Item("EmailTo")
                        '    EmailBCC = dt.Rows(0).Item("EmailBCC")
                        'End If
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadingUserAccountFiled(ByVal deptID As String, ByVal SignID As String) As DataTable
        DBConnection()
        Using dtLoadUserAccountFiled As New DataTable
            Using sqlLoadUserAccount As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoadUserAccount
                        .Connection = SQLConnection
                        .CommandText = "[sp2_LoadUserAccFiled]"
                        .CommandType = CommandType.StoredProcedure
                        .Parameters.Add("@DeptID", SqlDbType.BigInt).Value = deptID
                        .Parameters.Add("@SignID", SqlDbType.BigInt).Value = SignID
                        dtLoadUserAccountFiled.Load(.ExecuteReader)
                        Return dtLoadUserAccountFiled
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Sub LoadingUserAccount(ByVal deptID As String)
        DBConnection()
        Using dt As New DataTable
            Using sqlLoadUserAccount As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoadUserAccount
                        .Connection = SQLConnection
                        .CommandText = "[sp2_LoadUserAcc]"
                        .CommandType = CommandType.StoredProcedure
                        dt.Reset()
                        .Parameters.Clear()
                        .Parameters.Add("@DeptID", SqlDbType.Int).Value = deptID
                        dt.Load(.ExecuteReader)
                        frmApprove.dgvUser.DataSource = dt
                        frmUserRegistration.dgvUserAccount.DataSource = dt
                    End With
                End Using
            End Using
        End Using
    End Sub
    Public Sub LoadingUserAccountPending(ByVal deptID As String)
        DBConnection()
        Using dt As New DataTable
            Using sqlLoadUserAccount As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoadUserAccount
                        .Connection = SQLConnection
                        .CommandText = "[sp2_LoadUserAccPending]"
                        .CommandType = CommandType.StoredProcedure
                        dt.Reset()
                        .Parameters.Clear()
                        .Parameters.Add("@DeptID", SqlDbType.Int).Value = deptID
                        dt.Load(.ExecuteReader)
                        frmApprove.dgvUser.DataSource = dt
                        frmUserRegistration.dgvUserAccount.DataSource = dt
                    End With
                End Using
            End Using
        End Using
    End Sub
    Public Sub LoadUserAccountAdmin()
        DBConnection()
        Using dt As New DataTable
            Using sqlcmdLoad As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoad
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadUserAccountAdmin"
                        .CommandType = CommandType.StoredProcedure
                        dt.Load(.ExecuteReader)
                        If dt.Rows.Count = 0 Then
                            frmSelectDept.ShowDialog()
                            Exit Sub
                        End If
                    End With
                End Using
            End Using
            dt.Reset()
        End Using
    End Sub
    Public Function LoadingUserReportDetailsDONE(ByVal userID As String, ByVal FileStatus As String, ByVal signID As String) As DataTable
        DBConnection()
        Using dt As New DataTable
            Using sqlLoadUserReport As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoadUserReport
                        .Connection = SQLConnection
                        .CommandText = "[sp2_LoadUserReportDetailsDONE] '" & userID & "', '" & FileStatus & "','" & signID & "'"
                        .CommandType = CommandType.Text
                        dt.Load(.ExecuteReader)
                        Return dt
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadingUserReportDetailsFILED(ByVal userID As String, ByVal FileStatus As String, ByVal signID As String) As DataTable
        DBConnection()
        Using dt As New DataTable
            Using sqlLoadUserReport As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoadUserReport
                        .Connection = SQLConnection
                        .CommandText = "[sp2_LoadUserReportDetailsFILED] '" & userID & "', '" & FileStatus & "','" & signID & "'"
                        .CommandType = CommandType.Text
                        dt.Load(.ExecuteReader)
                        Return dt
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadingUserAccDept(ByVal UserID As String) As DataTable
        DBConnection()
        Using sqlcmdLoadUserAccByDept As New SqlCommand
            Using dtLoadUserAccByDept As New DataTable
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserAccByDept
                        .Connection = SQLConnection
                        dtLoadUserAccByDept.Reset()
                        .CommandText = "[sp2_LoadUserAccountByDept]"
                        .Parameters.Clear()
                        .Parameters.Add("@UserID", SqlDbType.VarChar).Value = UserID
                        .CommandType = CommandType.StoredProcedure
                        dtLoadUserAccByDept.Load(.ExecuteReader)
                        Return dtLoadUserAccByDept
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadingDepartment() As DataTable
        DBConnection()
        Using dt As New DataTable
            Using sqlLoadDepartment As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlLoadDepartment
                        .Parameters.Clear()
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadDepartment"
                        .CommandType = CommandType.StoredProcedure
                        dt.Load(.ExecuteReader)
                        Return dt
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoginUserAccount(ByVal Username As String, ByVal Password As String) As DataTable
        DBConnection()
        Using dtLoginUser As New DataTable
            Using sqlcmdLoginUser As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoginUser
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoginUser"
                        dtLoginUser.Reset()
                        .Parameters.Clear()
                        .Parameters.Add("@username", SqlDbType.VarChar).Value = Username
                        .Parameters.Add("@password", SqlDbType.VarChar).Value = Password
                        .CommandType = CommandType.StoredProcedure
                        .ExecuteNonQuery()
                        dtLoginUser.Load(.ExecuteReader)
                        Return dtLoginUser
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Sub loadingPreviousER(ByVal userID As String, ByVal sdate As String, ByVal edate As String)
        Try
            ConnectionPreviousER()
            Using dt As New DataTable
                Using sqlcmdLoadPrevious As New SqlCommand
                    Using conn As SqlConnection = mConn.conn
                        With sqlcmdLoadPrevious
                            .Connection = conn
                            .CommandText = "[sp2_LoadDataReport]"
                            .Parameters.Add("@userID", SqlDbType.VarChar).Value = userID
                            .Parameters.Add("@sDate", SqlDbType.VarChar).Value = sdate
                            .Parameters.Add("@eDate", SqlDbType.VarChar).Value = edate
                            .CommandType = CommandType.StoredProcedure
                            dt.Load(.ExecuteReader)
                            frmPreviousER.DataGridView1.DataSource = dt
                        End With
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub
    Public Sub LoadingExpenseER(ByVal userID As String, ByVal reportID As String, ByVal sdate As String, ByVal edate As String)
        Try
            ConnectionPreviousER()
            Using dt As New DataTable
                Using sqlcmdLoadExpenseER As New SqlCommand
                    Using conn As SqlConnection = mConn.conn
                        With sqlcmdLoadExpenseER
                            .Connection = conn
                            .CommandText = "sp_LoadExpense"
                            .CommandType = CommandType.StoredProcedure
                            .Parameters.Add("@ReportID", SqlDbType.VarChar).Value = reportID
                            .Parameters.Add("@userID", SqlDbType.VarChar).Value = userID
                            .Parameters.Add("@sDate", SqlDbType.VarChar).Value = sdate
                            .Parameters.Add("@eDate", SqlDbType.VarChar).Value = edate
                            dt.Load(.ExecuteReader)
                            frmPreviousERExpense.DataGridView1.DataSource = dt
                        End With
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub
    Public Sub LoadMaxUserID()
        DBConnection()
        Using dtLoadMaxUserID As New DataTable
            Using sqlcmdLoadMaxUserID As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadMaxUserID
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadUserIDMax"
                        .CommandType = CommandType.StoredProcedure
                        dtLoadMaxUserID.Load(.ExecuteReader)
                        If dtLoadMaxUserID.Rows.Count <> 0 Then
                            MaxUserID = dtLoadMaxUserID.Rows(0).Item("User ID")
                        End If
                    End With
                End Using
            End Using
        End Using
    End Sub
    Public Sub LoadExpenseDetails(ByVal Location As String, ByVal DeptID As String)
        DBConnection()
        Using dtLoadExpenseDetails As New DataTable
            Using sqlcmdLoadExpenseDetails As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadExpenseDetails
                        .Connection = SQLConnection
                        .CommandText = "[sp2_LoadExpenceDetails]"
                        .Parameters.Add("@Location", SqlDbType.NVarChar).Value = Location
                        .Parameters.Add("@DeptID", SqlDbType.NVarChar).Value = DeptID
                        .CommandType = CommandType.StoredProcedure
                        dtLoadExpenseDetails.Load(.ExecuteReader)
                        frmExpenseDetails.dgvViewingExpenseDetails.DataSource = dtLoadExpenseDetails
                    End With
                End Using
            End Using
        End Using
    End Sub
    Public Sub LoadClient()
        Dim dtLoadClient As New DataTable
        Dim sqlcmdLoadClient As New SqlCommand
        With sqlcmdLoadClient
            .Connection = SQLConnection
            dtLoadClient.Reset()
            .Parameters.Clear()
            .CommandText = "Select a.ID,a.clientName from tblClient as a order by a.clientName"
            .CommandType = CommandType.Text
            dtLoadClient.Load(.ExecuteReader)
            frmExpenseDetails.cbbClientName.DataSource = dtLoadClient
            frmExpenseDetails.cbbClientName.ValueMember = "ID"
            frmExpenseDetails.cbbClientName.DisplayMember = "clientName"
        End With
    End Sub

    Public Sub LoadClientToGrid(ByVal ClientCodeName As String)
        DBConnection()
        Using dtLoadClient As New DataTable
            Using sqlcmdLoadClient As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadClient
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadClientToGrid"
                        .Parameters.Add("@ClientCodeName", SqlDbType.VarChar).Value = ClientCodeName
                        .CommandType = CommandType.StoredProcedure
                        dtLoadClient.Load(.ExecuteReader)
                        frmHistory.dgvHistory.DataSource = dtLoadClient
                    End With
                End Using
            End Using
        End Using
    End Sub
    Public Function LoadSearchClient(ByVal ClientName As String)
        DBConnection()
        Using dtLoadSearchClient As New DataTable
            Using sqlcmdLoadSearchClient As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadSearchClient
                        .Connection = SQLConnection
                        .CommandText = "Select * from tblClient as a where a.clientName = '" & ClientName & "'"
                        .CommandType = CommandType.Text
                        dtLoadSearchClient.Load(.ExecuteReader)
                    End With
                    LoadSearchClient = dtLoadSearchClient.Rows.Count.ToString
                End Using
            End Using
        End Using

    End Function
    Public Sub LoadHistory(ByVal Details As String, ByVal DataToLoad As String)
        DBConnection()
        Using dtLoadHistory As New DataTable
            Using sqlcmdLoadHistory As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadHistory
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadClientData"
                        .Parameters.Add("@ClientInstrumentSerialService", SqlDbType.VarChar).Value = Details
                        .Parameters.Add("@ClientDataToLoad", SqlDbType.VarChar).Value = DataToLoad
                        .CommandType = CommandType.StoredProcedure
                        dtLoadHistory.Load(.ExecuteReader)
                        frmHistory.dgvHistory.DataSource = dtLoadHistory
                    End With
                End Using
            End Using
        End Using

    End Sub

    Public Sub LoadUserWorkWith()
        DBConnection()
        Using dtLoadUserWorkWith As New DataTable
            Using sqlcmdLoadUserWorkWith As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserWorkWith
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadUserWorkWIth"
                        .Parameters.Clear()
                        .Parameters.Add("@DeptID", SqlDbType.VarChar).Value = GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0)
                        .Parameters.Add("@Username", SqlDbType.VarChar).Value = GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0)
                        .CommandType = CommandType.StoredProcedure
                        dtLoadUserWorkWith.Load(.ExecuteReader)
                        frmAdditionalInput.dgvUser.DataSource = dtLoadUserWorkWith
                        frmAdditionalInput.dgvUser.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                        Dim chkbox As New DataGridViewCheckBoxColumn With {
                            .FlatStyle = FlatStyle.Standard
                        }
                        frmAdditionalInput.dgvUser.Columns.Insert(0, chkbox)
                    End With
                End Using
            End Using
        End Using
    End Sub

    Public Sub LoadUserMealRate(ByVal Username As String, ByVal Meal As String)
        DBConnection()
        Using dtLoadUserMealRate As New DataTable
            Using sqlcmdLoadUserMealRate As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserMealRate
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadUserRates"
                        .Parameters.AddWithValue("@Username", Username).SqlDbType = SqlDbType.VarChar
                        .CommandType = CommandType.StoredProcedure
                        dtLoadUserMealRate.Load(.ExecuteReader)
                        If dtLoadUserMealRate.Rows.Count <> 0 Then
                            UserTotalAmountPaidFor = IIf(UserTotalAmountPaidFor = 0, 0, UserTotalAmountPaidFor) + dtLoadUserMealRate.Rows(0).Item(Meal)
                            UserWorkWithExpense = IIf(dtLoadUserMealRate.Rows(0).Item(Meal) = 0, 0, dtLoadUserMealRate.Rows(0).Item(Meal))
                        End If
                    End With
                End Using
            End Using
        End Using
    End Sub

    Public Function LoadUserExpenseRate(ByVal Username As String) As DataTable
        DBConnection()
        Using dtLoadUserExpenseRate As New DataTable
            Using sqlcmdLoadUserExpenseRate As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserExpenseRate
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadUserRates"
                        .Parameters.AddWithValue("@Username", Username).SqlDbType = SqlDbType.VarChar
                        .CommandType = CommandType.StoredProcedure
                        dtLoadUserExpenseRate.Load(.ExecuteReader)
                        Return dtLoadUserExpenseRate
                    End With
                End Using
            End Using
        End Using
    End Function

    Public Function LoadUserExpenseMeal(ByVal ExpenseID As String) As DataTable
        DBConnection()
        Using dtLoadUserExpenseMeal As New DataTable
            Using sqlcmdLoadUserExpenseMeal As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserExpenseMeal
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadUserExpenseMeal"
                        .Parameters.AddWithValue("@ExpenseID", ExpenseID).SqlDbType = SqlDbType.VarChar
                        .CommandType = CommandType.StoredProcedure
                        dtLoadUserExpenseMeal.Load(.ExecuteReader)
                        Return dtLoadUserExpenseMeal
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadFare() As DataTable
        DBConnection()
        Using dtLoadFare As New DataTable
            Using sqlcmdLoadFare As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadFare
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadFare"
                        .Parameters.Add("@DeptID", SqlDbType.VarChar).Value = GetRegistryValue("Software\\ER System\\UserAccount", {"DeptID"})(0)
                        .CommandType = CommandType.StoredProcedure
                        dtLoadFare.Load(.ExecuteReader)
                        Return dtLoadFare
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadUserExpenseTrans(ByVal ExpenseID As String) As DataTable
        DBConnection()
        Using dtLoadUserExpenseTrans As New DataTable
            Using sqlcmdLoadUserExpenseTrans As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserExpenseTrans
                        .Connection = SQLConnection
                        .CommandText = "[sp2_LoadUserExpenseTrans]"
                        .Parameters.AddWithValue("@ExpenseID", ExpenseID).SqlDbType = SqlDbType.BigInt
                        .CommandType = CommandType.StoredProcedure
                        dtLoadUserExpenseTrans.Load(.ExecuteReader)
                        Return dtLoadUserExpenseTrans
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function Load1stApprover(ByVal UserID As String) As Integer
        DBConnection()
        Using dtLoadApproverAuthentication As New DataTable
            Using sqlcmdLoadApproverAuthentication As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadApproverAuthentication
                        .Connection = SQLConnection
                        .CommandText = "sp2_Load1stApprover"
                        .Parameters.AddWithValue("@UserID", UserID).SqlDbType = SqlDbType.Int
                        .CommandType = CommandType.StoredProcedure
                        dtLoadApproverAuthentication.Load(.ExecuteReader)
                        If dtLoadApproverAuthentication.Rows(0).Item("Approver1") = GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) Then
                            Return 1
                        Else
                            Return 0
                        End If
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function Load2ndApprover(ByVal UserID As String) As Integer
        DBConnection()
        Using dtLoadApproverAuthentication As New DataTable
            Using sqlcmdLoadApproverAuthentication As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadApproverAuthentication
                        .Connection = SQLConnection
                        .CommandText = "sp2_Load1stApprover"
                        .Parameters.AddWithValue("@UserID", UserID).SqlDbType = SqlDbType.Int
                        .CommandType = CommandType.StoredProcedure
                        dtLoadApproverAuthentication.Load(.ExecuteReader)
                        If dtLoadApproverAuthentication.Rows(0).Item("Approver2").ToString() = GetRegistryValue("Software\\ER System\\UserAccount", {"username"})(0) Then
                            Return 1
                        Else
                            Return 0
                        End If
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadEReportDetails(ByVal ReportID As String, ByVal UserID As String) As DataTable
        DBConnection()
        Using dtLoadEReportDetails As New DataTable
            Using sqlcmdLoadEReportDetails As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadEReportDetails
                        .Connection = mConn.SQLConnection
                        .CommandText = "[sp2_LoadEReportDetails]"
                        .Parameters.AddWithValue("@ReportID", ReportID).SqlDbType = SqlDbType.VarChar
                        .Parameters.AddWithValue("@userID", UserID).SqlDbType = SqlDbType.Int
                        .CommandType = CommandType.StoredProcedure
                        dtLoadEReportDetails.Load(.ExecuteReader)
                        Return dtLoadEReportDetails
                    End With
                End Using
            End Using
        End Using
    End Function
    Public Function LoadExpenseReportDetails(ByVal ReportID As String, ByVal ExpenseID As String) As DataTable
        DBConnection()
        Using dtLoadExpenseDetails As New DataTable
            Using sqlcmdLoadExpenseDetails As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadExpenseDetails
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadEReportExpenseDetails"
                        .Parameters.AddWithValue("@ReportID", ReportID).SqlDbType = SqlDbType.VarChar
                        .Parameters.AddWithValue("@ExpenseID", ExpenseID).SqlDbType = SqlDbType.VarChar
                        .CommandType = CommandType.StoredProcedure
                        dtLoadExpenseDetails.Load(.ExecuteReader)
                        Return dtLoadExpenseDetails
                    End With
                End Using
            End Using
        End Using
    End Function
    Friend Function LoadUserAuthority(ByVal UserID As String, ByVal SignID As String) As DataTable
        DBConnection()
        Using dtLoadUserAuthority As New DataTable
            Using sqlcmdLoadUserAuthority As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserAuthority
                        .Connection = SQLConnection
                        .CommandText = "SELECT * FROM tbUserAuthority where UserID = '" & UserID & "' and AuthorityID = '" & SignID & "'"
                        .CommandType = CommandType.Text
                        dtLoadUserAuthority.Load(.ExecuteReader)
                        Return dtLoadUserAuthority
                    End With
                End Using
            End Using
        End Using
    End Function
    Friend Function LoadUserAuthorityAccept(ByVal ReportID As String, ByVal SignID As String, ByVal UserID As String) As DataTable
        DBConnection()
        Using dtLoadUserAuthorityAccept As New DataTable
            Using sqlcmdLoadUserAuthorityAccept As New SqlCommand
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadUserAuthorityAccept
                        .Connection = SQLConnection
                        .CommandText = "SELECT COUNT(ReportID) as myCount,SignID  FROM tbReportAuthority 
                                        WHERE ReportID = '" & ReportID & "' and UserID = '" & UserID & "' 
                                        Group BY SignID,ReportID"
                        .CommandType = CommandType.Text
                        dtLoadUserAuthorityAccept.Load(.ExecuteReader)
                        Return dtLoadUserAuthorityAccept
                    End With
                End Using
            End Using
        End Using
    End Function
    Friend Function LoadReportSentStatus(ByVal reportidexport As String) As DataTable
        DBConnection()

        Using sqlcmdLoadReportSentStatus As New SqlCommand
            Using dtloadReportSentStatus As New DataTable
                Using SqlConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadReportSentStatus
                        .Connection = SqlConnection
                        .CommandText = "SELECT a.ReportSentStatus from tbReportDetails as a where [ID] = '" & reportidexport & "'"
                        .CommandType = CommandType.Text
                        dtloadReportSentStatus.Load(.ExecuteReader)
                        Return dtloadReportSentStatus
                    End With
                End Using
            End Using
        End Using
    End Function

    Friend Function LoadNotification(ByVal DateIncluded As String, ByVal Username As String, ByVal Category As String) As String
        DBConnection()
        Using sqlcmdLoadNotification As New SqlCommand
            Using dtLoadNotification As New DataTable
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdLoadNotification
                        .Connection = SQLConnection
                        .CommandText = "sp2_LoadNotification"
                        .Parameters.Clear()
                        .Parameters.Add("@DateIncluded", SqlDbType.Date).Value = DateIncluded
                        .Parameters.Add("@Username", SqlDbType.VarChar).Value = Username
                        .Parameters.Add("@Category", SqlDbType.VarChar).Value = Category
                        .CommandType = CommandType.StoredProcedure
                        dtLoadNotification.Load(.ExecuteReader)
                        If dtLoadNotification.Rows.Count <> 0 Then
                            Return ProcNotification(dtLoadNotification)
                        Else
                            Return ""
                        End If
                    End With
                End Using
            End Using
        End Using
    End Function

    Private Function ProcNotification(ByVal dt As DataTable) As String
        Dim str As String = ""
        For x = 0 To dt.Rows.Count - 1
            If dt.Rows.Count <> 0 Then
                If dt.Rows(x).Item("Category") Like "*Breakfast*" Then
                    str = IIf(str = "", "", str + "/") + "1^" + dt.Rows(0).Item("username") + "^" + dt.Rows(x).Item("Category").ToString().Split("^")(1)
                ElseIf dt.Rows(x).Item("Category") Like "*Lunch*" Then
                    str = IIf(str = "", "", "/") + "2^" + dt.Rows(0).Item("username") + "^" + dt.Rows(x).Item("Category").ToString().Split("^")(1)
                Else
                    str = IIf(str = "", "", str + "/") + "3^" + dt.Rows(0).Item("username") + "^" + dt.Rows(x).Item("Category").ToString().Split("^")(1)
                End If
            Else
                str = ""
            End If
        Next
        Return str
    End Function

    Friend Function SearchVersion() As String
        DBConnection()
        Using sqlcmdSearchVersion As New SqlCommand
            Using dtSearchVersion As New DataTable
                Using SQLConnection As SqlConnection = mConn.SQLConnection
                    With sqlcmdSearchVersion
                        .Connection = SQLConnection
                        .CommandText = "Select FileVersion from tbExeUpdater where [ID] = 1"
                        .Parameters.Clear()
                        .CommandType = CommandType.Text
                        dtSearchVersion.Load(.ExecuteReader)
                        If dtSearchVersion.Rows.Count <> 0 Then
                            Return dtSearchVersion.Rows(0).Item("FileVersion")
                        Else
                            Return ""
                        End If
                    End With
                End Using
            End Using
        End Using
    End Function
End Module
