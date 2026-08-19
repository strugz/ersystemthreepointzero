Option Strict On

Imports ERSystem.Infrastructure.Configuration

Namespace AppServices
    Friend Class UserAccountService
        Private Const EncryptionKey As String = "crimsonmonastery2003"

        Private Shared ReadOnly UserAccountValueNames As String() = {
            "UserID", "username", "Userlevel", "DeptID", "Fullname", "emp_Dept", "BreakFastRate",
            "LunchRate", "DinnerRate", "OTMeal", "TranspoRate", "Password", "Approver1", "Approver2"
        }

        Private ReadOnly _repository As Infrastructure.Data.Repositories.UserAccountRepository
        Private ReadOnly _userAccountRegistryProvider As UserAccountRegistryProvider
        Private ReadOnly _encryption As clsEncryption

        Public Sub New()
            _repository = New Infrastructure.Data.Repositories.UserAccountRepository()
            _userAccountRegistryProvider = New UserAccountRegistryProvider()
            _encryption = New clsEncryption(EncryptionKey)
        End Sub

        Public Function EnsureConnection() As StartupConnectionResult
            DBConnection()

            If IsConnected Then
                Return New StartupConnectionResult With {
                    .IsConnected = True,
                    .RequiresConnectionSetup = False,
                    .ShouldExitApplication = False
                }
            End If

            Return New StartupConnectionResult With {
                .IsConnected = False,
                .RequiresConnectionSetup = True,
                .ShouldExitApplication = False
            }
        End Function

        Public Function FinalizeConnectionSetup() As StartupConnectionResult
            DBConnection()

            Return New StartupConnectionResult With {
                .IsConnected = IsConnected,
                .RequiresConnectionSetup = Not IsConnected,
                .ShouldExitApplication = Not IsConnected
            }
        End Function

        Public Function CheckAdminAccounts() As StartupAdminCheckResult
            Dim hasAdminAccounts As Boolean = _repository.HasAdminAccounts()

            Return New StartupAdminCheckResult With {
                .HasAdminAccounts = hasAdminAccounts,
                .RequiresDepartmentSelection = Not hasAdminAccounts
            }
        End Function

        Public Function Login(request As LoginRequest) As LoginResult
            If request Is Nothing Then
                Throw New ArgumentNullException(NameOf(request))
            End If

            Dim normalizedUserName As String = request.UserName.Trim().ToUpperInvariant()
            Dim password As String = request.Password.Trim()

            If normalizedUserName.Length = 0 OrElse password.Length = 0 Then
                Return New LoginResult With {
                    .IsSuccess = False,
                    .Message = "Please Fill Your Username/Password"
                }
            End If

            Using loginUserAccount As DataTable = _repository.LoginUserAccount(normalizedUserName, _encryption.EncryptData(password))
                If loginUserAccount.Rows.Count = 0 Then
                    Return New LoginResult With {
                        .IsSuccess = False,
                        .Message = "Username " & request.UserName & " Not Detected"
                    }
                End If

                PersistUserAccount(loginUserAccount)
                Return BuildLoginResult(normalizedUserName)
            End Using
        End Function

        Private Function BuildLoginResult(normalizedUserName As String) As LoginResult
            Dim department As String = GetStoredValue("emp_Dept")
            Dim registryUserName As String = GetStoredValue("username")
            Dim userLevel As String = GetStoredValue("Userlevel")
            Dim fullName As String = GetStoredValue("Fullname").Replace(vbCrLf, String.Empty)
            Dim isImsDepartment As Boolean = String.Equals(department, "IMS", StringComparison.OrdinalIgnoreCase)
            Dim isAdmin As Boolean = String.Equals(userLevel, "Admin", StringComparison.OrdinalIgnoreCase)
            Dim isUserMatch As Boolean = String.Equals(registryUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase)

            If Not isUserMatch Then
                Return New LoginResult With {
                    .IsSuccess = False,
                    .Message = "Invalid Username/Password"
                }
            End If

            If Not isImsDepartment AndAlso isAdmin Then
                Return CreateSuccessfulLoginResult(fullName.TrimStart(), department.TrimStart(), True, True, True, False, False, False)
            End If

            If isImsDepartment AndAlso isAdmin Then
                Return CreateSuccessfulLoginResult(fullName, department, True, True, True, True, True, True)
            End If

            If isImsDepartment Then
                Return CreateSuccessfulLoginResult(fullName, department.TrimStart(), False, True, True, True, False, False)
            End If

            If String.Equals(userLevel, "User", StringComparison.OrdinalIgnoreCase) Then
                Return CreateSuccessfulLoginResult(fullName.TrimStart(), department.TrimStart(), False, True, True, False, False, False)
            End If

            If String.Equals(userLevel, "Finance", StringComparison.OrdinalIgnoreCase) Then
                Return CreateSuccessfulLoginResult(fullName.TrimStart(), department.TrimStart(), True, True, True, True, False, False)
            End If

            Return New LoginResult With {
                .IsSuccess = False,
                .Message = "Invalid Username/Password"
            }
        End Function

        Private Function CreateSuccessfulLoginResult(fullName As String,
                                                     department As String,
                                                     showMenuForms As Boolean,
                                                     showMenuFile As Boolean,
                                                     enableMainForm As Boolean,
                                                     showPreviousReports As Boolean,
                                                     showUserAccountMenu As Boolean,
                                                     showExpenseSummary As Boolean) As LoginResult
            Return New LoginResult With {
                .IsSuccess = True,
                .FullName = fullName,
                .Department = department,
                .ShowMenuForms = showMenuForms,
                .ShowMenuFile = showMenuFile,
                .EnableMainForm = enableMainForm,
                .ShowPreviousReports = showPreviousReports,
                .ShowUserAccountMenu = showUserAccountMenu,
                .ShowExpenseSummary = showExpenseSummary
            }
        End Function

        Private Sub PersistUserAccount(loginUserAccount As DataTable)
            Dim values As String() = {
                loginUserAccount.Rows(0).Item(UserAccountValueNames(0)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(1)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(2)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(3)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(4)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(5)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(6)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(7)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(8)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(9)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(10)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(11)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(12)).ToString(),
                loginUserAccount.Rows(0).Item(UserAccountValueNames(13)).ToString()
            }

            _userAccountRegistryProvider.Save(UserAccountValueNames, values)
        End Sub

        Private Function GetStoredValue(valueName As String) As String
            Return _userAccountRegistryProvider.GetValue(valueName)
        End Function
    End Class
End Namespace
