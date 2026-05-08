Option Strict On

Imports ER_System.Domain.Entities
Imports ER_System.Domain.Enums

Namespace Application.Services
    Public Class LoginAccessService
        Private Const ImsDepartment As String = "IMS"

        Public Function Resolve(ByVal userAccount As UserAccount, ByVal enteredUsername As String) As LoginAccessResult
            If userAccount Is Nothing OrElse Not IsExpectedUser(userAccount, enteredUsername) Then
                Return New LoginAccessResult With {.IsAllowed = False}
            End If

            Dim isImsDepartment As Boolean = String.Equals(userAccount.DepartmentName, ImsDepartment, StringComparison.OrdinalIgnoreCase)

            If String.Equals(userAccount.UserLevel, UserLevel.Admin, StringComparison.OrdinalIgnoreCase) Then
                Return CreateResult(userAccount, True, True, True, isImsDepartment, isImsDepartment, isImsDepartment)
            End If

            If String.Equals(userAccount.UserLevel, UserLevel.User, StringComparison.OrdinalIgnoreCase) Then
                Return CreateResult(userAccount, False, True, True, isImsDepartment, False, False)
            End If

            Return New LoginAccessResult With {.IsAllowed = False}
        End Function

        Private Function IsExpectedUser(ByVal userAccount As UserAccount, ByVal enteredUsername As String) As Boolean
            Return String.Equals(userAccount.UserName, enteredUsername, StringComparison.OrdinalIgnoreCase)
        End Function

        Private Function CreateResult(
            ByVal userAccount As UserAccount,
            ByVal menuFormsVisible As Boolean,
            ByVal menuFileVisible As Boolean,
            ByVal mainFormEnabled As Boolean,
            ByVal previousReportsVisible As Boolean,
            ByVal userAccountVisible As Boolean,
            ByVal expenseSummaryVisible As Boolean) As LoginAccessResult

            Return New LoginAccessResult With {
                .IsAllowed = True,
                .DisplayName = SanitizeDisplayValue(userAccount.FullName),
                .DepartmentName = SanitizeDisplayValue(userAccount.DepartmentName),
                .MenuFormsVisible = menuFormsVisible,
                .MenuFileVisible = menuFileVisible,
                .MainFormEnabled = mainFormEnabled,
                .PreviousReportsVisible = previousReportsVisible,
                .UserAccountVisible = userAccountVisible,
                .ExpenseSummaryVisible = expenseSummaryVisible
            }
        End Function

        Private Function SanitizeDisplayValue(ByVal value As String) As String
            If value Is Nothing Then
                Return String.Empty
            End If

            Return value.TrimStart().Replace(vbCrLf, String.Empty)
        End Function
    End Class
End Namespace
