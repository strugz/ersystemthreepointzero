Imports System.Data.Entity
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class AccountSettingsRepository
        Implements IAccountSettingsRepository

        Public Function LoadDepartments() As List(Of DepartmentDto) Implements IAccountSettingsRepository.LoadDepartments
            Using dbContext As New AppDbContext()
                Return dbContext.Departments.
                    AsNoTracking().
                    OrderBy(Function(item) item.emp_Dept).
                    Select(Function(item) New DepartmentDto With {
                        .Id = item.ID,
                        .Name = item.emp_Dept
                    }).
                    ToList()
            End Using
        End Function

        Public Function LoadAuthorityUsers() As List(Of AuthorityUserDto) Implements IAccountSettingsRepository.LoadAuthorityUsers
            Using dbContext As New AppDbContext()
                Return dbContext.UserRegistrations.
                    AsNoTracking().
                    Where(Function(item) item.UserID.HasValue AndAlso item.Username IsNot Nothing).
                    OrderBy(Function(item) item.Username).
                    Select(Function(item) New AuthorityUserDto With {
                        .UserId = item.UserID.Value,
                        .UserName = item.Username,
                        .FullName = item.Fullname
                    }).
                    ToList()
            End Using
        End Function

        Public Function LoadAccountSettings(userId As Integer) As AccountSettingsDto Implements IAccountSettingsRepository.LoadAccountSettings
            Using dbContext As New AppDbContext()
                Dim user As UserRegistrationModel = dbContext.UserRegistrations.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.UserID.HasValue AndAlso item.UserID.Value = userId)

                If user Is Nothing Then
                    Throw New InvalidOperationException("The current user account was not found.")
                End If

                Dim department As DepartmentModel = Nothing
                If user.DeptID.HasValue Then
                    Dim deptId As Integer = user.DeptID.Value
                    department = dbContext.Departments.AsNoTracking().FirstOrDefault(Function(item) item.ID = deptId)
                End If

                Dim employeeRate As EmployeeRateModel = dbContext.EmployeeRates.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.UserID.HasValue AndAlso item.UserID.Value = userId)

                Dim account As AccountSettingsDto = ToAccountSettingsDto(user, department, employeeRate)
                account.AuthorityRows = dbContext.UserAuthorities.
                    AsNoTracking().
                    Where(Function(item) item.UserID.HasValue AndAlso item.UserID.Value = userId).
                    OrderBy(Function(item) item.Sort).
                    ThenBy(Function(item) item.Id).
                    Select(Function(item) New UserAuthorityDto With {
                        .Id = item.Id,
                        .UserId = item.UserID.Value,
                        .AuthorityId = item.AuthorityID,
                        .AuthorityName = item.AuthorityName,
                        .Sort = item.Sort
                    }).
                    ToList()

                Return account
            End Using
        End Function

        Public Sub SaveAccountSettings(account As AccountSettingsDto) Implements IAccountSettingsRepository.SaveAccountSettings
            If account Is Nothing Then
                Throw New ArgumentNullException("account")
            End If

            Using dbContext As New AppDbContext()
            Using transaction = dbContext.Database.BeginTransaction()
                Try
                    UpdateUserRegistration(dbContext, account)
                    UpsertEmployeeRate(dbContext, account)
                    ReplaceAuthorities(dbContext, account)
                    dbContext.SaveChanges()
                    transaction.Commit()
                Catch
                    transaction.Rollback()
                    Throw
                End Try
            End Using
            End Using
        End Sub

        Public Function LoadSessionAccount(userId As Integer) As AccountSettingsDto Implements IAccountSettingsRepository.LoadSessionAccount
            Return LoadAccountSettings(userId)
        End Function

        Private Shared Function ToAccountSettingsDto(user As UserRegistrationModel,
                                                     department As DepartmentModel,
                                                     employeeRate As EmployeeRateModel) As AccountSettingsDto
            Dim account As New AccountSettingsDto With {
                .Id = user.ID,
                .UserId = user.UserID.GetValueOrDefault(),
                .UserName = user.Username,
                .FullName = user.Fullname,
                .UserLevel = user.Userlevel,
                .DeptId = user.DeptID,
                .DepartmentName = If(department Is Nothing, String.Empty, department.emp_Dept),
                .EmailAdd = user.EmailAdd,
                .EmailPass = user.EmailPass,
                .EmailTo = user.EmailTo,
                .EmailBcc = user.EmailBCC,
                .Signature = user.Signature,
                .Position = user.Position,
                .Status = user.Status,
                .Approver1 = user.Approver1,
                .Approver2 = user.Approver2,
                .ReportNumberStatus = user.ReportNumberStatus,
                .WorkWithStatus = user.WorkWithStatus,
                .SuperApprover = user.SuperApprover
            }

            If employeeRate IsNot Nothing Then
                account.TranspoRate = employeeRate.TranspoRate
                account.BreakFastRate = employeeRate.BreakFastRate
                account.LunchRate = employeeRate.LunchRate
                account.DinnerRate = employeeRate.DinnerRate
                account.OtMeal = employeeRate.OTMeal
            End If

            Return account
        End Function

        Private Shared Sub UpdateUserRegistration(dbContext As AppDbContext, account As AccountSettingsDto)
            Dim user As UserRegistrationModel = dbContext.UserRegistrations.
                FirstOrDefault(Function(item) item.UserID.HasValue AndAlso item.UserID.Value = account.UserId)

            If user Is Nothing Then
                Throw New InvalidOperationException("The current user account was not found.")
            End If

            user.Fullname = NormalizeEmpty(account.FullName)
            user.Userlevel = NormalizeEmpty(account.UserLevel)
            user.DeptID = account.DeptId
            user.EmailAdd = NormalizeEmpty(account.EmailAdd)
            user.EmailPass = NormalizeEmpty(account.EmailPass)
            user.EmailTo = NormalizeEmpty(account.EmailTo)
            user.EmailBCC = NormalizeEmpty(account.EmailBcc)
            user.Signature = account.Signature
            user.Position = NormalizeEmpty(account.Position)
            user.Status = NormalizeEmpty(account.Status)
            user.Approver1 = NormalizeEmpty(account.Approver1)
            user.Approver2 = NormalizeEmpty(account.Approver2)
            user.ReportNumberStatus = account.ReportNumberStatus
            user.WorkWithStatus = NormalizeEmpty(account.WorkWithStatus)
            user.SuperApprover = NormalizeEmpty(account.SuperApprover)
        End Sub

        Private Shared Sub UpsertEmployeeRate(dbContext As AppDbContext, account As AccountSettingsDto)
            Dim employeeRate As EmployeeRateModel = dbContext.EmployeeRates.
                FirstOrDefault(Function(item) item.UserID.HasValue AndAlso item.UserID.Value = account.UserId)

            If employeeRate Is Nothing Then
                employeeRate = New EmployeeRateModel With {.UserID = account.UserId}
                dbContext.EmployeeRates.Add(employeeRate)
            End If

            employeeRate.TranspoRate = account.TranspoRate
            employeeRate.BreakFastRate = account.BreakFastRate
            employeeRate.LunchRate = account.LunchRate
            employeeRate.DinnerRate = account.DinnerRate
            employeeRate.OTMeal = account.OtMeal
        End Sub

        Private Shared Sub ReplaceAuthorities(dbContext As AppDbContext, account As AccountSettingsDto)
            Dim existingAuthorities As List(Of UserAuthorityModel) = dbContext.UserAuthorities.
                Where(Function(item) item.UserID.HasValue AndAlso item.UserID.Value = account.UserId).
                ToList()

            dbContext.UserAuthorities.RemoveRange(existingAuthorities)

            For Each authority As UserAuthorityDto In account.AuthorityRows
                dbContext.UserAuthorities.Add(New UserAuthorityModel With {
                    .UserID = account.UserId,
                    .AuthorityID = authority.AuthorityId,
                    .AuthorityName = NormalizeEmpty(authority.AuthorityName),
                    .Sort = authority.Sort
                })
            Next
        End Sub

        Private Shared Function NormalizeEmpty(value As String) As String
            If String.IsNullOrWhiteSpace(value) Then
                Return Nothing
            End If

            Return value.Trim()
        End Function
    End Class
End Namespace
